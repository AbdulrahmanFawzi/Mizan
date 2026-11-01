using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Mizan.Application.Banking;

namespace Mizan.Infrastructure.Banking.Lean;

public sealed record ParsedWebhook(string EventId, string Type, string Payload);

// Checks that a webhook really came from Lean and turns it into our own signals.
public sealed class LeanWebhooks(IOptions<LeanOptions> options)
{
    public const string SignatureHeader = "lean-signature";

    // Header: "sha512=<hex>", an HMAC-SHA512 of the raw body keyed with the webhook secret.
    public bool IsSignatureValid(string rawBody, string? header)
    {
        var secret = options.Value.WebhookSecret;
        if (string.IsNullOrEmpty(secret) || string.IsNullOrWhiteSpace(header))
        {
            return false;
        }

        var received = header.StartsWith("sha512=", StringComparison.OrdinalIgnoreCase) ? header[7..] : header;
        var expected = Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(rawBody)));

        // Constant-time comparison so the signature can't be guessed byte by byte.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected.ToLowerInvariant()),
            Encoding.ASCII.GetBytes(received.Trim().ToLowerInvariant()));
    }

    public static ParsedWebhook? Parse(string rawBody)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(rawBody);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }

        var type = root?["type"]?.GetValue<string>();
        if (root is null || string.IsNullOrWhiteSpace(type))
        {
            return null;
        }

        // Lean recommends event_id as the idempotency key. If it's missing, the body's hash does the job.
        var eventId = root["event_id"]?.GetValue<string>()
            ?? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody)));

        return new ParsedWebhook(eventId, type, (root["payload"] ?? root).ToJsonString());
    }

    // Null for events we don't act on.
    public static BankingSignal? ToSignal(string type, string payloadJson)
    {
        var payload = JsonNode.Parse(payloadJson);
        var entityId = Text(payload, "entity_id") ?? Text(payload, "id");
        if (entityId is null)
        {
            return null;
        }

        return type switch
        {
            "entity.created" when Text(payload, "customer_id") is { } customerId => new BankingSignal.EntityConnected(customerId, entityId),
            "entity.data.refresh.updated" when Text(payload, "status") == "FINISHED" => new BankingSignal.DataReady(entityId),
            "entity.updated" => new BankingSignal.ConsentChanged(entityId),
            _ => null,
        };
    }

    private static string? Text(JsonNode? node, string key) =>
        node is JsonObject obj && obj[key] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
