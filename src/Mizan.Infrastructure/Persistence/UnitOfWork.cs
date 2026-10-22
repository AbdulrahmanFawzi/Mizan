using Microsoft.EntityFrameworkCore;
using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Application.EventHandlers;
using Mizan.Domain.Common;
using Npgsql;

namespace Mizan.Infrastructure.Persistence;

// One request = one database transaction.
// Changes are saved first so event handlers see them in queries (e.g. the new spending total),
// then the events are dispatched. Handlers may change more data and raise more events,
// so this repeats until nothing is left, and only then commits.
internal sealed class UnitOfWork(MizanDbContext db, DomainEventDispatcher dispatcher) : IUnitOfWork
{
    private const int MaxDispatchRounds = 10;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            for (var round = 1; ; round++)
            {
                var events = CollectDomainEvents();
                await db.SaveChangesAsync(cancellationToken);

                if (events.Count == 0)
                {
                    break;
                }

                if (round > MaxDispatchRounds)
                {
                    throw new InvalidOperationException("Domain events kept raising new events; possible loop.");
                }

                foreach (var domainEvent in events)
                {
                    await dispatcher.DispatchAsync(domainEvent, cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two requests passed the application check at the same time; the unique index caught it.
            throw new ConflictException("This item already exists.");
        }
    }

    private List<IDomainEvent> CollectDomainEvents()
    {
        var aggregates = db.ChangeTracker.Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());
        return events;
    }
}
