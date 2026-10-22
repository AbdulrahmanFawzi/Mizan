namespace Mizan.Domain.Common;


public sealed record Currency
{
    public static readonly Currency SAR = new("SAR");

    public string Code { get; }

    private Currency(string code) => Code = code;

    public static Currency From(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        var normalized = code.Trim().ToUpperInvariant();

        if (normalized.Length != 3 || !normalized.All(char.IsAsciiLetterUpper))
        {
            throw new DomainException($"'{code}' is not a valid currency code.");
        }

        return new Currency(normalized);
    }

    public override string ToString() => Code;
}
