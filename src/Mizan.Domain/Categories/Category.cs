using Mizan.Domain.Common;

namespace Mizan.Domain.Categories;

public sealed class Category : AggregateRoot
{
    public static readonly CategoryName OtherName = CategoryName.From("أخرى");
    public static readonly CategoryName SavingsName = CategoryName.From("الادخار");

    public Guid UserId { get; private set; }
    public CategoryName Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public CategoryKind Kind { get; private set; }

    public bool IsSystem => Kind != CategoryKind.Regular;

    private Category()
    {
    }

    private Category(Guid userId, CategoryName name, CategoryKind kind)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        Kind = kind;
        SetName(name);
    }

    public static Category CreateRegular(Guid userId, CategoryName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return new Category(userId, name, CategoryKind.Regular);
    }

    // BR-B3: every user always has "Other" and "Savings".
    public static IReadOnlyList<Category> CreateSystemCategories(Guid userId) =>
    [
        new Category(userId, OtherName, CategoryKind.Other),
        new Category(userId, SavingsName, CategoryKind.Savings),
    ];

    public void Rename(CategoryName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        EnsureNotSystem("renamed");
        SetName(name);
    }

    public void Delete()
    {
        EnsureNotSystem("deleted");
        Raise(new CategoryDeleted(UserId, Id));
    }

    private void SetName(CategoryName name)
    {
        Name = name;
        NormalizedName = name.Normalized;
    }

    private void EnsureNotSystem(string action)
    {
        if (IsSystem)
        {
            throw new DomainException($"The '{Name}' category cannot be {action}.");
        }
    }
}
