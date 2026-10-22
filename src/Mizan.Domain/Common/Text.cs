namespace Mizan.Domain.Common;

internal static class Text
{
    public static string Required(string? value, int maxLength, string field)
    {
        var normalized = Normalize(value);

        if (normalized.Length == 0)
        {
            throw new DomainException($"{field} is required.");
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{field} must be at most {maxLength} characters.");
        }

        return normalized;
    }

    public static string? Optional(string? value, int maxLength, string field)
    {
        var normalized = Normalize(value);
        return normalized.Length == 0 ? null : Required(normalized, maxLength, field);
    }

    // Trims and collapses repeated whitespace: "  Fast   Food " -> "Fast Food"
    private static string Normalize(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
