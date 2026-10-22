using System.Globalization;
using Mizan.Domain.Budgets;

namespace Mizan.Domain.Tests.Budgets;

public class BudgetPeriodTests
{
    private static DateOnly D(string value) =>
        DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static PeriodStartDay Day(int value) => PeriodStartDay.From(value);

    // --- Containing (BR-P3) ---

    [Theory]
    [InlineData(27, "2026-10-30", "2026-10-27", "2026-11-26")] // salary on the 27th
    [InlineData(27, "2026-10-27", "2026-10-27", "2026-11-26")] // first day of period
    [InlineData(27, "2026-10-26", "2026-09-27", "2026-10-26")] // last day of previous period
    [InlineData(1, "2026-10-15", "2026-10-01", "2026-10-31")]  // calendar month
    [InlineData(27, "2026-01-05", "2025-12-27", "2026-01-26")] // crosses the year
    public void Containing_ReturnsPeriodThatIncludesDate(
        int startDay, string date, string expectedStart, string expectedEnd)
    {
        var period = BudgetPeriod.Containing(D(date), Day(startDay));

        Assert.Equal(D(expectedStart), period.Start);
        Assert.Equal(D(expectedEnd), period.End);
    }

    [Theory]
    [InlineData("2026-02-27", "2026-01-31", "2026-02-27")]
    [InlineData("2026-02-28", "2026-02-28", "2026-03-30")]
    [InlineData("2026-03-15", "2026-02-28", "2026-03-30")]
    public void Containing_Day31_ClampsInShortMonths(
        string date, string expectedStart, string expectedEnd)
    {
        var period = BudgetPeriod.Containing(D(date), Day(31));

        Assert.Equal(D(expectedStart), period.Start);
        Assert.Equal(D(expectedEnd), period.End);
    }

    // --- Next ---

    [Fact]
    public void Next_SameStartDay_StartsTheDayAfterAndKeepsTheCycle()
    {
        var current = BudgetPeriod.Containing(D("2026-10-30"), Day(27));

        var next = current.Next(Day(27));

        Assert.Equal(D("2026-11-27"), next.Start);
        Assert.Equal(D("2026-12-26"), next.End);
    }

    [Fact]
    public void Next_Day31_FollowsShortMonths()
    {
        var january = BudgetPeriod.Containing(D("2026-01-31"), Day(31));

        var next = january.Next(Day(31));

        Assert.Equal(D("2026-02-28"), next.Start);
        Assert.Equal(D("2026-03-30"), next.End);
    }

    // --- Next after changing the start day (BR-P4, BR-P8) ---

    [Fact]
    public void Next_ChangedStartDay_ShortTransitionIsExtended()
    {
        // 27 Nov → 30 Nov would be only 4 days, so it runs until the next 1st after that.
        var current = BudgetPeriod.Containing(D("2026-10-30"), Day(27));

        var next = current.Next(Day(1));

        Assert.Equal(D("2026-11-27"), next.Start);
        Assert.Equal(D("2026-12-31"), next.End);
    }

    [Fact]
    public void Next_ChangedStartDay_LongEnoughTransitionIsKept()
    {
        // 27 Nov → 14 Dec is 18 days.
        var current = BudgetPeriod.Containing(D("2026-10-30"), Day(27));

        var next = current.Next(Day(15));

        Assert.Equal(D("2026-11-27"), next.Start);
        Assert.Equal(D("2026-12-14"), next.End);
    }

    [Theory]
    [InlineData(12, "2026-12-11")] // exactly 15 days: kept
    [InlineData(11, "2027-01-10")] // 14 days: extended
    public void Next_ChangedStartDay_FifteenDaysIsTheBoundary(int newStartDay, string expectedEnd)
    {
        var current = BudgetPeriod.Containing(D("2026-10-30"), Day(27));

        var next = current.Next(Day(newStartDay));

        Assert.Equal(D("2026-11-27"), next.Start);
        Assert.Equal(D(expectedEnd), next.End);
    }

    [Fact]
    public void Next_AfterTransition_FollowsTheNewStartDay()
    {
        var current = BudgetPeriod.Containing(D("2026-10-30"), Day(27));
        var transition = current.Next(Day(1));

        var afterTransition = transition.Next(Day(1));

        Assert.Equal(D("2027-01-01"), afterTransition.Start);
        Assert.Equal(D("2027-01-31"), afterTransition.End);
    }

    // --- Length, Contains, DaysElapsed (BR-P7, BR-N2) ---

    [Fact]
    public void LengthInDays_IncludesBothEnds()
    {
        var period = BudgetPeriod.Containing(D("2026-10-30"), Day(27)); // 27 Oct → 26 Nov

        Assert.Equal(31, period.LengthInDays);
    }

    [Theory]
    [InlineData("2026-10-26", false)]
    [InlineData("2026-10-27", true)]
    [InlineData("2026-11-10", true)]
    [InlineData("2026-11-26", true)]
    [InlineData("2026-11-27", false)]
    public void Contains_ChecksBothEnds(string date, bool expected)
    {
        var period = BudgetPeriod.Containing(D("2026-10-30"), Day(27));

        Assert.Equal(expected, period.Contains(D(date)));
    }

    [Theory]
    [InlineData("2026-10-20", 0)]  // before the period
    [InlineData("2026-10-27", 1)]  // first day counts as day 1
    [InlineData("2026-11-15", 20)]
    [InlineData("2026-11-26", 31)] // last day
    [InlineData("2026-12-20", 31)] // after the period
    public void DaysElapsed_CountsTodayAndStaysWithinThePeriod(string today, int expected)
    {
        var period = BudgetPeriod.Containing(D("2026-10-30"), Day(27));

        Assert.Equal(expected, period.DaysElapsed(D(today)));
    }

    // --- Value Object ---

    [Fact]
    public void SameDates_AreEqual()
    {
        var a = BudgetPeriod.Containing(D("2026-10-28"), Day(27));
        var b = BudgetPeriod.Containing(D("2026-11-20"), Day(27));

        Assert.Equal(a, b);
    }
}
