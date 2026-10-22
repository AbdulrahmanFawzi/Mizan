using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Domain.Budgets;
using Mizan.Domain.Categories;
using Mizan.Domain.Common;

namespace Mizan.Application.Budgets;

// Loads what is needed to describe a budget: its categories and how much was spent in each.
public sealed class BudgetReader(ICategoryRepository categories, ITransactionRepository transactions)
{
    public async Task<BudgetSnapshot> ReadAsync(Budget budget, CancellationToken cancellationToken)
    {
        var categoryList = await categories.ListAsync(budget.UserId, cancellationToken);
        var sums = await transactions.SumByCategoryAsync(
            budget.UserId, budget.Period.Start, budget.Period.End, cancellationToken);

        var currency = budget.PlannedIncome.Currency;
        var spent = sums.ToDictionary(s => s.Key, s => Money.Of(s.Value, currency));

        return new BudgetSnapshot(budget, categoryList.ToDictionary(c => c.Id), spent);
    }
}

public sealed record BudgetSnapshot(
    Budget Budget,
    IReadOnlyDictionary<Guid, Category> Categories,
    IReadOnlyDictionary<Guid, Money> SpentByCategory)
{
    public IReadOnlyDictionary<Guid, CategoryKind> Kinds =>
        Categories.ToDictionary(c => c.Key, c => c.Value.Kind);

    public Money TotalSpent =>
        SpentByCategory.Values.Aggregate(Money.Zero(Budget.PlannedIncome.Currency), (total, m) => total + m);

    public BudgetDto ToDto(DateOnly today)
    {
        var statuses = Budget.StatusFor(SpentByCategory)
            .Where(s => Categories.ContainsKey(s.CategoryId))
            .Select(s => new CategoryStatusDto(
                s.CategoryId,
                Categories[s.CategoryId].Name.Value,
                Categories[s.CategoryId].Kind,
                MoneyDto.From(s.Limit),
                MoneyDto.From(s.Spent),
                MoneyDto.From(s.Remaining),
                s.PercentUsed,
                s.IsOverBudget))
            .OrderBy(s => s.Kind)
            .ThenBy(s => s.Name)
            .ToList();

        var period = Budget.Period;
        return new BudgetDto(
            Budget.Id,
            new PeriodDto(period.Start, period.End, period.LengthInDays, period.DaysElapsed(today)),
            MoneyDto.From(Budget.PlannedIncome),
            MoneyDto.From(Budget.TotalAllocated),
            MoneyDto.From(Budget.Unallocated),
            Budget.IsOverAllocated,
            MoneyDto.From(TotalSpent),
            statuses);
    }
}
