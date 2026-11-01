using Mizan.Domain.Banking;
using Mizan.Domain.Common;
using static Mizan.Domain.Tests.TestData;

namespace Mizan.Domain.Tests.Banking;

public class ConsentTests
{
    private static Consent ExpiringOn(string date) =>
        Consent.Of(Now.AddMonths(-11), new DateTimeOffset(D(date).ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero));

    [Fact]
    public void DefaultPeriod_IsTwelveMonths()
    {
        var consent = Consent.ForDefaultPeriod(Now);

        Assert.Equal(Now.AddMonths(12), consent.ExpiresAt);
    }

    [Fact]
    public void Of_ExpiryBeforeGrant_Throws()
    {
        Assert.Throws<DomainException>(() => Consent.Of(Now, Now.AddDays(-1)));
    }

    [Fact]
    public void IsExpired_FromTheExpiryMoment()
    {
        var consent = Consent.Of(Now.AddDays(-10), Now);

        Assert.False(consent.IsExpired(Now.AddSeconds(-1)));
        Assert.True(consent.IsExpired(Now));
    }

    [Theory]
    [InlineData("2026-11-20", null)]   // 21 days left: too early
    [InlineData("2026-11-13", 14)]     // 14 days left
    [InlineData("2026-11-10", 14)]     // 11 days left, 14 never sent: send it now
    [InlineData("2026-11-02", 3)]      // 3 days left
    [InlineData("2026-10-29", null)]   // already past
    public void DueReminder_NothingSentYet(string expiresOn, int? expected)
    {
        Assert.Equal(expected, ExpiringOn(expiresOn).DueReminder(Today, []));
    }

    [Fact]
    public void DueReminder_SkipsRemindersAlreadySent()
    {
        var consent = ExpiringOn("2026-11-10");

        Assert.Null(consent.DueReminder(Today, [14]));
        Assert.Equal(3, ExpiringOn("2026-11-01").DueReminder(Today, [14]));
    }
}
