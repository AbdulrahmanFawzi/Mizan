using Mizan.Domain.Categories;
using Mizan.Domain.Common;

namespace Mizan.Domain.Budgets;

public sealed class Budget : AggregateRoot
{
    private readonly List<BudgetAllocation> _allocations = [];

    public Guid UserId { get; private set; }
    public BudgetPeriod Period { get; private set; } = null!;
    public Money PlannedIncome { get; private set; } = null!;
    public IReadOnlyCollection<BudgetAllocation> Allocations => _allocations.AsReadOnly();

    private Budget()
    {
    }

    public static Budget Create(Guid userId, BudgetPeriod period, Money plannedIncome)
    {
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(plannedIncome);

        return new Budget
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Period = period,
            PlannedIncome = plannedIncome,
        };
    }

    // BR-P6: limits are copied, spending and sent alerts start from zero.
    public Budget CreateNext(BudgetPeriod period, Money plannedIncome)
    {
        ArgumentNullException.ThrowIfNull(period);

        if (period.Start <= Period.End)
        {
            throw new DomainException("The next budget must start after the current one ends.");
        }

        var next = Create(UserId, period, plannedIncome);
        foreach (var allocation in _allocations)
        {
            next.SetLimit(allocation.CategoryId, allocation.Limit);
        }

        return next;
    }

    public Money TotalAllocated =>
        _allocations.Aggregate(Money.Zero(PlannedIncome.Currency), (total, a) => total + a.Limit);

    // BR-B9
    public Money Unallocated => PlannedIncome - TotalAllocated;

    // BR-B5: allowed, but the user is warned.
    public bool IsOverAllocated => Unallocated.IsNegative;

    public bool HasAllocationFor(Guid categoryId) => _allocations.Any(a => a.CategoryId == categoryId);

    public void SetLimit(Guid categoryId, Money limit)
    {
        ArgumentNullException.ThrowIfNull(limit);
        EnsureSameCurrency(limit);

        var allocation = _allocations.SingleOrDefault(a => a.CategoryId == categoryId);
        if (allocation is null)
        {
            _allocations.Add(new BudgetAllocation(categoryId, limit));
        }
        else
        {
            allocation.ChangeLimit(limit);
        }
    }

    public void RemoveAllocation(Guid categoryId) =>
        _allocations.RemoveAll(a => a.CategoryId == categoryId);

    public void ChangePlannedIncome(Money plannedIncome)
    {
        ArgumentNullException.ThrowIfNull(plannedIncome);
        EnsureSameCurrency(plannedIncome);
        PlannedIncome = plannedIncome;
    }

    public void EvaluateThresholds(Guid categoryId, CategoryKind kind, Money spent, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(spent);

        // BR-A4: going over the savings plan is good news. BR-A5: only the current period alerts.
        if (kind == CategoryKind.Savings || !Period.Contains(today))
        {
            return;
        }

        var allocation = _allocations.SingleOrDefault(a => a.CategoryId == categoryId);
        var reached = allocation?.RegisterSpending(spent);

        if (reached is not null)
        {
            Raise(new BudgetThresholdReached(UserId, Id, categoryId, reached));
        }
    }

    // BR-B6, BR-B7, BR-B8
    public IReadOnlyList<AllocationStatus> StatusFor(IReadOnlyDictionary<Guid, Money> spentByCategory)
    {
        ArgumentNullException.ThrowIfNull(spentByCategory);

        return _allocations
            .Select(a =>
            {
                var spent = spentByCategory.GetValueOrDefault(a.CategoryId) ?? Money.Zero(a.Limit.Currency);
                decimal? percentUsed = a.Limit.IsZero
                    ? null
                    : decimal.Round(spent.Amount / a.Limit.Amount * 100m, 2, MidpointRounding.AwayFromZero);

                return new AllocationStatus(a.CategoryId, a.Limit, spent, a.Limit - spent, percentUsed);
            })
            .ToList();
    }

    private void EnsureSameCurrency(Money money)
    {
        if (PlannedIncome is not null && money.Currency != PlannedIncome.Currency)
        {
            throw new DomainException($"Budget amounts must be in {PlannedIncome.Currency}.");
        }
    }
}
