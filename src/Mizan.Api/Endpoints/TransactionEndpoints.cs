using System.Security.Claims;
using Mizan.Api.Infrastructure;
using Mizan.Application.Transactions;

namespace Mizan.Api.Endpoints;

internal static class TransactionEndpoints
{
    public static void MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transactions").WithTags("Transactions").RequireAuthorization();

        group.MapGet("/", async (
                DateOnly? from, DateOnly? to, Guid? categoryId, int? page, int? pageSize,
                ClaimsPrincipal user, TransactionService transactions, CancellationToken ct) =>
            TypedResults.Ok(await transactions.ListAsync(
                user.GetUserId(),
                new TransactionQuery(from, to, categoryId, page ?? 1, pageSize ?? 20),
                ct)));

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, TransactionService transactions, CancellationToken ct) =>
            TypedResults.Ok(await transactions.GetAsync(user.GetUserId(), id, ct)));

        group.MapPost("/", async (RecordExpenseCommand command, ClaimsPrincipal user, TransactionService transactions, CancellationToken ct) =>
        {
            var transaction = await transactions.RecordAsync(user.GetUserId(), command, ct);
            return TypedResults.Created($"/api/transactions/{transaction.Id}", transaction);
        });

        group.MapPut("/{id:guid}", async (Guid id, RecordExpenseCommand command, ClaimsPrincipal user, TransactionService transactions, CancellationToken ct) =>
            TypedResults.Ok(await transactions.UpdateAsync(user.GetUserId(), id, command, ct)));

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, TransactionService transactions, CancellationToken ct) =>
        {
            await transactions.DeleteAsync(user.GetUserId(), id, ct);
            return TypedResults.NoContent();
        });
    }
}
