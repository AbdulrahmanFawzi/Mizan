using Mizan.Domain.Common;

namespace Mizan.Domain.Categories;

public sealed record CategoryDeleted(Guid UserId, Guid CategoryId) : IDomainEvent;
