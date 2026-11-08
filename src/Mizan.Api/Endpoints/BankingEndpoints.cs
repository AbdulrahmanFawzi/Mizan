using System.Security.Claims;
using Mizan.Api.Infrastructure;
using Mizan.Application.Banking;
using Mizan.Infrastructure.Banking.Lean;
using Mizan.Infrastructure.Messaging;

namespace Mizan.Api.Endpoints;

public sealed record SetAccountIncludedRequest(bool IncludeInBudget);

internal static class BankingEndpoints
{
    public static void MapBankingEndpoints(this IEndpointRouteBuilder app)
    {
        var connections = app.MapGroup("/api/bank-connections").WithTags("Bank connections").RequireAuthorization();

        // UC-70: what the app passes to the provider's link screen.
        connections.MapPost("/link", async (ClaimsPrincipal user, BankConnectionService banking, CancellationToken ct) =>
            TypedResults.Ok(await banking.CreateLinkSessionAsync(user.GetUserId(), ct)));

        // Right after the link screen closes: picks up the new link and imports it.
        connections.MapPost("/discover", async (ClaimsPrincipal user, BankConnectionService banking, CancellationToken ct) =>
            TypedResults.Ok(await banking.DiscoverAsync(user.GetUserId(), ct)));

        // UC-72
        connections.MapGet("/", async (ClaimsPrincipal user, BankConnectionService banking, CancellationToken ct) =>
            TypedResults.Ok(await banking.ListAsync(user.GetUserId(), ct)));

        // OB-6: the app calls this when it opens.
        connections.MapPost("/sync", async (ClaimsPrincipal user, BankConnectionService banking, CancellationToken ct) =>
            TypedResults.Ok(await banking.SyncAllAsync(user.GetUserId(), ct)));

        // UC-82
        connections.MapPost("/{id:guid}/sync", async (Guid id, ClaimsPrincipal user, BankConnectionService banking, CancellationToken ct) =>
            TypedResults.Ok(await banking.SyncAsync(user.GetUserId(), id, ct)));

        // UC-74 (BR-C9): ?keepTransactions=false deletes what this bank imported.
        connections.MapDelete("/{id:guid}", async (
            Guid id, bool? keepTransactions, ClaimsPrincipal user, BankConnectionService banking, CancellationToken ct) =>
        {
            await banking.RevokeAsync(user.GetUserId(), id, keepTransactions ?? true, ct);
            return TypedResults.NoContent();
        });

        // BR-X10
        app.MapPatch("/api/bank-accounts/{id:guid}", async (
                Guid id, SetAccountIncludedRequest request, ClaimsPrincipal user, BankConnectionService banking, CancellationToken ct) =>
            TypedResults.Ok(await banking.SetAccountIncludedAsync(user.GetUserId(), id, request.IncludeInBudget, ct)))
            .WithTags("Bank connections")
            .RequireAuthorization();

        // Lean calls this. No login: the HMAC signature proves it's Lean (BR-S8).
        // The body is stored and answered at once (Lean waits 10 seconds); processing happens in the background.
        app.MapPost("/api/webhooks/lean", async (HttpRequest request, LeanWebhooks webhooks, WebhookInbox inbox, CancellationToken ct) =>
        {
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync(ct);

            if (!webhooks.IsSignatureValid(body, request.Headers[LeanWebhooks.SignatureHeader]))
            {
                return Results.Unauthorized();
            }

            if (LeanWebhooks.Parse(body) is { } parsed)
            {
                await inbox.SaveAsync("lean", parsed.EventId, parsed.Type, parsed.Payload, ct);
            }

            return Results.Ok();
        })
        .WithTags("Webhooks")
        .AllowAnonymous()
        .ExcludeFromDescription();
    }
}
