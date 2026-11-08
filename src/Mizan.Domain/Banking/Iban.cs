using System.Text.RegularExpressions;
using Mizan.Domain.Common;

namespace Mizan.Domain.Banking;

// An account number in IBAN form, used to recognise transfers between the user's own accounts (BR-R1).
public sealed partial record Iban
{
    public string Value { get; }

    private Iban(string value) => Value = value;

    public static Iban From(string value)
    {
        var normalized = TryNormalize(value) ?? throw new DomainException($"'{value}' is not a valid IBAN.");
        return new Iban(normalized);
    }

    // Bank data is not always clean, so imports use this instead of throwing.
    public static Iban? TryFrom(string? value) => TryNormalize(value) is { } normalized ? new Iban(normalized) : null;

    // For display: "SA•• •••• 1234".
    public string Masked => $"{Value[..2]}•• •••• {Value[^4..]}";

    public override string ToString() => Masked;

    private static string? TryNormalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = string.Concat(value.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
        return Pattern().IsMatch(normalized) ? normalized : null;
    }

    // Country code, two check digits, then 10 to 30 letters or digits (Saudi IBANs are SA + 22 digits).
    [GeneratedRegex("^[A-Z]{2}[0-9]{2}[A-Z0-9]{10,30}$")]
    private static partial Regex Pattern();
}
