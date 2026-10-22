using Mizan.Domain.Common;

namespace Mizan.Domain.Budgets;

public sealed record BudgetThresholdReached(
    Guid UserId,
    Guid BudgetId,
    Guid CategoryId,
    AlertThreshold Threshold) : IDomainEvent;
