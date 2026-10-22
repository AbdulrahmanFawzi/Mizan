using System.Security.Claims;
using Mizan.Api.Infrastructure;
using Mizan.Application.Dashboard;

namespace Mizan.Api.Endpoints;

internal static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", async (ClaimsPrincipal user, DashboardService dashboard, CancellationToken ct) =>
                TypedResults.Ok(await dashboard.GetAsync(user.GetUserId(), ct)))
            .WithTags("Dashboard")
            .RequireAuthorization();
    }
}
