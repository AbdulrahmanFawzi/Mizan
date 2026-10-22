using Mizan.Domain.Users;

namespace Mizan.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    PasswordCheck Verify(string passwordHash, string password);
}

public enum PasswordCheck
{
    Failed,
    Success,
    SuccessRehashNeeded,
}

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

public interface IAccessTokenIssuer
{
    AccessToken Issue(User user);
}
