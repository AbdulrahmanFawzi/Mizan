using Mizan.Domain.Budgets;
using Mizan.Domain.Categories;
using Mizan.Domain.Common;
using Mizan.Domain.Insights;
using static Mizan.Domain.Tests.TestData;

namespace Mizan.Domain.Tests.Insights;

public class SpendingAnalysisTests
{
    // 1 Oct -> 31 Oct 2026 (31 days)
    private static readonly BudgetPeriod October = BudgetPeriod.Containing(D("2026-10-15"), PeriodStartDay.From(1));

    [Fact]
    public void VariancePercent_PrdExample()
    {
        // Food: budget 1,000, actual 1,270 -> 27% more (BR-N1)
        Assert.Equal(27m, SpendingAnalysis.VariancePercent(Sar(1_000), Sar(1_270)));
    }

    [Fact]
    public void VariancePercent_ZeroPlanned_IsNull()
    {
        Assert.Null(SpendingAnalysis.VariancePercent(Sar(0), Sar(50)));
    }

    [Fact]
    public void ProjectedSpending_UsesDailyAverage()
    {
        // 450 over 20 days = 22.5/day * 31 days = 697.50
        var projected = SpendingAnalysis.ProjectedSpending(Sar(450), October, D("2026-10-20"));

        Assert.Equal(Sar(697.50m), projected);
    }

    [Theory]
    [InlineData("2026-10-01")]
    [InlineData("2026-10-02")]
    public void ProjectedSpending_BeforeThreeDays_IsNull(string today)
    {
        Assert.Null(SpendingAnalysis.ProjectedSpending(Sar(200), October, D(today)));
    }

    [Fact]
    public void ProjectedSpending_OutsidePeriod_IsNull()
    {
        Assert.Null(SpendingAnalysis.ProjectedSpending(Sar(200), October, D("2026-11-05")));
    }

    [Fact]
    public void Generate_ReportsOverBudgetAndProjectedOverBudget()
    {
        var food = Guid.NewGuid();
        var fuel = Guid.NewGuid();
        var savings = Guid.NewGuid();
        var shopping = Guid.NewGuid();

        var budget = Budget.Create(Guid.NewGuid(), October, Sar(10_000));
        budget.SetLimit(food, Sar(1_000));
        budget.SetLimit(fuel, Sar(500));
        budget.SetLimit(savings, Sar(2_000));
        budget.SetLimit(shopping, Sar(800));

        var spent = new Dictionary<Guid, Money>
        {
            [food] = Sar(1_270),     // over
            [fuel] = Sar(450),       // projected 697.50 > 500
            [savings] = Sar(3_000),  // over, but savings
            [shopping] = Sar(100),   // fine
        };
        var kinds = new Dictionary<Guid, CategoryKind>
        {
            [food] = CategoryKind.Regular,
            [fuel] = CategoryKind.Regular,
            [savings] = CategoryKind.Savings,
            [shopping] = CategoryKind.Regular,
        };

        var insights = SpendingAnalysis.Generate(budget, spent, kinds, D("2026-10-20"));

        Assert.Equal(2, insights.Count);
        var over = Assert.Single(insights.OfType<OverBudgetInsight>());
        Assert.Equal(food, over.CategoryId);
        Assert.Equal(Sar(270), over.OverBy);
        Assert.Equal(27m, over.PercentOver);
        var projected = Assert.Single(insights.OfType<ProjectedOverBudgetInsight>());
        Assert.Equal(fuel, projected.CategoryId);
    }
}
