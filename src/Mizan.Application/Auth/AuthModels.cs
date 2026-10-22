namespace Mizan.Application.Auth;

public sealed record RegisterCommand(
    string Name,
    string Email,
    string Password,
    decimal MonthlyIncome,
    int PeriodStartDay);

public sealed record LoginCommand(string Email, string Password);

public sealed record RefreshCommand(string RefreshToken);

public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
