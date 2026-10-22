using Mizan.Application.Abstractions;
using Mizan.Application.Budgets;
using Mizan.Application.Common;
using Mizan.Domain.Common;
using Mizan.Domain.Transactions;

namespace Mizan.Application.EventHandlers;

public sealed class EvaluateBudgetOnTransactionRecorded(BudgetAlerts alerts, GoalCompletion goals)
    : IDomainEventHandler<TransactionRecorded>
{
    public async Task HandleAsync(TransactionRecorded e, CancellationToken cancellationToken)
    {
        await alerts.EvaluateAsync(e.UserId, e.CategoryId, e.Date, cancellationToken);
        await goals.CheckAsync(e.UserId, e.GoalId, cancellationToken);
    }
}

// BR-T5: spending only went up in the transaction's (possibly new) category and period.
// TransactionDeleted needs no handler: less spending can never reach a new threshold.
public sealed class EvaluateBudgetOnTransactionUpdated(BudgetAlerts alerts, GoalCompletion goals)
    : IDomainEventHandler<TransactionUpdated>
{
    public async Task HandleAsync(TransactionUpdated e, CancellationToken cancellationToken)
    {
        await alerts.EvaluateAsync(e.UserId, e.CategoryId, e.Date, cancellationToken);
        await goals.CheckAsync(e.UserId, e.GoalId, cancellationToken);
    }
}

public sealed class BudgetAlerts(
    IUserRepository users,
    ICategoryRepository categories,
    ITransactionRepository transactions,
    BudgetProvisioner provisioner,
    Clock clock)
{
    public async Task EvaluateAsync(Guid userId, Guid categoryId, DateOnly date, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User");
        var budget = await provisioner.GetOrCreateCurrentAsync(user, cancellationToken);

        // BR-A5: only spending in the current period can raise alerts.
        if (!budget.Period.Contains(date))
        {
            return;
        }

        var category = await categories.GetAsync(userId, categoryId, cancellationToken);
        if (category is null)
        {
            return;
        }

        var sums = await transactions.SumByCategoryAsync(userId, budget.Period.Start, budget.Period.End, cancellationToken);
        var spent = Money.Of(sums.GetValueOrDefault(categoryId), user.Currency);

        budget.EvaluateThresholds(categoryId, category.Kind, spent, clock.Today);
    }
}

public sealed class GoalCompletion(
    IUserRepository users,
    IGoalRepository goals,
    ITransactionRepository transactions,
    Clock clock)
{
    public async Task CheckAsync(Guid userId, Guid? goalId, CancellationToken cancellationToken)
    {
        if (goalId is null)
        {
            return;
        }

        var user = await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User");
        var goal = await goals.GetAsync(userId, goalId.Value, cancellationToken);
        if (goal is null)
        {
            return;
        }

        var contributions = await transactions.SumByGoalAsync(userId, cancellationToken);
        goal.RegisterContributions(Money.Of(contributions.GetValueOrDefault(goal.Id), user.Currency), clock.Now);
    }
}
