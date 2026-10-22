using System.Security.Claims;
using Mizan.Api.Infrastructure;
using Mizan.Application.Budgets;

namespace Mizan.Api.Endpoints;

internal static class BudgetEndpoints
{
    public static void MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/budgets").WithTags("Budgets").RequireAuthorization();

        group.MapGet("/current", async (ClaimsPrincipal user, BudgetService budgets, CancellationToken ct) =>
            TypedResults.Ok(await budgets.GetCurrentAsync(user.GetUserId(), ct)));

        // The budget of a past period, e.g. /api/budgets?date=2026-09-15
        group.MapGet("/", async (DateOnly date, ClaimsPrincipal user, BudgetService budgets, CancellationToken ct) =>
            TypedResults.Ok(await budgets.GetForDateAsync(user.GetUserId(), date, ct)));

        group.MapPut("/current/categories/{categoryId:guid}/limit", async (
                Guid categoryId, SetLimitCommand command, ClaimsPrincipal user, BudgetService budgets, CancellationToken ct) =>
            TypedResults.Ok(await budgets.SetLimitAsync(user.GetUserId(), categoryId, command, ct)));
    }
}
