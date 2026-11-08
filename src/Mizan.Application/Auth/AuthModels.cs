namespace Mizan.Application.Auth;

public sealed record RegisterCommand(
    string Name,
    string Email,
    string Password,
    decimal MonthlyIncome,
    int PeriodStartDay);

public sealed record LoginCommand(string Email, string Password);

public sealed record RefreshCommand(string RefreshToken);

public sealed record ForgotPasswordCommand(string Email);

public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword);

public sealed record VerifyEmailCommand(string Code);

public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
