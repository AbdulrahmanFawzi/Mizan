using Mizan.Domain.Common;

namespace Mizan.Domain.Banking;

// The period the user allowed us to read their bank data.
public sealed record Consent
{
    // BR-C4 (OB-7)
    public const int DefaultMonths = 12;

    // BR-C5: remind 14 days and 3 days before it expires.
    public static IReadOnlyList<int> ReminderDays { get; } = [14, 3];

    public DateTimeOffset GrantedAt { get; }
    public DateTimeOffset ExpiresAt { get; }

    private Consent(DateTimeOffset grantedAt, DateTimeOffset expiresAt)
    {
        GrantedAt = grantedAt;
        ExpiresAt = expiresAt;
    }

    public static Consent Of(DateTimeOffset grantedAt, DateTimeOffset expiresAt)
    {
        if (expiresAt <= grantedAt)
        {
            throw new DomainException("A consent must expire after it is granted.");
        }

        return new Consent(grantedAt, expiresAt);
    }

    public static Consent ForDefaultPeriod(DateTimeOffset grantedAt) => Of(grantedAt, grantedAt.AddMonths(DefaultMonths));

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public int DaysUntilExpiry(DateOnly today) => ExpiresOn.DayNumber - today.DayNumber;

    public DateOnly ExpiresOn => DateOnly.FromDateTime(ExpiresAt.UtcDateTime);

    // The reminder that is due today, if any, skipping ones already sent.
    // A missed day still sends the reminder once it is due (e.g. 13 days left and 14 not sent yet).
    public int? DueReminder(DateOnly today, IReadOnlyCollection<int> alreadySent)
    {
        var daysLeft = DaysUntilExpiry(today);
        if (daysLeft < 0)
        {
            return null;
        }

        return ReminderDays
            .Where(day => daysLeft <= day && !alreadySent.Contains(day))
            .Select(day => (int?)day)
            .LastOrDefault();
    }
}
