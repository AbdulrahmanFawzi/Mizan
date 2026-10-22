using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Domain.Notifications;

namespace Mizan.Application.Notifications;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    IReadOnlyDictionary<string, string> Data,
    DateTimeOffset CreatedAt,
    bool IsRead);

public sealed class NotificationService(INotificationRepository notifications, IUnitOfWork unitOfWork, Clock clock)
{
    public const int MaxItems = 50;

    // UC-60
    public async Task<IReadOnlyList<NotificationDto>> ListAsync(Guid userId, bool unreadOnly, CancellationToken cancellationToken)
    {
        var list = await notifications.ListAsync(userId, unreadOnly, MaxItems, cancellationToken);
        return list.Select(ToDto).ToList();
    }

    // UC-61
    public async Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var notification = await notifications.GetAsync(userId, notificationId, cancellationToken)
            ?? throw new NotFoundException("Notification");

        notification.MarkAsRead(clock.Now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static NotificationDto ToDto(Notification notification)
    {
        var (title, message) = NotificationText.For(notification);
        return new NotificationDto(
            notification.Id,
            notification.Type,
            title,
            message,
            notification.Data,
            notification.CreatedAt,
            notification.IsRead);
    }
}
