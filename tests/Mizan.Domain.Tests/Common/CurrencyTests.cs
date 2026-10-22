using Mizan.Domain.Common;

namespace Mizan.Domain.Tests.Common;

public class CurrencyTests
{
    [Theory]
    [InlineData("SAR")]
    [InlineData("sar")]
    [InlineData(" SAR ")]
    public void From_NormalizesCode(string code)
    {
        var currency = Currency.From(code);

        Assert.Equal(Currency.SAR, currency);
    }

    [Theory]
    [InlineData("")]
    [InlineData("SA")]
    [InlineData("SARR")]
    [InlineData("S1R")]
    [InlineData("ريا")]
    public void From_InvalidCode_Throws(string code)
    {
        Assert.Throws<DomainException>(() => Currency.From(code));
    }

    [Fact]
    public void SameCode_AreEqual()
    {
        Assert.Equal(Currency.From("USD"), Currency.From("usd"));
    }
}
