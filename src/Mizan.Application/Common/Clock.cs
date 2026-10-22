namespace Mizan.Application.Common;

// "Today" is the date in Saudi Arabia, not on the server (which usually runs in UTC).
public sealed class Clock(TimeProvider timeProvider)
{
    private static readonly TimeZoneInfo Riyadh = TimeZoneInfo.FindSystemTimeZoneById("Asia/Riyadh");

    public DateTimeOffset Now => timeProvider.GetUtcNow();

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(Now, Riyadh).DateTime);
}
