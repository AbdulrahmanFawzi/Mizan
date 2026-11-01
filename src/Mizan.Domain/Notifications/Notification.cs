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

    // BR-C5
    public static Notification ConsentExpiring(Guid userId, Guid connectionId, string bankName, int daysLeft, DateOnly expiresOn, DateTimeOffset now) =>
        new(userId, NotificationType.ConsentExpiring, new Dictionary<string, string>
        {
            ["connectionId"] = connectionId.ToString(),
            ["bankName"] = bankName,
            ["daysLeft"] = daysLeft.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["expiresOn"] = expiresOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        }, now);

    // BR-C6
    public static Notification ConsentExpired(Guid userId, Guid connectionId, string bankName, DateTimeOffset now) =>
        new(userId, NotificationType.ConsentExpired, new Dictionary<string, string>
        {
            ["connectionId"] = connectionId.ToString(),
            ["bankName"] = bankName,
        }, now);

    // OB-1 and OB-3: some imported transactions need the user's decision.
    public static Notification ReviewNeeded(Guid userId, int count, DateTimeOffset now) =>
        new(userId, NotificationType.ReviewNeeded, new Dictionary<string, string>
        {
            ["count"] = count.ToString(System.Globalization.CultureInfo.InvariantCulture),
        }, now);

    public void MarkAsRead(DateTimeOffset now) => ReadAt ??= now;
}
