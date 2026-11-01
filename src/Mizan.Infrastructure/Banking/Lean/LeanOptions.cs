namespace Mizan.Infrastructure.Banking.Lean;

public sealed class LeanOptions
{
    public const string SectionName = "Lean";

    public string ApiUrl { get; init; } = "https://sandbox.sa.leantech.me";
    public string AuthUrl { get; init; } = "https://auth.sandbox.sa.leantech.me";
    public bool Sandbox { get; init; } = true;

    // Secrets: user-secrets in development, environment variables in production. Never in git.
    public string AppToken { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;

    // Must also be registered in the Lean dashboard (Integration settings → Redirection URLs).
    public string SuccessRedirectUrl { get; init; } = "https://www.leantech.me/?success=true";
    public string FailRedirectUrl { get; init; } = "https://www.leantech.me/?success=false";

    // BR-C4 (OB-7)
    public int ConsentMonths { get; init; } = 12;

    // BR-C13: the least we need.
    public string[] Permissions { get; init; } = ["accounts", "balance", "transactions"];

    public bool IsConfigured => !string.IsNullOrWhiteSpace(AppToken) && !string.IsNullOrWhiteSpace(ClientSecret);
}
