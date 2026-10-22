using Microsoft.EntityFrameworkCore;
using Mizan.Application.Abstractions;
using Mizan.Domain.Goals;

namespace Mizan.Infrastructure.Persistence.Repositories;

internal sealed class GoalRepository(MizanDbContext db) : IGoalRepository
{
    public Task<Goal?> GetAsync(Guid userId, Guid goalId, CancellationToken cancellationToken) =>
        db.Goals.SingleOrDefaultAsync(g => g.UserId == userId && g.Id == goalId, cancellationToken);

    public async Task<IReadOnlyList<Goal>> ListAsync(Guid userId, bool includeArchived, CancellationToken cancellationToken) =>
        await db.Goals
            .Where(g => g.UserId == userId && (includeArchived || !g.IsArchived))
            .OrderBy(g => g.TargetDate)
            .ToListAsync(cancellationToken);

    public void Add(Goal goal) => db.Goals.Add(goal);
}
