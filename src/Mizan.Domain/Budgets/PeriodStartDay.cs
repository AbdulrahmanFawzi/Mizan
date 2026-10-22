using Mizan.Domain.Common;

namespace Mizan.Domain.Budgets;

public sealed record PeriodStartDay
{
    public int Value { get; }

    private PeriodStartDay(int value) => Value = value;

    public static PeriodStartDay From(int value)
    {
        if (value is < 1 or > 31)
        {
            throw new DomainException($"Period start day must be between 1 and 31, but was {value}.");
        }

        return new PeriodStartDay(value);
    }

    public DateOnly DateIn(int year, int month)
    {
        var day = Math.Min(Value, DateTime.DaysInMonth(year, month));
        return new DateOnly(year, month, day);
    }

    public DateOnly FirstStartAfter(DateOnly date)
    {
        var candidate = DateIn(date.Year, date.Month);
        if (candidate > date)
        {
            return candidate;
        }

        var nextMonth = date.AddMonths(1);
        return DateIn(nextMonth.Year, nextMonth.Month);
    }

    public override string ToString() => Value.ToString();
}
