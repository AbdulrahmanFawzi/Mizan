using Microsoft.Extensions.Logging;
using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Domain.Banking;
using Mizan.Domain.Categorization;
using Mizan.Domain.Common;
using Mizan.Domain.Importing;
using Mizan.Domain.Transactions;

namespace Mizan.Application.Banking;

public sealed record ImportResult(int Added, int Updated, int Removed, int NeedReview, int FailedAccounts);

// UC-80, UC-83, UC-84: brings one bank connection's accounts and transactions up to date.
// The rules themselves live in the Domain services; this class only fetches, applies, and saves.
public sealed class BankImportService(
    IBankingProvider provider,
    IBankConnectionRepository connections,
    IUserRepository users,
    ICategoryRepository categories,
    IMerchantRuleRepository merchantRules,
    ITransactionRepository transactions,
    IBankTransactionRepository bankTransactions,
    ISyncLock syncLock,
    IUnitOfWork unitOfWork,
    Clock clock,
    ILogger<BankImportService> logger)
{
    // Null when the connection isn't active or another sync for it is already running.
    public async Task<ImportResult?> ImportAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        await using var held = await syncLock.TryAcquireAsync(connectionId, cancellationToken);
        if (held is null)
        {
            logger.LogInformation("Sync for connection {ConnectionId} is already running", connectionId);
            return null;
        }

        var connection = await connections.GetByIdAsync(connectionId, cancellationToken)
            ?? throw new NotFoundException("Bank connection");

        if (!connection.IsActive)
        {
            return null;
        }

        try
        {
            return await ImportActiveAsync(connection, cancellationToken);
        }
        catch (ConsentExpiredException)
        {
            // BR-C6: stop syncing and tell the user once.
            connection.Expire();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }
    }

    private async Task<ImportResult> ImportActiveAsync(BankConnection connection, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(connection.UserId, cancellationToken) ?? throw new NotFoundException("User");
        var now = clock.Now;
        var today = clock.Today;

        var accountData = await provider.GetAccountsAsync(connection.ProviderEntityId, cancellationToken);
        var accounts = connection.SyncAccounts(accountData.Select(ToSnapshot), user.Currency);
        var window = connection.NextSyncWindow(today);
        var isInitial = !connection.HistoryImported;

        var categoryChoices = (await categories.ListAsync(user.Id, cancellationToken))
            .Select(c => new CategoryChoice(c.Id, c.Name.Value, c.Kind))
            .ToList();
        var ownAccounts = await OwnAccountsAsync(user.Id, cancellationToken);

        var context = new ImportContext(
            categoryChoices,
            await merchantRules.ListAsync(user.Id, cancellationToken),
            ownAccounts.Where(a => a.Iban is not null).ToDictionary(a => a.Iban!, a => new CounterpartyAccount(a.Id, a.Kind)),
            ownAccounts.ToDictionary(a => a.Id, a => a.Kind),
            categoryChoices.Single(c => c.Kind == Domain.Categories.CategoryKind.Savings).Id,
            user.Currency);

        var affected = new HashSet<AffectedSpending>();
        var imported = new List<(Transaction Transaction, BankTransactionFacts Facts)>();
        int updated = 0, removed = 0, failed = 0, received = 0;

        foreach (var account in accounts)
        {
            IReadOnlyList<BankTransactionData> data;
            try
            {
                data = await provider.GetTransactionsAsync(connection.ProviderEntityId, account.ProviderAccountId, window, cancellationToken);
            }
            catch (BankingProviderException exception)
            {
                // One slow account (a 504 in the sandbox) must not stop the others.
                logger.LogWarning(exception, "Fetching account {AccountId} failed", account.Id);
                failed++;
                continue;
            }

            var facts = data.Where(d => d.Amount > 0).Select(ToFacts).ToList();
            received += facts.Count;

            var existing = await bankTransactions.GetByProviderIdsAsync(
                connection.Id, facts.Select(f => f.ProviderTransactionId).ToList(), cancellationToken);

            foreach (var fact in facts)
            {
                if (existing.TryGetValue(fact.ProviderTransactionId, out var known))
                {
                    // BR-S5 and BR-X7: update in place, the user's choices stay.
                    var before = new AffectedSpending(known.CategoryId, known.Date);
                    var changed = known.ApplyBankUpdate(fact, now);
                    changed |= LinkLateCounterparty(known, fact, account, context, now);

                    if (changed)
                    {
                        affected.Add(before);
                        affected.Add(new AffectedSpending(known.CategoryId, known.Date));
                        updated++;
                    }

                    continue;
                }

                var transaction = Create(connection, account, fact, context, now);
                transactions.Add(transaction);
                imported.Add((transaction, fact));
                affected.Add(new AffectedSpending(transaction.CategoryId, transaction.Date));
            }

            removed += await DropVanishedPendingAsync(account, window, facts, affected, cancellationToken);
        }

        if (accounts.Count > 0 && failed == accounts.Count)
        {
            throw new BankingProviderException("Could not fetch any account from the bank.");
        }

        var needReview = imported.Count(i => i.Transaction.Review is not null);
        needReview += await FlagTransfersAsync(user.Id, window, imported, context, now, cancellationToken);
        needReview += await FlagDuplicatesAsync(user.Id, imported, affected, now, cancellationToken);

        // A first import that came back empty may simply be too early (the provider is still collecting data).
        var complete = failed == 0 && (!isInitial || received > 0);
        connection.RecordSync(now, affected.ToList(), needReview, complete);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Imported connection {ConnectionId}: {Added} added, {Updated} updated, {Removed} removed, {Review} to review, {Failed} accounts failed",
            connection.Id, imported.Count, updated, removed, needReview, failed);

        return new ImportResult(imported.Count, updated, removed, needReview, failed);
    }

    private static Transaction Create(
        BankConnection connection, BankAccount account, BankTransactionFacts fact, ImportContext context, DateTimeOffset now)
    {
        // BR-R1: is the other side one of the user's own accounts?
        var counterparty = fact.CounterpartyIban is { } iban ? context.OwnAccounts.GetValueOrDefault(iban) : null;

        var classification = TransactionClassifier.Classify(
            fact.Direction, fact.SubType, fact.Amount.Currency,
            new AccountContext(account.IncludeInBudget, account.Currency), counterparty, context.BudgetCurrency);

        var category = CategoryResolver.Resolve(
            fact.MerchantKey, fact.ProviderCategory, fact.IsCashWithdrawal, context.Rules, context.Categories);

        // OB-13: saving goes to the Savings category; only the user's own merchant rule can say otherwise.
        if (classification.IsSaving && category.Source != CategorySource.MerchantRule)
        {
            category = new CategoryDecision(context.SavingsCategoryId, CategorySource.Fallback);
        }

        return Transaction.ImportFromBank(
            connection.UserId,
            new BankReference(connection.Id, account.Id, fact.ProviderTransactionId),
            fact,
            classification.Treatment,
            classification.Review,
            category.CategoryId,
            category.Source,
            now,
            counterparty?.AccountId);
    }

    // Transactions imported before counterparties were kept: link them now. One still waiting for a
    // transfer answer is classified again, so a transfer into savings becomes saving on its own (OB-13).
    private static bool LinkLateCounterparty(
        Transaction known, BankTransactionFacts fact, BankAccount account, ImportContext context, DateTimeOffset now)
    {
        if (known.CounterpartyAccountId is not null
            || fact.CounterpartyIban is not { } iban
            || context.OwnAccounts.GetValueOrDefault(iban) is not { } counterparty)
        {
            return false;
        }

        known.LinkCounterparty(counterparty.AccountId, now);
        if (known.Review != ReviewReason.PossibleTransfer)
        {
            return false;
        }

        var classification = TransactionClassifier.Classify(
            fact.Direction, fact.SubType, fact.Amount.Currency,
            new AccountContext(account.IncludeInBudget, account.Currency), counterparty, context.BudgetCurrency);
        known.Reclassify(classification, context.SavingsCategoryId, now);
        return true;
    }

    // BR-X9: a pending transaction missing from a fetch that covers its date was cancelled.
    private async Task<int> DropVanishedPendingAsync(
        BankAccount account, SyncWindow window, IReadOnlyList<BankTransactionFacts> facts,
        HashSet<AffectedSpending> affected, CancellationToken cancellationToken)
    {
        var seen = facts.Select(f => f.ProviderTransactionId).ToHashSet();
        var vanished = (await bankTransactions.ListPendingAsync(account.Id, window, cancellationToken))
            .Where(t => !seen.Contains(t.Bank!.ProviderTransactionId))
            .ToList();

        foreach (var transaction in vanished)
        {
            await ReleaseDuplicateReviewAsync(transaction, cancellationToken);
            transaction.Discard();
            transactions.Remove(transaction);
            affected.Add(new AffectedSpending(transaction.CategoryId, transaction.Date));
        }

        return vanished.Count;
    }

    // BR-R1 fallback, for transfers without an IBAN on the other side.
    private async Task<int> FlagTransfersAsync(
        Guid userId, SyncWindow window, List<(Transaction Transaction, BankTransactionFacts Facts)> imported,
        ImportContext context, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var outgoing = imported
            .Where(i => i.Facts.CounterpartyIban is null
                && i.Transaction.Direction == TransactionDirection.Debit
                && i.Transaction.Treatment == Treatment.Spending
                && i.Transaction.Review is null)
            .Select(i => i.Transaction)
            .ToList();

        if (outgoing.Count == 0)
        {
            return 0;
        }

        var incoming = imported.Select(i => i.Transaction).Where(t => t.Direction == TransactionDirection.Credit)
            .Concat(await bankTransactions.ListCreditsAsync(
                userId, window.From.AddDays(-TransferPairMatcher.MaxDaysApart), window.To, cancellationToken))
            .DistinctBy(t => t.Id);

        var transfers = TransferPairMatcher.FindOutgoingTransfers(
            outgoing.Select(ToMovement), incoming.Select(ToMovement));

        var flagged = 0;
        foreach (var transaction in outgoing.Where(t => transfers.ContainsKey(t.Id)))
        {
            var target = transfers[transaction.Id];
            var counterparty = new CounterpartyAccount(target, context.AccountKinds.GetValueOrDefault(target, AccountKind.Other));
            var classification = TransactionClassifier.Classify(
                transaction.Direction, transaction.BankSubType ?? BankSubType.None, transaction.Amount.Currency,
                new AccountContext(true, context.BudgetCurrency), counterparty, context.BudgetCurrency);

            transaction.LinkCounterparty(target, now);
            transaction.Reclassify(classification, context.SavingsCategoryId, now);
            flagged += transaction.Review is null ? 0 : 1;
        }

        return flagged;
    }

    // BR-D1 to BR-D7, from the bank side.
    private async Task<int> FlagDuplicatesAsync(
        Guid userId, List<(Transaction Transaction, BankTransactionFacts Facts)> imported,
        HashSet<AffectedSpending> affected, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var spending = imported
            .Select(i => i.Transaction)
            .Where(t => t.Direction == TransactionDirection.Debit && t.Treatment == Treatment.Spending && t.Review is null)
            .ToList();

        if (spending.Count == 0)
        {
            return 0;
        }

        var from = spending.Min(t => t.Date).AddDays(-DuplicateMatcher.MaxDaysApart);
        var to = spending.Max(t => t.Date).AddDays(DuplicateMatcher.MaxDaysApart);
        var manual = (await bankTransactions.ListUnmatchedManualAsync(userId, from, to, cancellationToken)).ToList();
        var flagged = 0;

        // A new bank transaction has never been rejected with anything.
        var noRejections = new HashSet<Guid>();

        foreach (var bank in spending)
        {
            var matchId = DuplicateMatcher.FindMatch(ToCandidate(bank), manual.Select(ToCandidate), noRejections);
            if (matchId is null)
            {
                continue;
            }

            var match = manual.Single(m => m.Id == matchId);
            bank.FlagPossibleDuplicate(match, now);
            manual.Remove(match);
            affected.Add(new AffectedSpending(match.CategoryId, match.Date));
            flagged++;
        }

        return flagged;
    }

    private async Task ReleaseDuplicateReviewAsync(Transaction bank, CancellationToken cancellationToken)
    {
        if (bank is { Review: ReviewReason.PossibleDuplicate, ReviewCandidateId: { } manualId }
            && await transactions.GetAsync(bank.UserId, manualId, cancellationToken) is { } manual)
        {
            bank.RejectDuplicate(manual, clock.Now);
        }
    }

    private async Task<IReadOnlyList<BankAccount>> OwnAccountsAsync(Guid userId, CancellationToken cancellationToken) =>
        (await connections.ListAsync(userId, cancellationToken))
            .Where(c => c.Status != ConnectionStatus.Revoked)
            .SelectMany(c => c.Accounts)
            .ToList();

    private static MatchCandidate ToCandidate(Transaction t) => new(t.Id, t.Amount, t.Date, t.MerchantKey);

    private static TransferPairMatcher.Movement ToMovement(Transaction t) => new(t.Id, t.Bank!.AccountId, t.Amount, t.Date);

    internal static BankAccountSnapshot ToSnapshot(BankAccountData data)
    {
        var currency = Currency.From(data.Currency);
        return new BankAccountSnapshot(
            data.ProviderAccountId,
            data.Kind,
            currency,
            Iban.TryFrom(data.Iban),
            data.Nickname,
            data.Balance is { } balance ? Money.Of(decimal.Round(balance, 2), currency) : null,
            data.BalanceAsOf);
    }

    internal static BankTransactionFacts ToFacts(BankTransactionData data)
    {
        var currency = Currency.From(data.Currency);
        return new BankTransactionFacts(
            data.ProviderTransactionId,
            Money.Of(decimal.Round(data.Amount, 2), currency),
            data.Direction,
            data.Status,
            data.Date,
            data.MerchantName,
            data.ProviderCategoryCode is { } code ? new ProviderCategory(code, data.ProviderCategoryConfidence ?? 0) : null,
            data.SubType,
            Iban.TryFrom(data.CounterpartyIban),
            data.Fee is > 0 ? Money.Of(decimal.Round(data.Fee.Value, 2), currency) : null,
            data.IsCashWithdrawal);
    }

    private sealed record ImportContext(
        IReadOnlyList<CategoryChoice> Categories,
        IReadOnlyDictionary<MerchantKey, Guid> Rules,
        IReadOnlyDictionary<Iban, CounterpartyAccount> OwnAccounts,
        IReadOnlyDictionary<Guid, AccountKind> AccountKinds,
        Guid SavingsCategoryId,
        Currency BudgetCurrency);
}
