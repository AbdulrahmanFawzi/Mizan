using Mizan.Domain.Common;

namespace Mizan.Domain.Budgets;

public sealed record AllocationStatus(
    Guid CategoryId,
    Money Limit,
    Money Spent,
    Money Remaining,
    decimal? PercentUsed)
{
    public bool IsOverBudget => Remaining.IsNegative;
}
