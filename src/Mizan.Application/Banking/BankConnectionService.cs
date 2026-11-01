using Microsoft.Extensions.Logging;
using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Domain.Banking;
using Mizan.Domain.Categories;
using Mizan.Domain.Importing;
using Mizan.Domain.Transactions;

namespace Mizan.Application.Banking;

public sealed record LinkSessionDto(
    string AppToken,
    string CustomerId,
    string AccessToken,
    bool Sandbox,
    IReadOnlyList<string> Permissions,
    DateTimeOffset AccessTo,
    string SuccessRedirectUrl,
    string FailRedirectUrl);

public sealed record BankAccountDto(
    Guid Id,
    string Kind,
    string Currency,
    string? IbanMasked,
    string Nickname,
    bool IncludeInBudget,
    MoneyDto? Balance,
    DateTimeOffset? BalanceAsOf);

public sealed record BankConnectionDto(
    Guid Id,
    string BankIdentifier,
    string BankName,
    string Status,
    DateTimeOffset ConsentExpiresAt,
    int ConsentDaysLeft,
    DateTimeOffset? LastSyncedAt,
    bool HistoryImported,
    IReadOnlyList<BankAccountDto> Accounts);

public sealed record SyncResultDto(Guid ConnectionId, string Status, int Added, int Updated, int NeedReview);

// What the provider told us through a webhook, already translated from its format.
public abstract record BankingSignal
{
    public sealed record EntityConnected(string ProviderCustomerId, string EntityId) : BankingSignal;

    public sealed record DataReady(string EntityId) : BankingSignal;

    public sealed record ConsentChanged(string EntityId) : BankingSignal;
}

// UC-70 to UC-76, UC-81, UC-82.
public sealed class BankConnectionService(
    IBankingProvider provider,
    IBankingCustomerRepository customers,
    IBankConnectionRepository connections,
    IUserRepository users,
    ICategoryRepository categories,
    ITransactionRepository transactions,
    IBankTransactionRepository bankTransactions,
    BankImportService importer,
    IUnitOfWork unitOfWork,
    Clock clock,
    ILogger<BankConnectionService> logger)
{
    // UC-70: everything the app needs to open the provider's link screen.
    public async Task<LinkSessionDto> CreateLinkSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var customer = await GetOrCreateCustomerAsync(userId, cancellationToken);
        var session = await provider.CreateLinkSessionAsync(customer.ProviderCustomerId, cancellationToken);

        return new LinkSessionDto(session.AppToken, session.CustomerId, session.AccessToken, session.Sandbox,
            session.Permissions, session.AccessTo, session.SuccessRedirectUrl, session.FailRedirectUrl);
    }

    // UC-72
    public async Task<IReadOnlyList<BankConnectionDto>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        (await connections.ListAsync(userId, cancellationToken))
            .Where(c => c.Status != ConnectionStatus.Revoked)
            .Select(ToDto)
            .ToList();

    // Called by the app right after the link screen closes: picks up new links without waiting
    // for the webhook (which can't reach a developer's machine), then imports them.
    public async Task<IReadOnlyList<BankConnectionDto>> DiscoverAsync(Guid userId, CancellationToken cancellationToken)
    {
        var customer = await customers.GetAsync(userId, cancellationToken);
        if (customer is not null)
        {
            foreach (var entity in await provider.ListEntitiesAsync(customer.ProviderCustomerId, cancellationToken))
            {
                var connection = await ActivateAsync(userId, entity, cancellationToken);
                if (connection is { IsActive: true, HistoryImported: false })
                {
                    await TryImportAsync(connection.Id, cancellationToken);
                }
            }
        }

        return await ListAsync(userId, cancellationToken);
    }

    // UC-82: "refresh now". Imports what the provider already has, then asks the bank for fresh data
    // (at most every 4 hours, BR-S2). In that order on purpose: while the provider rebuilds its data
    // after a refresh request, its data calls time out (seen in the sandbox). The fresh data arrives
    // with the "data ready" webhook, or with the next sync.
    public async Task<SyncResultDto> SyncAsync(Guid userId, Guid connectionId, CancellationToken cancellationToken)
    {
        var connection = await connections.GetAsync(userId, connectionId, cancellationToken)
            ?? throw new NotFoundException("Bank connection");

        if (connection.IsRefreshing(clock.Now))
        {
            // The provider is still preparing the data we asked for; the "data ready" webhook imports it.
            return new SyncResultDto(connection.Id, "Refreshing", 0, 0, 0);
        }

        var result = connection.IsActive ? await importer.ImportAsync(connection.Id, cancellationToken) : null;
        await RequestRefreshIfAllowedAsync(connection, cancellationToken);

        return new SyncResultDto(connection.Id, connection.Status.ToString(),
            result?.Added ?? 0, result?.Updated ?? 0, result?.NeedReview ?? 0);
    }

    // OB-6: the app calls this when it opens. Errors in one bank don't block the others.
    public async Task<IReadOnlyList<SyncResultDto>> SyncAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var results = new List<SyncResultDto>();
        foreach (var connection in (await connections.ListAsync(userId, cancellationToken)).Where(c => c.IsActive))
        {
            try
            {
                results.Add(await SyncAsync(userId, connection.Id, cancellationToken));
            }
            catch (BankingProviderException exception)
            {
                logger.LogWarning(exception, "Sync failed for connection {ConnectionId}", connection.Id);
                results.Add(new SyncResultDto(connection.Id, "Failed", 0, 0, 0));
            }
        }

        return results;
    }

    // UC-74 (BR-C8): the provider must confirm before we mark it removed.
    public async Task RevokeAsync(Guid userId, Guid connectionId, bool keepTransactions, CancellationToken cancellationToken)
    {
        var connection = await connections.GetAsync(userId, connectionId, cancellationToken)
            ?? throw new NotFoundException("Bank connection");

        if (connection.IsActive && await customers.GetAsync(userId, cancellationToken) is { } customer)
        {
            await provider.RevokeConsentAsync(customer.ProviderCustomerId, connection.ProviderEntityId, cancellationToken);
        }

        connection.Revoke(keepTransactions);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // BR-X10 and BR-X13: the choice also applies to what this account already brought in,
    // so "include this account" makes its transactions count right away.
    public async Task<BankConnectionDto> SetAccountIncludedAsync(
        Guid userId, Guid accountId, bool include, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User");
        var all = await connections.ListAsync(userId, cancellationToken);
        var connection = all.SingleOrDefault(c => c.Accounts.Any(a => a.Id == accountId))
            ?? throw new NotFoundException("Bank account");

        connection.SetAccountIncluded(accountId, include, user.Currency);

        var account = connection.Accounts.Single(a => a.Id == accountId);
        var kinds = all.SelectMany(c => c.Accounts).ToDictionary(a => a.Id, a => a.Kind);
        var savings = await categories.GetSystemAsync(userId, CategoryKind.Savings, cancellationToken);

        foreach (var transaction in await bankTransactions.ListByAccountAsync(accountId, cancellationToken))
        {
            var counterparty = transaction.CounterpartyAccountId is { } other
                ? new CounterpartyAccount(other, kinds.GetValueOrDefault(other, AccountKind.Other))
                : null;

            var classification = TransactionClassifier.Classify(
                transaction.Direction, transaction.BankSubType ?? BankSubType.None, transaction.Amount.Currency,
                new AccountContext(account.IncludeInBudget, account.Currency), counterparty, user.Currency);

            // A manual expense waiting next to it goes back to counting on its own.
            if (classification.Treatment == Treatment.OutsideBudget
                && transaction is { Review: ReviewReason.PossibleDuplicate, ReviewCandidateId: { } manualId }
                && await transactions.GetAsync(userId, manualId, cancellationToken) is { } manual)
            {
                transaction.RejectDuplicate(manual, clock.Now);
            }

            transaction.Reclassify(classification, savings.Id, clock.Now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(connection);
    }

    // Webhooks (UC-71, UC-75, UC-80), processed in the background from the inbox.
    public async Task HandleSignalAsync(BankingSignal signal, CancellationToken cancellationToken)
    {
        switch (signal)
        {
            case BankingSignal.EntityConnected connected:
                var customer = await customers.GetByProviderIdAsync(connected.ProviderCustomerId, cancellationToken);
                if (customer is null)
                {
                    logger.LogWarning("Webhook for unknown customer {CustomerId}", connected.ProviderCustomerId);
                    return;
                }

                var entity = await provider.GetEntityAsync(customer.ProviderCustomerId, connected.EntityId, cancellationToken);
                if (entity is not null)
                {
                    // Don't import yet: the provider is still collecting data (it sends DataReady when done).
                    await ActivateAsync(customer.UserId, entity, cancellationToken);
                }

                return;

            case BankingSignal.DataReady ready:
                if (await connections.GetByEntityIdAsync(ready.EntityId, cancellationToken) is { IsActive: true } connection)
                {
                    await importer.ImportAsync(connection.Id, cancellationToken);
                }

                return;

            case BankingSignal.ConsentChanged changed:
                await RefreshConsentAsync(changed.EntityId, cancellationToken);
                return;
        }
    }

    // UC-81 and BR-C5: once a day for every active connection.
    public async Task RunDailyAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        var connection = await connections.GetByIdAsync(connectionId, cancellationToken);
        if (connection is null)
        {
            return;
        }

        connection.CheckConsent(clock.Now, clock.Today);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (connection.IsActive)
        {
            await importer.ImportAsync(connection.Id, cancellationToken);
            await RequestRefreshIfAllowedAsync(connection, cancellationToken);
        }
    }

    private async Task<BankConnection?> ActivateAsync(Guid userId, ProviderEntity entity, CancellationToken cancellationToken)
    {
        var existing = await connections.GetByEntityIdAsync(entity.EntityId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var consent = Consent.Of(entity.ConsentGrantedAt, entity.ConsentExpiresAt);
        var previous = await connections.GetLatestForBankAsync(userId, entity.BankIdentifier, cancellationToken);

        BankConnection connection;
        if (previous is { Status: ConnectionStatus.Expired } || previous is { IsActive: true })
        {
            // UC-73 and BR-C2: the same bank continues on the same connection.
            previous.Reconnect(entity.EntityId, consent);
            connection = previous;
        }
        else
        {
            connection = BankConnection.Connect(userId, entity.EntityId, entity.BankIdentifier, entity.BankName, consent, clock.Now);
            connections.Add(connection);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return connection;
    }

    private async Task RefreshConsentAsync(string entityId, CancellationToken cancellationToken)
    {
        var connection = await connections.GetByEntityIdAsync(entityId, cancellationToken);
        if (connection is null || connection.Status == ConnectionStatus.Revoked)
        {
            return;
        }

        var customer = await customers.GetAsync(connection.UserId, cancellationToken);
        var entity = customer is null ? null : await provider.GetEntityAsync(customer.ProviderCustomerId, entityId, cancellationToken);

        if (entity is null || entity.ConsentExpiresAt <= clock.Now)
        {
            connection.Expire();
        }
        else if (!connection.IsActive)
        {
            connection.Reconnect(entityId, Consent.Of(entity.ConsentGrantedAt, entity.ConsentExpiresAt));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task RequestRefreshIfAllowedAsync(BankConnection connection, CancellationToken cancellationToken)
    {
        if (!connection.CanRequestRefresh(clock.Now))
        {
            return;
        }

        try
        {
            await provider.RequestRefreshAsync(connection.ProviderEntityId, cancellationToken);
            connection.MarkRefreshRequested(clock.Now);
        }
        catch (ConsentExpiredException)
        {
            connection.Expire();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task TryImportAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        try
        {
            await importer.ImportAsync(connectionId, cancellationToken);
        }
        catch (BankingProviderException exception)
        {
            // The connection is saved; the next sync will try again.
            logger.LogWarning(exception, "First import failed for connection {ConnectionId}", connectionId);
        }
    }

    private async Task<BankingCustomer> GetOrCreateCustomerAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (await customers.GetAsync(userId, cancellationToken) is { } existing)
        {
            return existing;
        }

        _ = await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User");
        var providerId = await provider.CreateCustomerAsync(userId, cancellationToken);
        var customer = BankingCustomer.Create(userId, provider.Name, providerId, clock.Now);

        customers.Add(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return customer;
    }

    private BankConnectionDto ToDto(BankConnection connection) => new(
        connection.Id,
        connection.BankIdentifier,
        connection.BankName,
        connection.Status.ToString(),
        connection.Consent.ExpiresAt,
        connection.Consent.DaysUntilExpiry(clock.Today),
        connection.LastSyncedAt,
        connection.HistoryImported,
        connection.Accounts.Select(a => new BankAccountDto(
            a.Id,
            a.Kind.ToString(),
            a.Currency.Code,
            a.Iban?.Masked,
            a.Nickname,
            a.IncludeInBudget,
            a.Balance is null ? null : MoneyDto.From(a.Balance),
            a.BalanceAsOf)).ToList());
}
