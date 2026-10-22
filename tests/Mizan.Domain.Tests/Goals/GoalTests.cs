using Mizan.Domain.Budgets;
using Mizan.Domain.Common;
using Mizan.Domain.Goals;
using static Mizan.Domain.Tests.TestData;

namespace Mizan.Domain.Tests.Goals;

public class GoalTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly PeriodStartDay FirstOfMonth = PeriodStartDay.From(1);

    private static Goal Travel(decimal target = 10_000, decimal initial = 3_500, string targetDate = "2027-12-31", string today = "2026-10-30") =>
        Goal.Create(UserId, GoalName.From("Travel"), Sar(target), Sar(initial), D(targetDate), D(today), Now);

    [Fact]
    public void Create_TargetDateNotInFuture_Throws()
    {
        Assert.Throws<DomainException>(() => Travel(targetDate: "2026-10-30"));
    }

    [Fact]
    public void Create_NonPositiveTarget_Throws()
    {
        Assert.Throws<DomainException>(() => Travel(target: 0));
    }

    [Fact]
    public void Create_NegativeInitialAmount_Throws()
    {
        Assert.Throws<DomainException>(() => Travel(initial: -1));
    }

    [Fact]
    public void Progress_PrdExample_RequiredMonthlySavingRoundsUp()
    {
        // 6,500 remaining over 12 periods (Jan -> Dec 2027) = 541.67 -> 542 (BR-G4, BR-G5)
        var goal = Travel(today: "2026-12-15");

        var progress = goal.ProgressAt(Sar(0), D("2027-01-10"), FirstOfMonth);

        Assert.Equal(Sar(3_500), progress.Current);
        Assert.Equal(Sar(6_500), progress.Remaining);
        Assert.Equal(12, progress.RemainingPeriods);
        Assert.Equal(Sar(542), progress.RequiredPerPeriod);
        Assert.Equal(35m, progress.PercentComplete);
        Assert.Equal(GoalStatus.Active, progress.Status);
    }

    [Fact]
    public void Progress_ContributionsAddToInitialAmount()
    {
        var progress = Travel().ProgressAt(Sar(1_500), Today, FirstOfMonth);

        Assert.Equal(Sar(5_000), progress.Current);
        Assert.Equal(50m, progress.PercentComplete);
    }

    [Fact]
    public void Progress_TargetReached_IsCompleted()
    {
        var progress = Travel().ProgressAt(Sar(7_000), Today, FirstOfMonth);

        Assert.Equal(GoalStatus.Completed, progress.Status);
        Assert.Equal(Sar(0), progress.Remaining);
        Assert.Equal(Sar(0), progress.RequiredPerPeriod);
        Assert.Equal(100m, progress.PercentComplete);
    }

    [Fact]
    public void Progress_PastTargetDate_IsOverdue()
    {
        var progress = Travel(targetDate: "2026-11-30").ProgressAt(Sar(0), D("2026-12-05"), FirstOfMonth);

        Assert.Equal(GoalStatus.Overdue, progress.Status);
        Assert.Equal(0, progress.RemainingPeriods);
    }

    [Fact]
    public void Progress_TargetInCurrentPeriod_NeedsEverythingNow()
    {
        var progress = Travel(targetDate: "2026-10-31").ProgressAt(Sar(0), Today, FirstOfMonth);

        Assert.Equal(1, progress.RemainingPeriods);
        Assert.Equal(Sar(6_500), progress.RequiredPerPeriod);
    }

    [Fact]
    public void Update_ChangingTargetDate_RecalculatesRequiredSaving()
    {
        var goal = Travel(today: "2026-12-15");

        goal.Update(GoalName.From("Travel"), Sar(10_000), D("2027-06-30"), D("2027-01-10"));
        var progress = goal.ProgressAt(Sar(0), D("2027-01-10"), FirstOfMonth);

        Assert.Equal(6, progress.RemainingPeriods);
        Assert.Equal(Sar(1_084), progress.RequiredPerPeriod); // 6,500 / 6 = 1,083.33
    }

    [Fact]
    public void RegisterContributions_AnnouncesCompletionOnce()
    {
        var goal = Travel();

        goal.RegisterContributions(Sar(6_000), Now);
        Assert.Empty(goal.DomainEvents);

        goal.RegisterContributions(Sar(6_500), Now);
        goal.RegisterContributions(Sar(7_000), Now);

        Assert.Equal(new GoalCompleted(UserId, goal.Id), Assert.Single(goal.DomainEvents));
    }

    [Fact]
    public void ArchivedGoal_CannotBeUpdated()
    {
        var goal = Travel();
        goal.Archive();

        Assert.Throws<DomainException>(() =>
            goal.Update(GoalName.From("Travel"), Sar(10_000), D("2027-12-31"), Today));
        Assert.Equal(GoalStatus.Archived, goal.ProgressAt(Sar(0), Today, FirstOfMonth).Status);
    }
}
