using System.Security.Claims;
using Mizan.Api.Infrastructure;
using Mizan.Application.Goals;

namespace Mizan.Api.Endpoints;

internal static class GoalEndpoints
{
    public static void MapGoalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/goals").WithTags("Goals").RequireAuthorization();

        group.MapGet("/", async (bool? includeArchived, ClaimsPrincipal user, GoalService goals, CancellationToken ct) =>
            TypedResults.Ok(await goals.ListAsync(user.GetUserId(), includeArchived ?? false, ct)));

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, GoalService goals, CancellationToken ct) =>
            TypedResults.Ok(await goals.GetAsync(user.GetUserId(), id, ct)));

        group.MapPost("/", async (CreateGoalCommand command, ClaimsPrincipal user, GoalService goals, CancellationToken ct) =>
        {
            var goal = await goals.CreateAsync(user.GetUserId(), command, ct);
            return TypedResults.Created($"/api/goals/{goal.Id}", goal);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateGoalCommand command, ClaimsPrincipal user, GoalService goals, CancellationToken ct) =>
            TypedResults.Ok(await goals.UpdateAsync(user.GetUserId(), id, command, ct)));

        group.MapPost("/{id:guid}/archive", async (Guid id, ClaimsPrincipal user, GoalService goals, CancellationToken ct) =>
        {
            await goals.ArchiveAsync(user.GetUserId(), id, ct);
            return TypedResults.NoContent();
        });

        group.MapPost("/{id:guid}/contributions", async (Guid id, ContributeCommand command, ClaimsPrincipal user, GoalService goals, CancellationToken ct) =>
        {
            var transaction = await goals.ContributeAsync(user.GetUserId(), id, command, ct);
            return TypedResults.Created($"/api/transactions/{transaction.Id}", transaction);
        });
    }
}
