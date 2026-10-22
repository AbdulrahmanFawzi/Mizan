using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Domain.Budgets;
using Mizan.Domain.Goals;
using Mizan.Domain.Notifications;

namespace Mizan.Application.EventHandlers;

public sealed class NotifyOnBudgetThresholdReached(
    ICategoryRepository categories,
    INotificationRepository notifications,
    Clock clock) : IDomainEventHandler<BudgetThresholdReached>
{
    public async Task HandleAsync(BudgetThresholdReached e, CancellationToken cancellationToken)
    {
        var category = await categories.GetAsync(e.UserId, e.CategoryId, cancellationToken);
        if (category is null)
        {
            return;
        }

        notifications.Add(Notification.BudgetThreshold(e.UserId, e.CategoryId, category.Name.Value, e.Threshold, clock.Now));
    }
}

public sealed class NotifyOnGoalCompleted(
    IGoalRepository goals,
    INotificationRepository notifications,
    Clock clock) : IDomainEventHandler<GoalCompleted>
{
    public async Task HandleAsync(GoalCompleted e, CancellationToken cancellationToken)
    {
        var goal = await goals.GetAsync(e.UserId, e.GoalId, cancellationToken);
        if (goal is null)
        {
            return;
        }

        notifications.Add(Notification.GoalCompleted(e.UserId, e.GoalId, goal.Name.Value, clock.Now));
    }
}
