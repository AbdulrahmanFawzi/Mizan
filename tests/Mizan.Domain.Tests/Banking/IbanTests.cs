using Mizan.Domain.Banking;
using Mizan.Domain.Common;

namespace Mizan.Domain.Tests.Banking;

public class IbanTests
{
    [Fact]
    public void From_RemovesSpacesAndUppercases()
    {
        Assert.Equal("SA0380000000608010167519", Iban.From("sa03 8000 0000 6080 1016 7519").Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("SA03 8000")]
    [InlineData("SA03-8000-0000-6080-1016-7519")]
    public void From_Invalid_Throws(string value)
    {
        Assert.Throws<DomainException>(() => Iban.From(value));
    }

    [Fact]
    public void TryFrom_Invalid_ReturnsNull()
    {
        Assert.Null(Iban.TryFrom("not an iban"));
        Assert.Null(Iban.TryFrom(null));
    }

    [Fact]
    public void SameNumber_IsEqual_RegardlessOfFormatting()
    {
        Assert.Equal(Iban.From("SA03 8000 0000 6080 1016 7519"), Iban.From("sa0380000000608010167519"));
    }

    [Fact]
    public void Masked_ShowsOnlyCountryAndLastFourDigits()
    {
        Assert.Equal("SA•• •••• 7519", Iban.From("SA0380000000608010167519").Masked);
    }
}
