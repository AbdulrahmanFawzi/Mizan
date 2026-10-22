using Mizan.Domain.Categories;
using Mizan.Domain.Common;

namespace Mizan.Domain.Tests.Categories;

public class CategoryTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void CreateSystemCategories_CreatesOtherAndSavings()
    {
        var categories = Category.CreateSystemCategories(UserId);

        Assert.Equal(
            [CategoryKind.Other, CategoryKind.Savings],
            categories.Select(c => c.Kind).ToArray());
        Assert.All(categories, c => Assert.True(c.IsSystem));
    }

    [Fact]
    public void SystemCategory_CannotBeRenamedOrDeleted()
    {
        var other = Category.CreateSystemCategories(UserId)[0];

        Assert.Throws<DomainException>(() => other.Rename(CategoryName.From("Misc")));
        Assert.Throws<DomainException>(() => other.Delete());
    }

    [Fact]
    public void RegularCategory_Delete_RaisesCategoryDeleted()
    {
        var food = Category.CreateRegular(UserId, CategoryName.From("Food"));

        food.Delete();

        Assert.Equal(new CategoryDeleted(UserId, food.Id), Assert.Single(food.DomainEvents));
    }

    [Fact]
    public void Rename_UpdatesNormalizedName()
    {
        var category = Category.CreateRegular(UserId, CategoryName.From("food"));

        category.Rename(CategoryName.From("Groceries"));

        Assert.Equal("GROCERIES", category.NormalizedName);
    }
}
