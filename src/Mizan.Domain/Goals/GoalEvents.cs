using Mizan.Domain.Common;

namespace Mizan.Domain.Goals;

public sealed record GoalCompleted(Guid UserId, Guid GoalId) : IDomainEvent;
