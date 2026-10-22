using Mizan.Domain.Budgets;

namespace Mizan.Domain.Tests.Budgets;

public class AlertThresholdTests
{
    [Theory]
    [InlineData(79.99, null)]
    [InlineData(80, 80)]
    [InlineData(89.5, 80)]
    [InlineData(90, 90)]
    [InlineData(100, 100)]
    [InlineData(150, 100)]
    public void HighestReachedBy_ReturnsHighestThresholdAtOrBelowPercent(decimal percent, int? expected)
    {
        Assert.Equal(expected, AlertThreshold.HighestReachedBy(percent)?.Percent);
    }
}
