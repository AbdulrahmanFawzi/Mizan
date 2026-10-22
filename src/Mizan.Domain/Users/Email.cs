using System.Net.Mail;
using Mizan.Domain.Common;

namespace Mizan.Domain.Users;

public sealed record Email
{
    public const int MaxLength = 256;

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email From(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();

        if (normalized.Length is 0 or > MaxLength
            || !MailAddress.TryCreate(normalized, out var address)
            || address.Address != normalized
            || !address.Host.Contains('.'))
        {
            throw new DomainException($"'{value}' is not a valid email address.");
        }

        return new Email(normalized);
    }

    public override string ToString() => Value;
}
