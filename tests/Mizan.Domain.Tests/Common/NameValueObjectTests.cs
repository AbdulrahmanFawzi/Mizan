using Mizan.Domain.Categories;
using Mizan.Domain.Common;
using Mizan.Domain.Goals;
using Mizan.Domain.Users;

namespace Mizan.Domain.Tests.Common;

public class NameValueObjectTests
{
    [Fact]
    public void Names_AreTrimmedAndWhitespaceCollapsed()
    {
        Assert.Equal("Fast Food", CategoryName.From("  Fast   Food ").Value);
        Assert.Equal("عبدالرحمن محمد", PersonName.From(" عبدالرحمن  محمد").Value);
        Assert.Equal("Travel", GoalName.From("Travel ").Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Names_Empty_Throw(string? value)
    {
        Assert.Throws<DomainException>(() => CategoryName.From(value));
        Assert.Throws<DomainException>(() => PersonName.From(value));
        Assert.Throws<DomainException>(() => GoalName.From(value));
    }

    [Fact]
    public void CategoryName_TooLong_Throws()
    {
        Assert.Throws<DomainException>(() => CategoryName.From(new string('a', CategoryName.MaxLength + 1)));
    }

    [Fact]
    public void CategoryName_NormalizedIgnoresCase()
    {
        Assert.Equal(CategoryName.From("food").Normalized, CategoryName.From("FOOD").Normalized);
    }
}
