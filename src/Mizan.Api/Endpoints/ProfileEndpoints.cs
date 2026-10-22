using System.Security.Claims;
using Mizan.Api.Infrastructure;
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

        group.MapDelete("/", async (ClaimsPrincipal user, ProfileService profile, CancellationToken ct) =>
        {
            await profile.DeleteAccountAsync(user.GetUserId(), ct);
            return TypedResults.NoContent();
        });
    }
}
