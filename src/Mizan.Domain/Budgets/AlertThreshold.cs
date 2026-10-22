using Mizan.Domain.Common;

namespace Mizan.Domain.Budgets;

public sealed record AlertThreshold
{
    public static readonly AlertThreshold Eighty = new(80);
    public static readonly AlertThreshold Ninety = new(90);
    public static readonly AlertThreshold Hundred = new(100);

    // Ascending order matters: HighestReachedBy relies on it.
    public static IReadOnlyList<AlertThreshold> All { get; } = [Eighty, Ninety, Hundred];

    public int Percent { get; }

    private AlertThreshold(int percent) => Percent = percent;

    public static AlertThreshold FromPercent(int percent) =>
        All.FirstOrDefault(t => t.Percent == percent)
        ?? throw new DomainException($"{percent}% is not an alert threshold.");

    public static AlertThreshold? HighestReachedBy(decimal percentUsed) =>
        All.LastOrDefault(t => percentUsed >= t.Percent);

    public override string ToString() => $"{Percent}%";
}
