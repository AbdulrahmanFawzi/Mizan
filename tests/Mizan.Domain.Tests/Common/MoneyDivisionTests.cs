using Mizan.Domain.Common;
using static Mizan.Domain.Tests.TestData;

namespace Mizan.Domain.Tests.Common;

public class MoneyDivisionTests
{
    [Theory]
    [InlineData(6500, 12, 542)] // 541.67 rounds up (BR-G5)
    [InlineData(1200, 12, 100)]
    [InlineData(100.01, 1, 101)]
    public void DivideRoundingUpToWhole_RoundsUp(decimal amount, int divisor, decimal expected)
    {
        Assert.Equal(Sar(expected), Sar(amount).DivideRoundingUpToWhole(divisor));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DivideRoundingUpToWhole_NonPositiveDivisor_Throws(int divisor)
    {
        Assert.Throws<DomainException>(() => Sar(100).DivideRoundingUpToWhole(divisor));
    }
}
