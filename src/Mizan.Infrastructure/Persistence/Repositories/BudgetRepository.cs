using Microsoft.EntityFrameworkCore;
using Mizan.Application.Abstractions;
using Mizan.Domain.Budgets;

namespace Mizan.Infrastructure.Persistence.Repositories;

internal sealed class BudgetRepository(MizanDbContext db) : IBudgetRepository
{
    public async Task<Budget?> GetContainingAsync(Guid userId, DateOnly date, CancellationToken cancellationToken) =>
        // A budget added earlier in this same request is not in the database yet.
        db.Budgets.Local.SingleOrDefault(b => b.UserId == userId && b.Period.Contains(date))
        ?? await db.Budgets.SingleOrDefaultAsync(
            b => b.UserId == userId && b.Period.Start <= date && b.Period.End >= date,
            cancellationToken);

    public Task<Budget?> GetLatestAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Budgets
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.Period.Start)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Budget>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.Budgets.Where(b => b.UserId == userId).ToListAsync(cancellationToken);

    public void Add(Budget budget) => db.Budgets.Add(budget);
}
