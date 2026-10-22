using Microsoft.AspNetCore.Identity;
using Mizan.Application.Abstractions;
using Mizan.Domain.Users;

namespace Mizan.Infrastructure.Security;

// Uses ASP.NET Core Identity's hasher (PBKDF2 with a random salt) without the rest of Identity.
internal sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public PasswordCheck Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheck.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.SuccessRehashNeeded,
            _ => PasswordCheck.Failed,
        };
}
