namespace Mizan.Infrastructure.Messaging;

// A webhook as it arrived. Saved first and answered with 200 right away (Lean allows 10 seconds),
// then processed in the background. The event id makes a repeated delivery a no-op (BR-S8).
public sealed class InboxMessage
{
    public string EventId { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public string? LastError { get; set; }
}

// Work for an outside system, saved in the same transaction as the change that needs it.
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
