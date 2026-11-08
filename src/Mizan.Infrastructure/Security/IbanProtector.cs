using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Mizan.Infrastructure.Security;

public sealed class BankingSecurityOptions
{
    public const string SectionName = "Banking";

    // Base64 of 32 random bytes. Generate one with: openssl rand -base64 32
    public string EncryptionKey { get; init; } = string.Empty;
}

// Account numbers are financial data (PDPL), so they are encrypted at rest with AES-GCM.
// Stored as base64(nonce | tag | ciphertext). Losing the key means losing the stored IBANs,
// which is acceptable: they come back with the next bank sync.
public sealed class IbanProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public IbanProtector(IOptions<BankingSecurityOptions> options)
    {
        _key = Convert.FromBase64String(options.Value.EncryptionKey);
        if (_key.Length != 32)
        {
            throw new InvalidOperationException("Banking:EncryptionKey must be 32 bytes (base64).");
        }
    }

    public string Protect(string plain)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plain);
        var output = new byte[NonceSize + TagSize + plainBytes.Length];
        var nonce = output.AsSpan(0, NonceSize);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, output.AsSpan(NonceSize + TagSize), output.AsSpan(NonceSize, TagSize));
        return Convert.ToBase64String(output);
    }

    public string Unprotect(string stored)
    {
        var input = Convert.FromBase64String(stored);
        var plain = new byte[input.Length - NonceSize - TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(input.AsSpan(0, NonceSize), input.AsSpan(NonceSize + TagSize), input.AsSpan(NonceSize, TagSize), plain);
        return Encoding.UTF8.GetString(plain);
    }
}
