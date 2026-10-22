using Mizan.Domain.Notifications;

namespace Mizan.Application.Notifications;

// The text is built when shown, so wording can change without touching stored notifications.
internal static class NotificationText
{
    public static (string Title, string Message) For(Notification notification) => notification.Type switch
    {
        NotificationType.BudgetThreshold when notification.Data["threshold"] == "100" =>
            ("تجاوز الميزانية", $"⚠️ تجاوزت ميزانية {notification.Data["categoryName"]}."),

        NotificationType.BudgetThreshold =>
            ("تنبيه الميزانية", $"⚠️ استخدمت {notification.Data["threshold"]}% من ميزانية {notification.Data["categoryName"]}."),

        NotificationType.GoalCompleted =>
            ("هدف مكتمل", $"🎉 مبروك! وصلت لهدفك: {notification.Data["goalName"]}."),

        _ => ("إشعار", string.Empty),
    };
}
