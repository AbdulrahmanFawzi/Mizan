using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Mizan.Api.Infrastructure;
using Mizan.Application.Auth;
using Mizan.Application.Profile;

namespace Mizan.Api.Endpoints;

internal static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/me").WithTags("Profile").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user, ProfileService profile, CancellationToken ct) =>
            TypedResults.Ok(await profile.GetAsync(user.GetUserId(), ct)));

        group.MapPut("/", async (UpdateProfileCommand command, ClaimsPrincipal user, ProfileService profile, CancellationToken ct) =>
            TypedResults.Ok(await profile.UpdateAsync(user.GetUserId(), command, ct)));

        // UC-09: downloaded as a file, named with the date.
        group.MapGet("/export", async (ClaimsPrincipal user, DataExportService export, IOptions<JsonOptions> json, CancellationToken ct) =>
        {
            var data = await export.ExportAsync(user.GetUserId(), ct);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(data, new JsonSerializerOptions(json.Value.SerializerOptions) { WriteIndented = true });
            return TypedResults.File(bytes, "application/json", $"halalati-data-{data.ExportedAt:yyyy-MM-dd}.json");
        });

        group.MapPost("/email/send-code", async (ClaimsPrincipal user, AuthService auth, CancellationToken ct) =>
        {
            await auth.SendVerificationAsync(user.GetUserId(), ct);
            return TypedResults.NoContent();
        }).RequireRateLimiting(ApiServices.AuthRateLimitPolicy);

        group.MapPost("/email/verify", async (VerifyEmailCommand command, ClaimsPrincipal user, AuthService auth, CancellationToken ct) =>
        {
            await auth.VerifyEmailAsync(user.GetUserId(), command, ct);
            return TypedResults.NoContent();
        }).RequireRateLimiting(ApiServices.AuthRateLimitPolicy);

        group.MapDelete("/", async (ClaimsPrincipal user, ProfileService profile, CancellationToken ct) =>
        {
            await profile.DeleteAccountAsync(user.GetUserId(), ct);
            return TypedResults.NoContent();
        });
    }
}
