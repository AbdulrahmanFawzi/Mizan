using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Domain.Categories;
using Mizan.Domain.Categorization;
using Mizan.Domain.Common;
using Mizan.Domain.Importing;
using Mizan.Domain.Transactions;

namespace Mizan.Application.Transactions;

public enum ReviewDecision
{
    SameAsManual,   // OB-1: merge
    NotDuplicate,   // OB-1: both are real
    OwnTransfer,    // OB-3: money moved between my accounts
    Spending,       // OB-3: it was real spending
}

public sealed record ReviewItemDto(TransactionDto Transaction, string Reason, TransactionDto? Candidate);

public sealed record ChangeCategoryCommand(Guid CategoryId, bool RememberMerchant = false);

public sealed record SetExcludedCommand(bool Excluded);

public sealed record SetNoteCommand(string? Note);

// UC-90 to UC-94: what the user does with bank transactions.
public sealed class ReviewService(
    IUserRepository users,
    ICategoryRepository categories,
    ITransactionRepository transactions,
    IBankTransactionRepository bankTransactions,
    IMerchantRuleRepository merchantRules,
    IRejectedMatchRepository rejectedMatches,
    IBudgetRepository budgets,
    IUnitOfWork unitOfWork,
    Clock clock)
{
    public async Task<IReadOnlyList<ReviewItemDto>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        var items = new List<ReviewItemDto>();
        foreach (var transaction in await bankTransactions.ListNeedingReviewAsync(userId, cancellationToken))
        {
            var candidate = transaction.ReviewCandidateId is { } id
                ? await transactions.GetAsync(userId, id, cancellationToken)
                : null;

            items.Add(new ReviewItemDto(
                TransactionService.ToDto(transaction),
                transaction.Review!.Value.ToString(),
                candidate is null ? null : TransactionService.ToDto(candidate)));
        }

        return items;
    }

    public async Task<TransactionDto> ResolveAsync(Guid userId, Guid transactionId, ReviewDecision decision, CancellationToken cancellationToken)
    {
        var transaction = await GetAsync(userId, transactionId, cancellationToken);

        switch (decision)
        {
            case ReviewDecision.SameAsManual or ReviewDecision.NotDuplicate:
                var manual = transaction.ReviewCandidateId is { } manualId
                    ? await transactions.GetAsync(userId, manualId, cancellationToken)
                    : null;

                if (manual is null || transaction.Review != ReviewReason.PossibleDuplicate)
                {
                    throw new DomainException("This transaction is not waiting to be compared.");
                }

                if (decision == ReviewDecision.SameAsManual)
                {
                    transaction.ConfirmDuplicate(manual, clock.Now);    // BR-D5
                    transactions.Remove(manual);
                }
                else
                {
                    transaction.RejectDuplicate(manual, clock.Now);     // BR-D6
                    rejectedMatches.Add(new RejectedMatch(transaction.Id, manual.Id));
                }

                break;

            case ReviewDecision.OwnTransfer or ReviewDecision.Spending:
                transaction.ResolveTransfer(decision == ReviewDecision.OwnTransfer, clock.Now);
                break;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TransactionService.ToDto(transaction);
    }

    // UC-91, BR-K2: optionally remember the merchant and fix this period's automatic guesses for it.
    public async Task<TransactionDto> ChangeCategoryAsync(
        Guid userId, Guid transactionId, ChangeCategoryCommand command, CancellationToken cancellationToken)
    {
        var transaction = await GetAsync(userId, transactionId, cancellationToken);
        var category = await categories.GetAsync(userId, command.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Category");

        // D4 (MVP 0): a goal contribution stays in Savings.
        if (transaction.GoalId is not null && category.Kind != CategoryKind.Savings)
        {
            throw new DomainException("A goal contribution must stay in the Savings category.");
        }

        transaction.Recategorize(category.Id, CategorySource.User, clock.Now);

        if (command.RememberMerchant)
        {
            await RememberMerchantAsync(userId, transaction, category.Id, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TransactionService.ToDto(transaction);
    }

    // UC-92
    public async Task<TransactionDto> SetExcludedAsync(Guid userId, Guid transactionId, bool excluded, CancellationToken cancellationToken)
    {
        var transaction = await GetAsync(userId, transactionId, cancellationToken);

        if (excluded)
        {
            transaction.Exclude(clock.Now);
        }
        else
        {
            transaction.Include(clock.Now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TransactionService.ToDto(transaction);
    }

    public async Task<TransactionDto> SetNoteAsync(Guid userId, Guid transactionId, string? note, CancellationToken cancellationToken)
    {
        var transaction = await GetAsync(userId, transactionId, cancellationToken);
        transaction.SetNote(note, clock.Now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TransactionService.ToDto(transaction);
    }

    private async Task RememberMerchantAsync(Guid userId, Transaction transaction, Guid categoryId, CancellationToken cancellationToken)
    {
        if (transaction.MerchantKey is not { } merchant)
        {
            throw new ValidationException("This transaction has no merchant name to remember.");
        }

        var rule = await merchantRules.GetAsync(userId, merchant, cancellationToken);
        if (rule is null)
        {
            merchantRules.Add(MerchantRule.Create(userId, merchant, categoryId, clock.Now));
        }
        else
        {
            rule.ChangeCategory(categoryId);
        }

        // Same merchant earlier this period, where the user didn't choose the category themselves.
        var user = await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User");
        var budget = await budgets.GetContainingAsync(userId, clock.Today, cancellationToken);
        if (budget is null)
        {
            return;
        }

        var others = await bankTransactions.ListAutoCategorizedAsync(
            user.Id, merchant, budget.Period.Start, budget.Period.End, cancellationToken);

        foreach (var other in others.Where(o => o.Id != transaction.Id))
        {
            other.Recategorize(categoryId, CategorySource.MerchantRule, clock.Now);
        }
    }

    private async Task<Transaction> GetAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken) =>
        await transactions.GetAsync(userId, transactionId, cancellationToken) ?? throw new NotFoundException("Transaction");
}
