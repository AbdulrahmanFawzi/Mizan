using Mizan.Domain.Budgets;
using Mizan.Domain.Categories;
using Mizan.Domain.Common;
using static Mizan.Domain.Tests.TestData;

namespace Mizan.Domain.Tests.Budgets;

public class BudgetTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid Fuel = Guid.NewGuid();
    private static readonly Guid Food = Guid.NewGuid();
    private static readonly Guid Savings = Guid.NewGuid();

    // 27 Oct -> 26 Nov 2026, Today (30 Oct) is inside.
    private static readonly BudgetPeriod Period = BudgetPeriod.Containing(Today, PeriodStartDay.From(27));

    private static Budget NewBudget()
    {
        var budget = Budget.Create(UserId, Period, Sar(10_000));
        budget.SetLimit(Fuel, Sar(500));
        budget.SetLimit(Food, Sar(1_000));
        budget.SetLimit(Savings, Sar(2_000));
        return budget;
    }

    private static IEnumerable<AlertThreshold> Reached(Budget budget) =>
        budget.DomainEvents.OfType<BudgetThresholdReached>().Select(e => e.Threshold);

    // --- Limits and totals ---

    [Fact]
    public void SetLimit_NegativeLimit_Throws()
    {
        Assert.Throws<DomainException>(() => NewBudget().SetLimit(Fuel, Sar(-1)));
    }

    [Fact]
    public void SetLimit_ExistingCategory_UpdatesInsteadOfDuplicating()
    {
        var budget = NewBudget();

        budget.SetLimit(Fuel, Sar(600));

        Assert.Equal(3, budget.Allocations.Count);
        Assert.Equal(Sar(600), budget.Allocations.Single(a => a.CategoryId == Fuel).Limit);
    }

    [Fact]
    public void Unallocated_IsIncomeMinusLimits()
    {
        var budget = NewBudget();

        Assert.Equal(Sar(3_500), budget.TotalAllocated);
        Assert.Equal(Sar(6_500), budget.Unallocated);
        Assert.False(budget.IsOverAllocated);
    }

    [Fact]
    public void OverAllocation_IsAllowedButFlagged()
    {
        var budget = NewBudget();

        budget.SetLimit(Food, Sar(8_000));

        Assert.True(budget.IsOverAllocated);
        Assert.Equal(Sar(-500), budget.Unallocated);
    }

    // --- Status (BR-B6..B8) ---

    [Fact]
    public void StatusFor_CalculatesSpentRemainingAndPercent()
    {
        var budget = NewBudget();
        var spent = new Dictionary<Guid, Money> { [Fuel] = Sar(50) };

        var fuel = budget.StatusFor(spent).Single(s => s.CategoryId == Fuel);
        var food = budget.StatusFor(spent).Single(s => s.CategoryId == Food);

        Assert.Equal(Sar(450), fuel.Remaining);
        Assert.Equal(10m, fuel.PercentUsed);
        Assert.Equal(Sar(0), food.Spent);
        Assert.Equal(0m, food.PercentUsed);
    }

    [Fact]
    public void StatusFor_OverBudget_HasNegativeRemaining()
    {
        var budget = NewBudget();

        var fuel = budget.StatusFor(new Dictionary<Guid, Money> { [Fuel] = Sar(580) }).Single(s => s.CategoryId == Fuel);

        Assert.Equal(Sar(-80), fuel.Remaining);
        Assert.Equal(116m, fuel.PercentUsed);
        Assert.True(fuel.IsOverBudget);
    }

    // --- Thresholds (BR-A1..A5) ---

    [Theory]
    [InlineData(399, null)]
    [InlineData(400, 80)]
    [InlineData(450, 90)]
    [InlineData(500, 100)]
    public void EvaluateThresholds_RaisesReachedThreshold(decimal spent, int? expected)
    {
        var budget = NewBudget();

        budget.EvaluateThresholds(Fuel, CategoryKind.Regular, Sar(spent), Today);

        Assert.Equal(expected, Reached(budget).SingleOrDefault()?.Percent);
    }

    [Fact]
    public void EvaluateThresholds_SameThresholdTwice_RaisesOnce()
    {
        var budget = NewBudget();

        budget.EvaluateThresholds(Fuel, CategoryKind.Regular, Sar(400), Today);
        budget.EvaluateThresholds(Fuel, CategoryKind.Regular, Sar(420), Today);

        Assert.Single(Reached(budget));
    }

    [Fact]
    public void EvaluateThresholds_DropAndRiseAgain_DoesNotRepeat()
    {
        var budget = NewBudget();

        budget.EvaluateThresholds(Fuel, CategoryKind.Regular, Sar(400), Today);
        budget.EvaluateThresholds(Fuel, CategoryKind.Regular, Sar(100), Today); // a transaction was deleted
        budget.EvaluateThresholds(Fuel, CategoryKind.Regular, Sar(400), Today);

        Assert.Single(Reached(budget));
    }

    [Fact]
    public void EvaluateThresholds_JumpOverSeveral_RaisesOnlyHighest()
    {
        var budget = NewBudget();

        budget.EvaluateThresholds(Fuel, CategoryKind.Regular, Sar(350), Today); // 70%
        budget.EvaluateThresholds(Fuel, CategoryKind.Regular, Sar(525), Today); // 105%

        Assert.Equal([AlertThreshold.Hundred], Reached(budget));
    }

    [Fact]
    public void EvaluateThresholds_ClimbingStepByStep_RaisesEach()
    {
        var budget = NewBudget();

        foreach (var spent in new[] { 400m, 450m, 500m })
        {
            budget.EvaluateThresholds(Fuel, CategoryKind.Regular, Sar(spent), Today);
        }

        Assert.Equal([80, 90, 100], Reached(budget).Select(t => t.Percent));
    }

    [Fact]
    public void EvaluateThresholds_SavingsCategory_NeverAlerts()
    {
        var budget = NewBudget();

        budget.EvaluateThresholds(Savings, CategoryKind.Savings, Sar(5_000), Today);

        Assert.Empty(Reached(budget));
    }

    [Fact]
    public void EvaluateThresholds_OutsideCurrentPeriod_DoesNotAlert()
    {
        var budget = NewBudget();

        budget.EvaluateThresholds(Fuel, CategoryKind.Regular, Sar(500), D("2026-12-01"));

        Assert.Empty(Reached(budget));
    }

    [Fact]
    public void EvaluateThresholds_ZeroLimit_AnySpendingIsOverBudget()
    {
        var budget = NewBudget();
        var other = Guid.NewGuid();
        budget.SetLimit(other, Sar(0));

        budget.EvaluateThresholds(other, CategoryKind.Other, Sar(1), Today);

        Assert.Equal([AlertThreshold.Hundred], Reached(budget));
    }

    // --- Next period (BR-P6) ---

    [Fact]
    public void CreateNext_CopiesLimitsAndResetsAlerts()
    {
        var budget = NewBudget();
        budget.EvaluateThresholds(Fuel, CategoryKind.Regular, Sar(500), Today);

        var next = budget.CreateNext(Period.Next(PeriodStartDay.From(27)), Sar(11_000));
        next.EvaluateThresholds(Fuel, CategoryKind.Regular, Sar(400), D("2026-11-28"));

        Assert.Equal(Sar(11_000), next.PlannedIncome);
        Assert.Equal(budget.TotalAllocated, next.TotalAllocated);
        Assert.Equal([AlertThreshold.Eighty], Reached(next));
    }

    [Fact]
    public void CreateNext_OverlappingPeriod_Throws()
    {
        var budget = NewBudget();

        Assert.Throws<DomainException>(() => budget.CreateNext(Period, Sar(10_000)));
    }
}
