using Microsoft.EntityFrameworkCore;
using Mizan.Application.Abstractions;
using Mizan.Domain.Transactions;
using Mizan.Infrastructure.Persistence.Configurations;

namespace Mizan.Infrastructure.Persistence.Repositories;

internal sealed class TransactionRepository(MizanDbContext db) : ITransactionRepository
{
    public Task<Transaction?> GetAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken) =>
        db.Transactions.SingleOrDefaultAsync(t => t.UserId == userId && t.Id == transactionId, cancellationToken);

    public async Task<(IReadOnlyList<Transaction> Items, int TotalCount)> ListAsync(
        Guid userId, TransactionFilter filter, CancellationToken cancellationToken)
    {
        var query = db.Transactions.AsNoTracking().Where(t => t.UserId == userId);

        if (filter.From is { } from)
        {
            query = query.Where(t => t.Date >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(t => t.Date <= to);
        }

        if (filter.CategoryId is { } categoryId)
        {
            query = query.Where(t => t.CategoryId == categoryId);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Transaction>> ListByCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken) =>
        await db.Transactions.Where(t => t.UserId == userId && t.CategoryId == categoryId).ToListAsync(cancellationToken);

    // Sums each transaction's budget effect (bank transfers, exclusions and pending reviews count 0,
    // refunds count negative). A category never goes below zero because of refunds (BR-F4).
    public async Task<IReadOnlyDictionary<Guid, decimal>> SumByCategoryAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        (await db.Transactions
            .Where(t => t.UserId == userId && t.Date >= from && t.Date <= to)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => EF.Property<decimal>(t, TransactionConfiguration.BudgetAmount)) })
            .ToListAsync(cancellationToken))
        .ToDictionary(x => x.CategoryId, x => Math.Max(0m, x.Total));

    public async Task<IReadOnlyDictionary<Guid, decimal>> SumByGoalAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.Transactions
            .Where(t => t.UserId == userId && t.GoalId != null)
            .GroupBy(t => t.GoalId!.Value)
            .Select(g => new { GoalId = g.Key, Total = g.Sum(t => t.Amount.Amount) })
            .ToDictionaryAsync(x => x.GoalId, x => x.Total, cancellationToken);

    public void Add(Transaction transaction) => db.Transactions.Add(transaction);

    public void Remove(Transaction transaction) => db.Transactions.Remove(transaction);
}
