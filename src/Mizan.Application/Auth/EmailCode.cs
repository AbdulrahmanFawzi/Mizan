using System.Security.Cryptography;
using System.Text;

namespace Mizan.Application.Auth;

public enum EmailCodePurpose
{
    VerifyEmail,
    ResetPassword,
}

// A 6-digit code sent by email. Technical, like RefreshToken: only a hash is stored.
// Only the latest code of each purpose counts, so asking for a new one replaces the old one.
public sealed class EmailCode
{
    public const int Digits = 6;
    public const int MaxAttempts = 5;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ResendAfter = TimeSpan.FromSeconds(60);

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public EmailCodePurpose Purpose { get; private set; }
    public string CodeHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    private EmailCode()
    {
    }

    public static (EmailCode Code, string PlainText) Issue(Guid userId, EmailCodePurpose purpose, DateTimeOffset now)
    {
        var plainText = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var id = Guid.CreateVersion7();

        return (new EmailCode
        {
            Id = id,
            UserId = userId,
            Purpose = purpose,
            CodeHash = HashOf(id, plainText),
            CreatedAt = now,
            ExpiresAt = now.Add(Lifetime),
        }, plainText);
    }

    public bool CanResend(DateTimeOffset now) => now >= CreatedAt.Add(ResendAfter);

    // A wrong guess counts as an attempt; after MaxAttempts the code is dead (6 digits are easy to guess otherwise).
    public bool TryUse(string? plainText, DateTimeOffset now)
    {
        if (UsedAt is not null || now >= ExpiresAt || Attempts >= MaxAttempts)
        {
            return false;
        }

        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(HashOf(Id, plainText?.Trim() ?? string.Empty)),
            Encoding.ASCII.GetBytes(CodeHash));

        if (matches)
        {
            UsedAt = now;
        }
        else
        {
            Attempts++;
        }

        return matches;
    }

    // The id is mixed in so the same code in two rows doesn't give the same hash.
    private static string HashOf(Guid id, string plainText) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{id:N}:{plainText}")));
}
