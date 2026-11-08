using Mizan.Domain.Banking;
using Mizan.Domain.Common;
using static Mizan.Domain.Tests.TestData;

namespace Mizan.Domain.Tests.Banking;

public class SyncWindowTests
{
    [Fact]
    public void FirstLink_CoversThreeMonths()
    {
        var window = SyncWindow.ForFirstLink(Today);

        Assert.Equal(D("2026-07-30"), window.From);
        Assert.Equal(Today, window.To);
    }

    [Fact]
    public void After_ReReadsTheLastWeek()
    {
        var window = SyncWindow.After(D("2026-10-28"), Today);

        Assert.Equal(D("2026-10-21"), window.From);
        Assert.Equal(Today, window.To);
    }

    [Fact]
    public void Of_EndBeforeStart_Throws()
    {
        Assert.Throws<DomainException>(() => SyncWindow.Of(Today, Today.AddDays(-1)));
    }
}
