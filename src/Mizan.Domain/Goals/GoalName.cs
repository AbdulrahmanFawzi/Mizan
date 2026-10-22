using Mizan.Domain.Common;

namespace Mizan.Domain.Goals;

public sealed record GoalName
{
    public const int MaxLength = 100;

    public string Value { get; }

    private GoalName(string value) => Value = value;

    public static GoalName From(string? value) => new(Text.Required(value, MaxLength, "Goal name"));

    public override string ToString() => Value;
}
