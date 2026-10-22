using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Domain.Common;

namespace Mizan.Application.Budgets;

public sealed class BudgetService(
    IUserRepository users,
    IBudgetRepository budgets,
    ICategoryRepository categories,
    BudgetProvisioner provisioner,
    BudgetReader reader,
    IUnitOfWork unitOfWork,
    Clock clock)
{
    // UC-24, UC-51
    public async Task<BudgetDto> GetCurrentAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User");
        var budget = await provisioner.GetOrCreateCurrentAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var snapshot = await reader.ReadAsync(budget, cancellationToken);
        return snapshot.ToDto(clock.Today);
    }

    // Past budgets are read-only history.
    public async Task<BudgetDto> GetForDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken)
    {
        var budget = await budgets.GetContainingAsync(userId, date, cancellationToken)
            ?? throw new NotFoundException("Budget");

        var snapshot = await reader.ReadAsync(budget, cancellationToken);
        return snapshot.ToDto(clock.Today);
    }

    // UC-22
    public async Task<BudgetDto> SetLimitAsync(Guid userId, Guid categoryId, SetLimitCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User");
        _ = await categories.GetAsync(userId, categoryId, cancellationToken) ?? throw new NotFoundException("Category");

        var budget = await provisioner.GetOrCreateCurrentAsync(user, cancellationToken);
        budget.SetLimit(categoryId, Money.Of(command.Limit, user.Currency));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var snapshot = await reader.ReadAsync(budget, cancellationToken);
        return snapshot.ToDto(clock.Today);
    }
}
