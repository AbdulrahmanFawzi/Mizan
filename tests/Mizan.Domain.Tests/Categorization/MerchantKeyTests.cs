using Mizan.Domain.Categorization;

namespace Mizan.Domain.Tests.Categorization;

public class MerchantKeyTests
{
    [Theory]
    [InlineData("STC PAY 1234", "stc pay")]
    [InlineData("  stc   pay-998 ", "stc pay")]
    [InlineData("Starbucks #4421 Riyadh", "starbucks riyadh")]
    [InlineData("بنده 12", "بنده")]
    public void From_NormalizesTheName(string name, string expected)
    {
        Assert.Equal(expected, MerchantKey.From(name)!.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("1234 - 55")]
    public void From_NothingUseful_ReturnsNull(string? name)
    {
        Assert.Null(MerchantKey.From(name));
    }

    [Fact]
    public void SameShop_DifferentBranches_AreEqual()
    {
        Assert.Equal(MerchantKey.From("PANDA 0012"), MerchantKey.From("Panda-0098"));
    }
}
