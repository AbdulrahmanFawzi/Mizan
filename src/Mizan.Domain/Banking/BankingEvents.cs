using Mizan.Domain.Common;

namespace Mizan.Domain.Banking;

public sealed record BankConnectionActivated(Guid ConnectionId, Guid UserId) : IDomainEvent;

public sealed record ConsentExpiringSoon(Guid ConnectionId, Guid UserId, string BankName, int DaysLeft, DateOnly ExpiresOn)
    : IDomainEvent;

public sealed record BankConnectionExpired(Guid ConnectionId, Guid UserId, string BankName) : IDomainEvent;

public sealed record BankConnectionRevoked(Guid ConnectionId, Guid UserId, bool KeepTransactions) : IDomainEvent;

// One spending change for alerts: which category, in which budget period (by date).
public sealed record AffectedSpending(Guid CategoryId, DateOnly Date);

// Raised once per import, not per transaction, so a 3-month first import doesn't flood alerts (BR-H3).
public sealed record BankTransactionsImported(
    Guid ConnectionId,
    Guid UserId,
    bool IsInitialImport,
    IReadOnlyList<AffectedSpending> Affected,
    int NewReviewItems) : IDomainEvent;
