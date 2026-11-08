using Microsoft.EntityFrameworkCore;
using Mizan.Application.Abstractions;

namespace Mizan.Infrastructure.Persistence;

// A Postgres advisory lock per bank connection. It lives as long as the database session,
// so if the app crashes mid-sync the lock is released with the connection.
internal sealed class PostgresSyncLock(MizanDbContext db) : ISyncLock
{
    public async Task<IAsyncDisposable?> TryAcquireAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        var key = BitConverter.ToInt64(connectionId.ToByteArray(), 0);

        await db.Database.OpenConnectionAsync(cancellationToken);
        var acquired = await db.Database
            .SqlQuery<bool>($"SELECT pg_try_advisory_lock({key}) AS \"Value\"")
            .SingleAsync(cancellationToken);

        if (!acquired)
        {
            await db.Database.CloseConnectionAsync();
            return null;
        }

        return new Release(db, key);
    }

    private sealed class Release(MizanDbContext db, long key) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({key})");
            await db.Database.CloseConnectionAsync();
        }
    }
}
