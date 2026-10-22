using Mizan.Domain.Budgets;
using Mizan.Domain.Common;

namespace Mizan.Domain.Goals;

public sealed class Goal : AggregateRoot
{
    public Guid UserId { get; private set; }
    public GoalName Name { get; private set; } = null!;
    public Money TargetAmount { get; private set; } = null!;
    public Money InitialAmount { get; private set; } = null!;
    public DateOnly TargetDate { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private Goal()
    {
    }

    public static Goal Create(
        Guid userId,
        GoalName name,
        Money targetAmount,
        Money initialAmount,
        DateOnly targetDate,
        DateOnly today,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(initialAmount);

        // BR-G1
        if (initialAmount.IsNegative)
        {
            throw new DomainException("Initial amount cannot be negative.");
        }

        var goal = new Goal
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            InitialAmount = initialAmount,
            CreatedAt = now,
        };

        goal.Apply(name, targetAmount, targetDate, today);
        return goal;
    }

    public void Update(GoalName name, Money targetAmount, DateOnly targetDate, DateOnly today)
    {
        EnsureNotArchived();
        Apply(name, targetAmount, targetDate, today);
    }

    public void Archive() => IsArchived = true;

    public void EnsureNotArchived()
    {
        if (IsArchived)
        {
            throw new DomainException("This goal is archived.");
        }
    }

    public Money CurrentAmount(Money contributions) => InitialAmount + contributions;

    // BR-G8: announce completion once.
    public void RegisterContributions(Money contributions, DateTimeOffset now)
    {
        if (CompletedAt is null && CurrentAmount(contributions) >= TargetAmount)
        {
            CompletedAt = now;
            Raise(new GoalCompleted(UserId, Id));
        }
    }

    public GoalProgress ProgressAt(Money contributions, DateOnly today, PeriodStartDay periodStartDay)
    {
        ArgumentNullException.ThrowIfNull(periodStartDay);

        var current = CurrentAmount(contributions);
        var zero = Money.Zero(TargetAmount.Currency);
        var remaining = current >= TargetAmount ? zero : TargetAmount - current;
        var periods = RemainingPeriods(today, periodStartDay);

        // BR-G4, BR-G5
        var requiredPerPeriod = remaining.IsZero || periods == 0
            ? zero
            : remaining.DivideRoundingUpToWhole(periods);

        var percent = Math.Min(100m, decimal.Round(current.Amount / TargetAmount.Amount * 100m, 2, MidpointRounding.AwayFromZero));

        var status = IsArchived ? GoalStatus.Archived
            : remaining.IsZero ? GoalStatus.Completed
            : today > TargetDate ? GoalStatus.Overdue // BR-G9
            : GoalStatus.Active;

        return new GoalProgress(current, remaining, requiredPerPeriod, periods, percent, status);
    }

    // Counts budget periods from the current one up to the one containing the target date.
    private int RemainingPeriods(DateOnly today, PeriodStartDay periodStartDay)
    {
        if (today > TargetDate)
        {
            return 0;
        }

        var period = BudgetPeriod.Containing(today, periodStartDay);
        var count = 1;

        while (!period.Contains(TargetDate))
        {
            period = period.Next(periodStartDay);
            count++;
        }

        return count;
    }

    private void Apply(GoalName name, Money targetAmount, DateOnly targetDate, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(targetAmount);

        // BR-G1
        if (!targetAmount.IsPositive)
        {
            throw new DomainException("Target amount must be greater than zero.");
        }

        if (targetAmount.Currency != InitialAmount.Currency)
        {
            throw new DomainException("Target and initial amounts must use the same currency.");
        }

        // BR-G2, and BR-G9 when extending an overdue goal.
        if (targetDate <= today)
        {
            throw new DomainException("Target date must be in the future.");
        }

        Name = name;
        TargetAmount = targetAmount;
        TargetDate = targetDate;
    }
}
