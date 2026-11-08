using Mizan.Domain.Categories;
using Mizan.Domain.Categorization;
using Mizan.Domain.Importing;
using Mizan.Domain.Transactions;

namespace Mizan.Domain.Tests.Importing;

public class CategoryResolverTests
{
    private static readonly CategoryChoice Other = new(Guid.NewGuid(), "أخرى", CategoryKind.Other);
    private static readonly CategoryChoice Savings = new(Guid.NewGuid(), "الادخار", CategoryKind.Savings);
    private static readonly CategoryChoice Restaurants = new(Guid.NewGuid(), "المطاعم", CategoryKind.Regular);
    private static readonly CategoryChoice Groceries = new(Guid.NewGuid(), "Groceries", CategoryKind.Regular);
    private static readonly CategoryChoice[] Categories = [Other, Savings, Restaurants, Groceries];

    private static readonly MerchantKey Panda = MerchantKey.From("PANDA 001")!;

    private static CategoryDecision Resolve(
        ProviderCategory? provider = null,
        bool atm = false,
        Dictionary<MerchantKey, Guid>? rules = null,
        MerchantKey? merchant = null) =>
        CategoryResolver.Resolve(merchant ?? Panda, provider, atm, rules ?? [], Categories);

    [Fact]
    public void MerchantRule_WinsOverEverything()
    {
        var decision = Resolve(new ProviderCategory("GROCERIES", 0.99m), rules: new() { [Panda] = Restaurants.Id });

        Assert.Equal(new CategoryDecision(Restaurants.Id, CategorySource.MerchantRule), decision);
    }

    [Fact]
    public void ConfidentProviderCategory_MatchesTheUsersCategoryByName()
    {
        Assert.Equal(new CategoryDecision(Restaurants.Id, CategorySource.Provider),
            Resolve(new ProviderCategory("RESTAURANTS_DINING", 0.8m)));
        Assert.Equal(new CategoryDecision(Groceries.Id, CategorySource.Provider),
            Resolve(new ProviderCategory("GROCERIES", 0.5m)));
    }

    [Fact]
    public void UnsureProviderCategory_FallsBackToOther()
    {
        Assert.Equal(new CategoryDecision(Other.Id, CategorySource.Fallback), Resolve(new ProviderCategory("GROCERIES", 0.49m)));
    }

    [Fact]
    public void NoMatchingUserCategory_FallsBackToOther()
    {
        Assert.Equal(Other.Id, Resolve(new ProviderCategory("TRAVEL", 0.9m)).CategoryId);
    }

    [Fact]
    public void Savings_IsNeverPickedAutomatically()
    {
        var onlySavingsMatch = new[] { Other, new CategoryChoice(Guid.NewGuid(), "ادخار المطاعم", CategoryKind.Savings) };

        var decision = CategoryResolver.Resolve(Panda, new ProviderCategory("RESTAURANTS_DINING", 0.9m), false, new Dictionary<MerchantKey, Guid>(), onlySavingsMatch);

        Assert.Equal(Other.Id, decision.CategoryId);
    }

    [Fact]
    public void CashWithdrawal_GoesToOther()
    {
        Assert.Equal(new CategoryDecision(Other.Id, CategorySource.Fallback), Resolve(new ProviderCategory("RETAIL", 0.9m), atm: true));
    }

    [Fact]
    public void RuleForADeletedCategory_IsIgnored()
    {
        Assert.Equal(Other.Id, Resolve(rules: new() { [Panda] = Guid.NewGuid() }).CategoryId);
    }
}
