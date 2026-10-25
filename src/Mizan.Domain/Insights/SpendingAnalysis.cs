using Mizan.Domain.Budgets;
using Mizan.Domain.Categories;
using Mizan.Domain.Common;

namespace Mizan.Domain.Insights;

public static class SpendingAnalysis
{
    // BR-N5
    public const int MinimumDaysForForecast = 3;

    // BR-N1
    public static decimal? VariancePercent(Money planned, Money actual)
    {
        if (planned.IsZero)
        {
            return null;
        }

        var difference = actual - planned;
        return decimal.Round(difference.Amount / planned.Amount * 100m, 0, MidpointRounding.AwayFromZero);
    }

    // BR-N2, BR-N3
    public static Money? ProjectedSpending(Money spent, BudgetPeriod period, DateOnly today)
    {
        if (!period.Contains(today))
        {
            return null;
        }

        var daysElapsed = period.DaysElapsed(today);
        if (daysElapsed < MinimumDaysForForecast)
        {
            return null;
        }

        var projected = spent.Amount / daysElapsed * period.LengthInDays;
        return Money.Of(decimal.Round(projected, 2, MidpointRounding.AwayFromZero), spent.Currency);
    }

    // BR-N7: "you can spend today". Planned savings that haven't been saved yet are set aside first.
    // Rounded down to a whole riyal so it never suggests more than the user has. Never negative.
    public static Money? SafeToSpendToday(
        Money income,
        Money totalSpent,
        Money remainingPlannedSavings,
        BudgetPeriod period,
        DateOnly today)
    {
        if (!period.Contains(today))
        {
            return null;
        }

        var daysLeft = period.LengthInDays - period.DaysElapsed(today) + 1;
        var spendable = income - totalSpent - remainingPlannedSavings;

        if (!spendable.IsPositive)
        {
            return Money.Zero(income.Currency);
        }

        return Money.Of(Math.Floor(spendable.Amount / daysLeft), income.Currency);
    }

    public static IReadOnlyList<Insight> Generate(
        Budget budget,
        IReadOnlyDictionary<Guid, Money> spentByCategory,
        IReadOnlyDictionary<Guid, CategoryKind> categoryKinds,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(budget);

        var insights = new List<Insight>();

        foreach (var status in budget.StatusFor(spentByCategory))
        {
            // Saving more than planned is never a warning.
            if (categoryKinds.GetValueOrDefault(status.CategoryId) == CategoryKind.Savings)
            {
                continue;
            }

            if (status.IsOverBudget)
            {
                insights.Add(new OverBudgetInsight(
                    status.CategoryId,
                    status.Spent - status.Limit,
                    VariancePercent(status.Limit, status.Spent)));
                continue;
            }

            var projected = ProjectedSpending(status.Spent, budget.Period, today);
            if (projected is not null && projected > status.Limit)
            {
                insights.Add(new ProjectedOverBudgetInsight(status.CategoryId, projected, status.Limit));
            }
        }

        return insights;
    }
}
