using Mizan.Domain.Common;

namespace Mizan.Domain.Budgets;

public sealed class BudgetAllocation
{
    public Guid CategoryId { get; private set; }
    public Money Limit { get; private set; } = null!;
    public AlertThreshold? HighestThresholdSent { get; private set; }

    private BudgetAllocation()
    {
    }

    internal BudgetAllocation(Guid categoryId, Money limit)
    {
        CategoryId = categoryId;
        ChangeLimit(limit);
    }

    internal void ChangeLimit(Money limit)
    {
        // BR-B2
        if (limit.IsNegative)
        {
            throw new DomainException("A category limit cannot be negative.");
        }

        Limit = limit;
    }

    // Returns the threshold to announce, or null if nothing new was reached (BR-A2, BR-A3).
    internal AlertThreshold? RegisterSpending(Money spent)
    {
        var reached = Limit.IsZero
            ? (spent.IsPositive ? AlertThreshold.Hundred : null)
            : AlertThreshold.HighestReachedBy(spent.Amount / Limit.Amount * 100m);

        if (reached is null || (HighestThresholdSent is not null && reached.Percent <= HighestThresholdSent.Percent))
        {
            return null;
        }

        HighestThresholdSent = reached;
        return reached;
    }
}
