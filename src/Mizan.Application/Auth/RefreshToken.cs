using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Mizan.Application.Auth;

// Technical, not part of the domain. Only a hash of the token is stored, never the token itself.
public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private RefreshToken()
    {
    }

    public static (RefreshToken Token, string PlainText) Issue(Guid userId, DateTimeOffset now, TimeSpan lifetime)
    {
        var plainText = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

        var token = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TokenHash = HashOf(plainText),
            CreatedAt = now,
            ExpiresAt = now.Add(lifetime),
        };

        return (token, plainText);
    }

    public static string HashOf(string plainText) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainText)));

    public bool IsRevoked => RevokedAt is not null;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}
