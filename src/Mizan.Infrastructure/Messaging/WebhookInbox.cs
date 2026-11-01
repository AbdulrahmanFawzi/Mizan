using Microsoft.EntityFrameworkCore;
using Mizan.Application.Common;
using Mizan.Infrastructure.Persistence;
using Npgsql;

namespace Mizan.Infrastructure.Messaging;

// Stores a verified webhook. A second delivery of the same event is ignored (BR-S8).
public sealed class WebhookInbox(MizanDbContext db, Clock clock)
{
    public async Task SaveAsync(string provider, string eventId, string type, string payload, CancellationToken cancellationToken)
    {
        if (await db.Inbox.AnyAsync(m => m.EventId == eventId, cancellationToken))
        {
            return;
        }

        db.Inbox.Add(new InboxMessage
        {
            EventId = eventId,
            Provider = provider,
            Type = type,
            Payload = payload,
            ReceivedAt = clock.Now,
            NextAttemptAt = clock.Now,
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // The same event arrived twice at the same moment; the first one wins.
        }
    }
}
