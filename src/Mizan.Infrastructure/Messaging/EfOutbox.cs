using System.Text.Json;
using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Infrastructure.Persistence;

namespace Mizan.Infrastructure.Messaging;

// Adds the message to the same DbContext, so it's saved in the same transaction as the change.
internal sealed class EfOutbox(MizanDbContext db, Clock clock) : IOutbox
{
    public void Enqueue(string type, object payload) => db.Outbox.Add(new OutboxMessage
    {
        Id = Guid.CreateVersion7(),
        Type = type,
        Payload = JsonSerializer.Serialize(payload, payload.GetType(), JsonSerializerOptions.Web),
        CreatedAt = clock.Now,
        NextAttemptAt = clock.Now,
    });
}
