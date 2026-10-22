using System.Globalization;

namespace Mizan.Domain.Budgets;

public sealed record BudgetPeriod
{
    private const int MinimumTransitionDays = 15;

    public DateOnly Start { get; }
    public DateOnly End { get; }

    private BudgetPeriod(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public static BudgetPeriod Containing(DateOnly date, PeriodStartDay startDay)
    {
        ArgumentNullException.ThrowIfNull(startDay);

        var start = startDay.DateIn(date.Year, date.Month);
        if (start > date)
        {
            var previousMonth = date.AddMonths(-1);
            start = startDay.DateIn(previousMonth.Year, previousMonth.Month);
        }

        var nextStart = startDay.FirstStartAfter(start);
        return new BudgetPeriod(start, nextStart.AddDays(-1));
    }

    public BudgetPeriod Next(PeriodStartDay startDay)
    {
        ArgumentNullException.ThrowIfNull(startDay);

        var start = End.AddDays(1);
        var nextStart = startDay.FirstStartAfter(start);

        // BR-P8: after the start day changes, don't create a tiny transition period.
        if (nextStart.DayNumber - start.DayNumber < MinimumTransitionDays)
        {
            nextStart = startDay.FirstStartAfter(nextStart);
        }

        return new BudgetPeriod(start, nextStart.AddDays(-1));
    }

    public int LengthInDays => End.DayNumber - Start.DayNumber + 1;

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    public int DaysElapsed(DateOnly today)
    {
        if (today < Start)
        {
            return 0;
        }

        if (today > End)
        {
            return LengthInDays;
        }

        return today.DayNumber - Start.DayNumber + 1;
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Start:yyyy-MM-dd} → {End:yyyy-MM-dd}");
}
