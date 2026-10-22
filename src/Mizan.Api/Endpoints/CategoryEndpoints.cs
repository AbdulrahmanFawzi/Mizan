using System.Security.Claims;
using Mizan.Api.Infrastructure;
using Mizan.Application.Categories;

namespace Mizan.Api.Endpoints;

internal static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories").WithTags("Categories").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user, CategoryService categories, CancellationToken ct) =>
            TypedResults.Ok(await categories.ListAsync(user.GetUserId(), ct)));

        group.MapPost("/", async (CreateCategoryCommand command, ClaimsPrincipal user, CategoryService categories, CancellationToken ct) =>
        {
            var category = await categories.CreateAsync(user.GetUserId(), command, ct);
            return TypedResults.Created($"/api/categories/{category.Id}", category);
        });

        group.MapPut("/{id:guid}", async (Guid id, RenameCategoryCommand command, ClaimsPrincipal user, CategoryService categories, CancellationToken ct) =>
            TypedResults.Ok(await categories.RenameAsync(user.GetUserId(), id, command, ct)));

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, CategoryService categories, CancellationToken ct) =>
        {
            await categories.DeleteAsync(user.GetUserId(), id, ct);
            return TypedResults.NoContent();
        });
    }
}
