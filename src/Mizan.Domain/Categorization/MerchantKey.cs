using System.Text;

namespace Mizan.Domain.Categorization;

// A merchant name cleaned up so the same shop always gives the same key (BR-K3):
// "STC PAY 1234" and "stc  pay-998" both become "stc pay".
public sealed record MerchantKey
{
    public const int MaxLength = 100;

    public string Value { get; }

    private MerchantKey(string value) => Value = value;

    // Null when nothing useful is left (empty, or only digits and symbols).
    public static MerchantKey? From(string? merchantName)
    {
        if (string.IsNullOrWhiteSpace(merchantName))
        {
            return null;
        }

        var builder = new StringBuilder(merchantName.Length);
        foreach (var c in merchantName.ToLowerInvariant())
        {
            // Letters (Arabic or Latin) are kept. Digits, punctuation, and symbols become spaces.
            builder.Append(char.IsLetter(c) ? c : ' ');
        }

        var value = string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (value.Length == 0)
        {
            return null;
        }

        return new MerchantKey(value.Length > MaxLength ? value[..MaxLength].TrimEnd() : value);
    }

    public static MerchantKey Restore(string value) =>
        From(value) ?? throw new ArgumentException("Stored merchant key is empty.", nameof(value));

    public override string ToString() => Value;
}
