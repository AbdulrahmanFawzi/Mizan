using Mizan.Domain.Budgets;
using Mizan.Domain.Common;

namespace Mizan.Domain.Notifications;

public sealed class Notification : AggregateRoot
{
    private Dictionary<string, string> _data = [];

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public IReadOnlyDictionary<string, string> Data => _data;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    public bool IsRead => ReadAt is not null;

    private Notification()
    {
    }

    private Notification(Guid userId, NotificationType type, Dictionary<string, string> data, DateTimeOffset now)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        Type = type;
        _data = data;
        CreatedAt = now;
    }

    public static Notification BudgetThreshold(
        Guid userId,
        Guid categoryId,
        string categoryName,
        AlertThreshold threshold,
        DateTimeOffset now) =>
        new(userId, NotificationType.BudgetThreshold, new Dictionary<string, string>
        {
            ["categoryId"] = categoryId.ToString(),
            ["categoryName"] = categoryName,
            ["threshold"] = threshold.Percent.ToString(),
        }, now);

    public static Notification GoalCompleted(Guid userId, Guid goalId, string goalName, DateTimeOffset now) =>
        new(userId, NotificationType.GoalCompleted, new Dictionary<string, string>
        {
            ["goalId"] = goalId.ToString(),
            ["goalName"] = goalName,
        }, now);

    public void MarkAsRead(DateTimeOffset now) => ReadAt ??= now;
}
