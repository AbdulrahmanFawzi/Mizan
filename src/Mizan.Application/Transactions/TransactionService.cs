using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Domain.Categories;
using Mizan.Domain.Common;
using Mizan.Domain.Transactions;

namespace Mizan.Application.Transactions;

public sealed record TransactionDto(
    Guid Id,
    MoneyDto Amount,
    Guid CategoryId,
    DateOnly Date,
    string? Merchant,
    Guid? GoalId);

public sealed record RecordExpenseCommand(decimal Amount, Guid CategoryId, DateOnly Date, string? Merchant = null);

public sealed record TransactionQuery(DateOnly? From, DateOnly? To, Guid? CategoryId, int Page = 1, int PageSize = 20);

public sealed class TransactionService(
    IUserRepository users,
    ICategoryRepository categories,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    Clock clock)
{
    public const int MaxPageSize = 100;

    // UC-33
    public async Task<PagedResult<TransactionDto>> ListAsync(Guid userId, TransactionQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > MaxPageSize)
        {
            throw new ValidationException($"Page must be at least 1 and page size between 1 and {MaxPageSize}.");
        }

        if (query.From > query.To)
        {
            throw new ValidationException("'from' must not be after 'to'.");
        }

        var filter = new TransactionFilter(query.From, query.To, query.CategoryId, query.Page, query.PageSize);
        var (items, total) = await transactions.ListAsync(userId, filter, cancellationToken);
        return new PagedResult<TransactionDto>(items.Select(ToDto).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<TransactionDto> GetAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken) =>
        ToDto(await GetTransactionAsync(userId, transactionId, cancellationToken));

    // UC-30
    public async Task<TransactionDto> RecordAsync(Guid userId, RecordExpenseCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User");
        await EnsureCategoryExistsAsync(userId, command.CategoryId, cancellationToken);

        var transaction = Transaction.Record(
            userId,
            Money.Of(command.Amount, user.Currency),
            command.CategoryId,
            command.Date,
            command.Merchant,
            goalId: null,
            clock.Today,
            clock.Now);

        transactions.Add(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(transaction);
    }

    // UC-31
    public async Task<TransactionDto> UpdateAsync(Guid userId, Guid transactionId, RecordExpenseCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User");
        var transaction = await GetTransactionAsync(userId, transactionId, cancellationToken);
        var category = await EnsureCategoryExistsAsync(userId, command.CategoryId, cancellationToken);

        // A goal contribution must stay in the Savings category (D4).
        if (transaction.GoalId is not null && category.Kind != CategoryKind.Savings)
        {
            throw new DomainException("A goal contribution must stay in the Savings category.");
        }

        transaction.Update(
            Money.Of(command.Amount, user.Currency),
            command.CategoryId,
            command.Date,
            command.Merchant,
            clock.Today,
            clock.Now);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(transaction);
    }

    // UC-32
    public async Task DeleteAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken)
    {
        var transaction = await GetTransactionAsync(userId, transactionId, cancellationToken);
        transaction.Delete();
        transactions.Remove(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Transaction> GetTransactionAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken) =>
        await transactions.GetAsync(userId, transactionId, cancellationToken) ?? throw new NotFoundException("Transaction");

    // BR-T2: the category must belong to the same user.
    private async Task<Category> EnsureCategoryExistsAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken) =>
        await categories.GetAsync(userId, categoryId, cancellationToken) ?? throw new NotFoundException("Category");

    internal static TransactionDto ToDto(Transaction transaction) => new(
        transaction.Id,
        MoneyDto.From(transaction.Amount),
        transaction.CategoryId,
        transaction.Date,
        transaction.Merchant,
        transaction.GoalId);
}
