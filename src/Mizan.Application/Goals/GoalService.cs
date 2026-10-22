using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Application.Transactions;
using Mizan.Domain.Categories;
using Mizan.Domain.Common;
using Mizan.Domain.Goals;
using Mizan.Domain.Transactions;
using Mizan.Domain.Users;

namespace Mizan.Application.Goals;

public sealed record GoalDto(
    Guid Id,
    string Name,
    MoneyDto TargetAmount,
    MoneyDto InitialAmount,
    DateOnly TargetDate,
    MoneyDto CurrentAmount,
    MoneyDto Remaining,
    MoneyDto RequiredPerPeriod,
    int RemainingPeriods,
    decimal PercentComplete,
    GoalStatus Status);

public sealed record CreateGoalCommand(string Name, decimal TargetAmount, decimal InitialAmount, DateOnly TargetDate);

public sealed record UpdateGoalCommand(string Name, decimal TargetAmount, DateOnly TargetDate);

public sealed record ContributeCommand(decimal Amount, DateOnly? Date = null);

public sealed class GoalService(
    IUserRepository users,
    IGoalRepository goals,
    ICategoryRepository categories,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    Clock clock)
{
    // UC-43
    public async Task<IReadOnlyList<GoalDto>> ListAsync(Guid userId, bool includeArchived, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        var list = await goals.ListAsync(userId, includeArchived, cancellationToken);
        var contributions = await transactions.SumByGoalAsync(userId, cancellationToken);

        return list.Select(g => ToDto(g, user, contributions)).ToList();
    }

    public async Task<GoalDto> GetAsync(Guid userId, Guid goalId, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        var goal = await GetGoalAsync(userId, goalId, cancellationToken);
        return ToDto(goal, user, await transactions.SumByGoalAsync(userId, cancellationToken));
    }

    // UC-40
    public async Task<GoalDto> CreateAsync(Guid userId, CreateGoalCommand command, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);

        var goal = Goal.Create(
            userId,
            GoalName.From(command.Name),
            Money.Of(command.TargetAmount, user.Currency),
            Money.Of(command.InitialAmount, user.Currency),
            command.TargetDate,
            clock.Today,
            clock.Now);

        goal.RegisterContributions(Money.Zero(user.Currency), clock.Now);
        goals.Add(goal);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(goal, user, new Dictionary<Guid, decimal>());
    }

    // UC-41
    public async Task<GoalDto> UpdateAsync(Guid userId, Guid goalId, UpdateGoalCommand command, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        var goal = await GetGoalAsync(userId, goalId, cancellationToken);

        goal.Update(GoalName.From(command.Name), Money.Of(command.TargetAmount, user.Currency), command.TargetDate, clock.Today);

        var contributions = await transactions.SumByGoalAsync(userId, cancellationToken);
        goal.RegisterContributions(ContributionsOf(goal, user, contributions), clock.Now);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(goal, user, contributions);
    }

    // UC-44
    public async Task ArchiveAsync(Guid userId, Guid goalId, CancellationToken cancellationToken)
    {
        var goal = await GetGoalAsync(userId, goalId, cancellationToken);
        goal.Archive();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // UC-42, D4: a contribution is a Savings transaction linked to the goal (single source of truth).
    public async Task<TransactionDto> ContributeAsync(Guid userId, Guid goalId, ContributeCommand command, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        var goal = await GetGoalAsync(userId, goalId, cancellationToken);
        goal.EnsureNotArchived();

        var savings = await categories.GetSystemAsync(userId, CategoryKind.Savings, cancellationToken);

        var transaction = Transaction.Record(
            userId,
            Money.Of(command.Amount, user.Currency),
            savings.Id,
            command.Date ?? clock.Today,
            merchant: null,
            goal.Id,
            clock.Today,
            clock.Now);

        transactions.Add(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TransactionService.ToDto(transaction);
    }

    private async Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User");

    private async Task<Goal> GetGoalAsync(Guid userId, Guid goalId, CancellationToken cancellationToken) =>
        await goals.GetAsync(userId, goalId, cancellationToken) ?? throw new NotFoundException("Goal");

    private static Money ContributionsOf(Goal goal, User user, IReadOnlyDictionary<Guid, decimal> contributions) =>
        Money.Of(contributions.GetValueOrDefault(goal.Id), user.Currency);

    internal GoalDto ToDto(Goal goal, User user, IReadOnlyDictionary<Guid, decimal> contributions)
    {
        var progress = goal.ProgressAt(ContributionsOf(goal, user, contributions), clock.Today, user.PeriodStartDay);

        return new GoalDto(
            goal.Id,
            goal.Name.Value,
            MoneyDto.From(goal.TargetAmount),
            MoneyDto.From(goal.InitialAmount),
            goal.TargetDate,
            MoneyDto.From(progress.Current),
            MoneyDto.From(progress.Remaining),
            MoneyDto.From(progress.RequiredPerPeriod),
            progress.RemainingPeriods,
            progress.PercentComplete,
            progress.Status);
    }
}
