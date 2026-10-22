using Mizan.Domain.Common;
using Mizan.Domain.Users;

namespace Mizan.Domain.Tests.Users;

public class EmailTests
{
    [Fact]
    public void From_NormalizesToLowerCase()
    {
        Assert.Equal("ali@example.com", Email.From("  Ali@Example.COM ").Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ali")]
    [InlineData("ali@")]
    [InlineData("ali@example")]
    [InlineData("Ali <ali@example.com>")]
    public void From_Invalid_Throws(string? value)
    {
        Assert.Throws<DomainException>(() => Email.From(value));
    }
}
