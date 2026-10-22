using Mizan.Domain.Common;

namespace Mizan.Domain.Transactions;

public sealed record TransactionRecorded(
    Guid TransactionId,
    Guid UserId,
    Guid CategoryId,
    DateOnly Date,
    Guid? GoalId) : IDomainEvent;

public sealed record TransactionUpdated(
    Guid TransactionId,
    Guid UserId,
    Guid CategoryId,
    DateOnly Date,
    Guid? GoalId) : IDomainEvent;

public sealed record TransactionDeleted(
    Guid TransactionId,
    Guid UserId,
    Guid CategoryId,
    DateOnly Date,
    Guid? GoalId) : IDomainEvent;
