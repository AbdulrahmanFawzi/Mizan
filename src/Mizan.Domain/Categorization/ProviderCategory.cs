namespace Mizan.Domain.Categorization;

// The bank provider's own guess at a category, e.g. GROCERIES with 0.62 confidence.
public sealed record ProviderCategory(string Code, decimal Confidence)
{
    // BR-K4: below this, the guess is ignored.
    public const decimal MinReliableConfidence = 0.5m;

    public bool IsReliable => Confidence >= MinReliableConfidence && !string.IsNullOrWhiteSpace(Code);
}
