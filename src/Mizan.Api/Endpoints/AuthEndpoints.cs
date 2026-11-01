using Mizan.Api.Infrastructure;
using Mizan.Application.Auth;

namespace Mizan.Api.Endpoints;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .AllowAnonymous()
            .RequireRateLimiting(ApiServices.AuthRateLimitPolicy);

        group.MapPost("/register", async (RegisterCommand command, AuthService auth, CancellationToken ct) =>
            TypedResults.Ok(await auth.RegisterAsync(command, ct)));

        group.MapPost("/login", async (LoginCommand command, AuthService auth, CancellationToken ct) =>
            TypedResults.Ok(await auth.LoginAsync(command, ct)));

        group.MapPost("/refresh", async (RefreshCommand command, AuthService auth, CancellationToken ct) =>
            TypedResults.Ok(await auth.RefreshAsync(command, ct)));

        group.MapPost("/forgot-password", async (ForgotPasswordCommand command, AuthService auth, CancellationToken ct) =>
        {
            await auth.ForgotPasswordAsync(command, ct);
            return TypedResults.NoContent();
        });

        group.MapPost("/reset-password", async (ResetPasswordCommand command, AuthService auth, CancellationToken ct) =>
            TypedResults.Ok(await auth.ResetPasswordAsync(command, ct)));

        group.MapPost("/logout", async (RefreshCommand command, AuthService auth, CancellationToken ct) =>
        {
            await auth.LogoutAsync(command, ct);
            return TypedResults.NoContent();
        });
    }
}
