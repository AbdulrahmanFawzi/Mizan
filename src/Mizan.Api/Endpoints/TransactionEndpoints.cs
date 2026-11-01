using System.Security.Claims;
using Mizan.Api.Infrastructure;
using Mizan.Application.Transactions;

namespace Mizan.Api.Endpoints;

public sealed record ResolveReviewRequest(ReviewDecision Decision);

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

        // UC-92, UC-93: bank transactions waiting for the user's decision.
        group.MapGet("/review", async (ClaimsPrincipal user, ReviewService review, CancellationToken ct) =>
            TypedResults.Ok(await review.ListAsync(user.GetUserId(), ct)));

        group.MapPost("/{id:guid}/review", async (Guid id, ResolveReviewRequest request, ClaimsPrincipal user, ReviewService review, CancellationToken ct) =>
            TypedResults.Ok(await review.ResolveAsync(user.GetUserId(), id, request.Decision, ct)));

        // UC-91 (works for manual and bank transactions).
        group.MapPut("/{id:guid}/category", async (Guid id, ChangeCategoryCommand command, ClaimsPrincipal user, ReviewService review, CancellationToken ct) =>
            TypedResults.Ok(await review.ChangeCategoryAsync(user.GetUserId(), id, command, ct)));

        group.MapPut("/{id:guid}/excluded", async (Guid id, SetExcludedCommand command, ClaimsPrincipal user, ReviewService review, CancellationToken ct) =>
            TypedResults.Ok(await review.SetExcludedAsync(user.GetUserId(), id, command.Excluded, ct)));

        group.MapPut("/{id:guid}/note", async (Guid id, SetNoteCommand command, ClaimsPrincipal user, ReviewService review, CancellationToken ct) =>
            TypedResults.Ok(await review.SetNoteAsync(user.GetUserId(), id, command.Note, ct)));
    }
}
