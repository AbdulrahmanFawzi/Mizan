using System.Globalization;
using Mizan.Domain.Budgets;
using Mizan.Domain.Common;

namespace Mizan.Domain.Tests.Budgets;

public class PeriodStartDayTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(27)]
    [InlineData(31)]
    public void From_DayBetween1And31_IsCreated(int day)
    {
        Assert.Equal(day, PeriodStartDay.From(day).Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(-1)]
    public void From_DayOutOfRange_Throws(int day)
    {
        Assert.Throws<DomainException>(() => PeriodStartDay.From(day));
    }

    [Theory]
    [InlineData(27, 2026, 10, "2026-10-27")]
    [InlineData(31, 2026, 4, "2026-04-30")] // BR-P2: April has 30 days
    [InlineData(31, 2026, 2, "2026-02-28")] // BR-P2
    [InlineData(30, 2026, 2, "2026-02-28")] // BR-P2
    [InlineData(31, 2028, 2, "2028-02-29")] // leap year
    public void DateIn_ClampsToLastDayOfShortMonths(int day, int year, int month, string expected)
    {
        var date = PeriodStartDay.From(day).DateIn(year, month);

        Assert.Equal(DateOnly.ParseExact(expected, "yyyy-MM-dd", CultureInfo.InvariantCulture), date);
    }
}
