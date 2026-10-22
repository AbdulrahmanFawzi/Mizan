using Mizan.Domain.Common;

namespace Mizan.Domain.Categories;

public sealed record CategoryName
{
    public const int MaxLength = 50;

    public string Value { get; }

    private CategoryName(string value) => Value = value;

    public static CategoryName From(string? value) => new(Text.Required(value, MaxLength, "Category name"));

    // BR-B1: names are unique per user regardless of letter case.
    public string Normalized => Value.ToUpperInvariant();

    public override string ToString() => Value;
}
