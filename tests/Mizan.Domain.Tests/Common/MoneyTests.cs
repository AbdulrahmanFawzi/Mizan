using Mizan.Domain.Common;

namespace Mizan.Domain.Tests.Common;

public class MoneyTests
{
    private static readonly Currency SAR = Currency.SAR;
    private static readonly Currency USD = Currency.From("USD");

    // --- Creation (BR-M2) ---

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(10.5)]
    [InlineData(10.25)]
    [InlineData(-30.75)]
    public void Of_AmountWithUpToTwoDecimals_IsCreated(decimal amount)
    {
        var money = Money.Of(amount, SAR);

        Assert.Equal(amount, money.Amount);
        Assert.Equal(SAR, money.Currency);
    }

    [Fact]
    public void Of_AmountWithMoreThanTwoDecimals_Throws()
    {
        Assert.Throws<DomainException>(() => Money.Of(10.005m, SAR));
    }

    [Fact]
    public void Of_TrailingZerosBeyondTwoDecimals_AreAccepted()
    {
        var money = Money.Of(10.500m, SAR);

        Assert.Equal(Money.Of(10.5m, SAR), money);
    }

    // --- Equality (Value Object) ---

    [Fact]
    public void SameAmountAndCurrency_AreEqual()
    {
        Assert.Equal(Money.Of(50m, SAR), Money.Of(50m, SAR));
    }

    [Fact]
    public void SameAmountDifferentCurrency_AreNotEqual()
    {
        Assert.NotEqual(Money.Of(50m, SAR), Money.Of(50m, USD));
    }

    // --- Arithmetic ---

    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        var result = Money.Of(450m, SAR) + Money.Of(50m, SAR);

        Assert.Equal(Money.Of(500m, SAR), result);
    }

    [Fact]
    public void Subtract_CanGoNegative()
    {
        // BR-B7: remaining = 500 - 580 = -80 (over budget)
        var remaining = Money.Of(500m, SAR) - Money.Of(580m, SAR);

        Assert.Equal(Money.Of(-80m, SAR), remaining);
        Assert.True(remaining.IsNegative);
    }

    [Fact]
    public void Add_DecimalArithmetic_IsExact()
    {
        // With double, 0.1 + 0.2 = 0.30000000000000004
        var result = Money.Of(0.1m, SAR) + Money.Of(0.2m, SAR);

        Assert.Equal(Money.Of(0.3m, SAR), result);
    }

    // --- Currency mismatch (BR-M3) ---

    [Fact]
    public void Add_DifferentCurrencies_Throws()
    {
        Assert.Throws<DomainException>(() => Money.Of(10m, SAR) + Money.Of(10m, USD));
    }

    [Fact]
    public void Subtract_DifferentCurrencies_Throws()
    {
        Assert.Throws<DomainException>(() => Money.Of(10m, SAR) - Money.Of(10m, USD));
    }

    [Fact]
    public void Compare_DifferentCurrencies_Throws()
    {
        Assert.Throws<DomainException>(() => Money.Of(10m, SAR) > Money.Of(10m, USD));
    }

    // --- Comparison ---

    [Fact]
    public void Comparison_Operators_Work()
    {
        var small = Money.Of(10m, SAR);
        var large = Money.Of(20m, SAR);

        Assert.True(large > small);
        Assert.True(small < large);
        Assert.True(small <= Money.Of(10m, SAR));
        Assert.True(large >= Money.Of(20m, SAR));
    }

    // --- State ---

    [Fact]
    public void Zero_IsZero()
    {
        var zero = Money.Zero(SAR);

        Assert.True(zero.IsZero);
        Assert.False(zero.IsPositive);
        Assert.False(zero.IsNegative);
    }

    // --- Display ---

    [Theory]
    [InlineData(50, "50.00 SAR")]
    [InlineData(10.5, "10.50 SAR")]
    [InlineData(-80, "-80.00 SAR")]
    public void ToString_FormatsWithTwoDecimals(decimal amount, string expected)
    {
        Assert.Equal(expected, Money.Of(amount, SAR).ToString());
    }
}
