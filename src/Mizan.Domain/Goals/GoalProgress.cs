using Mizan.Domain.Common;

namespace Mizan.Domain.Goals;

public sealed record GoalProgress(
    Money Current,
    Money Remaining,
    Money RequiredPerPeriod,
    int RemainingPeriods,
    decimal PercentComplete,
    GoalStatus Status);
