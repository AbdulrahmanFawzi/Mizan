using Mizan.Domain.Common;

namespace Mizan.Domain.Users;

public sealed record PersonName
{
    public const int MaxLength = 100;

    public string Value { get; }

    private PersonName(string value) => Value = value;

    public static PersonName From(string? value) => new(Text.Required(value, MaxLength, "Name"));

    public override string ToString() => Value;
}
