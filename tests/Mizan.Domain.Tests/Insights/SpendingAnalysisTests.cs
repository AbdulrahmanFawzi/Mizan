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

    // --- Safe to spend today (BR-N7) ---

    [Fact]
    public void SafeToSpendToday_SplitsWhatIsLeftOverRemainingDaysIncludingToday()
    {
        // October has 31 days. On the 22nd, 10 days are left including today.
        // 10,000 - 2,940 spent = 7,060 -> 706 per day.
        var safe = SpendingAnalysis.SafeToSpendToday(Sar(10_000), Sar(2_940), Sar(0), October, D("2026-10-22"));

        Assert.Equal(Sar(706), safe);
    }

    [Fact]
    public void SafeToSpendToday_SetsAsidePlannedSavingsFirst()
    {
        // 7,060 left, but 2,000 is still planned for savings -> 5,060 / 10 = 506.
        var safe = SpendingAnalysis.SafeToSpendToday(Sar(10_000), Sar(2_940), Sar(2_000), October, D("2026-10-22"));

        Assert.Equal(Sar(506), safe);
    }

    [Fact]
    public void SafeToSpendToday_RoundsDownToWholeRiyal()
    {
        // 1,000 / 3 days = 333.33 -> 333 (never suggest more than there is)
        var safe = SpendingAnalysis.SafeToSpendToday(Sar(1_000), Sar(0), Sar(0), October, D("2026-10-29"));

        Assert.Equal(Sar(333), safe);
    }

    [Fact]
    public void SafeToSpendToday_OnTheLastDay_IsEverythingLeft()
    {
        var safe = SpendingAnalysis.SafeToSpendToday(Sar(1_000), Sar(400), Sar(0), October, D("2026-10-31"));

        Assert.Equal(Sar(600), safe);
    }

    [Fact]
    public void SafeToSpendToday_OverspentPeriod_IsZeroNotNegative()
    {
        var safe = SpendingAnalysis.SafeToSpendToday(Sar(1_000), Sar(1_200), Sar(0), October, D("2026-10-20"));

        Assert.Equal(Sar(0), safe);
    }

    [Fact]
    public void SafeToSpendToday_OutsideThePeriod_IsNull()
    {
        Assert.Null(SpendingAnalysis.SafeToSpendToday(Sar(1_000), Sar(0), Sar(0), October, D("2026-11-02")));
    }
}
