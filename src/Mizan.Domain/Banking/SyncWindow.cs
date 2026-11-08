using Mizan.Domain.Common;

namespace Mizan.Domain.Banking;

// The dates to fetch from the bank in one sync.
public sealed record SyncWindow
{
    // BR-H1 (OB-5)
    public const int FirstLinkMonths = 3;

    // BR-S6: re-read the last week to catch pending transactions that changed.
    public const int OverlapDays = 7;

    public DateOnly From { get; }
    public DateOnly To { get; }

    private SyncWindow(DateOnly from, DateOnly to)
    {
        From = from;
        To = to;
    }

    public static SyncWindow Of(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            throw new DomainException("A sync window must not end before it starts.");
        }

        return new SyncWindow(from, to);
    }

    public static SyncWindow ForFirstLink(DateOnly today) => Of(today.AddMonths(-FirstLinkMonths), today);

    public static SyncWindow After(DateOnly lastSynced, DateOnly today) =>
        Of(Min(lastSynced.AddDays(-OverlapDays), today), today);

    public bool Contains(DateOnly date) => date >= From && date <= To;

    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;
}
