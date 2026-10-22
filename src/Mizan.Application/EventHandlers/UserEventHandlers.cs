using Mizan.Application.Abstractions;
using Mizan.Application.Budgets;
using Mizan.Application.Common;
using Mizan.Domain.Categories;
using Mizan.Domain.Common;
using Mizan.Domain.Users;

namespace Mizan.Application.EventHandlers;

// Every new user starts with "Other" and "Savings" (BR-B3) and a budget for the current period.
public sealed class CreateStarterBudgetOnUserRegistered(
    IUserRepository users,
    ICategoryRepository categories,
    BudgetProvisioner provisioner) : IDomainEventHandler<UserRegistered>
{
    public async Task HandleAsync(UserRegistered domainEvent, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(domainEvent.UserId, cancellationToken) ?? throw new NotFoundException("User");
        var budget = await provisioner.GetOrCreateCurrentAsync(user, cancellationToken);

        foreach (var category in Category.CreateSystemCategories(user.Id))
        {
            categories.Add(category);
            budget.SetLimit(category.Id, Money.Zero(user.Currency));
        }
    }
}

// BR-I2: the current budget follows the new income; category limits stay as they are.
public sealed class UpdateBudgetOnMonthlyIncomeChanged(
    IBudgetRepository budgets,
    Clock clock) : IDomainEventHandler<MonthlyIncomeChanged>
{
    public async Task HandleAsync(MonthlyIncomeChanged domainEvent, CancellationToken cancellationToken)
    {
        var budget = await budgets.GetContainingAsync(domainEvent.UserId, clock.Today, cancellationToken);
        budget?.ChangePlannedIncome(domainEvent.MonthlyIncome);
    }
}
