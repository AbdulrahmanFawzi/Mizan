using System.Security.Claims;
using Mizan.Api.Infrastructure;
using Mizan.Application.Notifications;

namespace Mizan.Api.Endpoints;

internal static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();

        group.MapGet("/", async (bool? unreadOnly, ClaimsPrincipal user, NotificationService notifications, CancellationToken ct) =>
            TypedResults.Ok(await notifications.ListAsync(user.GetUserId(), unreadOnly ?? false, ct)));

        group.MapPost("/{id:guid}/read", async (Guid id, ClaimsPrincipal user, NotificationService notifications, CancellationToken ct) =>
        {
            await notifications.MarkAsReadAsync(user.GetUserId(), id, ct);
            return TypedResults.NoContent();
        });
    }
}
