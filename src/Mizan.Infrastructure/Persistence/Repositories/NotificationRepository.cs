using Microsoft.EntityFrameworkCore;
using Mizan.Application.Abstractions;
using Mizan.Domain.Notifications;

namespace Mizan.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository(MizanDbContext db) : INotificationRepository
{
    public Task<Notification?> GetAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken) =>
        db.Notifications.SingleOrDefaultAsync(n => n.UserId == userId && n.Id == notificationId, cancellationToken);

    public async Task<IReadOnlyList<Notification>> ListAsync(Guid userId, bool unreadOnly, int take, CancellationToken cancellationToken) =>
        await db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && (!unreadOnly || n.ReadAt == null))
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Notifications.CountAsync(n => n.UserId == userId && n.ReadAt == null, cancellationToken);

    public void Add(Notification notification) => db.Notifications.Add(notification);
}
