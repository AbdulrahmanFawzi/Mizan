using Mizan.Domain.Budgets;
using Mizan.Domain.Notifications;
using static Mizan.Domain.Tests.TestData;

namespace Mizan.Domain.Tests.Notifications;

public class NotificationTests
{
    [Fact]
    public void BudgetThreshold_StoresDetails()
    {
        var categoryId = Guid.NewGuid();

        var notification = Notification.BudgetThreshold(Guid.NewGuid(), categoryId, "الوقود", AlertThreshold.Ninety, Now);

        Assert.Equal(NotificationType.BudgetThreshold, notification.Type);
        Assert.Equal("90", notification.Data["threshold"]);
        Assert.Equal("الوقود", notification.Data["categoryName"]);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public void MarkAsRead_KeepsFirstReadTime()
    {
        var notification = Notification.GoalCompleted(Guid.NewGuid(), Guid.NewGuid(), "Travel", Now);

        notification.MarkAsRead(Now);
        notification.MarkAsRead(Now.AddHours(1));

        Assert.Equal(Now, notification.ReadAt);
    }
}
