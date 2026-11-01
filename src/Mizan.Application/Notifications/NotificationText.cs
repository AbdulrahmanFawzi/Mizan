using Mizan.Application.Common;
using Mizan.Domain.Notifications;

namespace Mizan.Application.Notifications;

// The text is built when shown, in the reader's language, so wording can change without touching stored notifications.
internal static class NotificationText
{
    public static (string Title, string Message) For(Notification notification)
    {
        var data = notification.Data;
        return notification.Type switch
        {
            NotificationType.BudgetThreshold when data["threshold"] == "100" => Lang.IsEnglish
                ? ("Over budget", $"⚠️ You went over your {data["categoryName"]} budget.")
                : ("تجاوز الميزانية", $"⚠️ تجاوزت ميزانية {data["categoryName"]}."),

            NotificationType.BudgetThreshold => Lang.IsEnglish
                ? ("Budget alert", $"⚠️ You've used {data["threshold"]}% of your {data["categoryName"]} budget.")
                : ("تنبيه الميزانية", $"⚠️ استخدمت {data["threshold"]}% من ميزانية {data["categoryName"]}."),

            NotificationType.GoalCompleted => Lang.IsEnglish
                ? ("Goal reached", $"🎉 Well done! You reached your goal: {data["goalName"]}.")
                : ("هدف مكتمل", $"🎉 مبروك! وصلت لهدفك: {data["goalName"]}."),

            NotificationType.ConsentExpiring => Lang.IsEnglish
                ? ("Bank link ends soon", $"🔗 Your {data["bankName"]} link ends in {data["daysLeft"]} days. Renew it to keep your spending coming in.")
                : ("ربط البنك ينتهي قريبًا", $"🔗 ربط {data["bankName"]} ينتهي بعد {data["daysLeft"]} يوم. جدّده عشان تستمر مصروفاتك تدخل تلقائيًا."),

            NotificationType.ConsentExpired => Lang.IsEnglish
                ? ("Bank link ended", $"🔗 Your {data["bankName"]} link has ended. Link it again to bring your spending back in.")
                : ("انتهى ربط البنك", $"🔗 انتهى ربط {data["bankName"]}. أعد الربط عشان ترجع المصروفات تدخل تلقائيًا."),

            NotificationType.ReviewNeeded => Lang.IsEnglish
                ? ("Transactions to review", $"👀 {data["count"]} bank transactions need your decision.")
                : ("عمليات تحتاج مراجعتك", $"👀 عندك {data["count"]} عمليات من البنك تحتاج قرارك."),

            _ => (Lang.Pick("إشعار", "Notification"), string.Empty),
        };
    }
}
