using Mizan.Domain.Common;

namespace Mizan.Domain.Users;

public sealed record UserRegistered(Guid UserId) : IDomainEvent;

public sealed record MonthlyIncomeChanged(Guid UserId, Money MonthlyIncome) : IDomainEvent;
