using Mizan.Domain.Common;

namespace Mizan.Domain.Categorization;

// "Always put this merchant in this category" (UC-91, BR-K2).
public sealed class MerchantRule : AggregateRoot
{
    public Guid UserId { get; private set; }
    public MerchantKey Merchant { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private MerchantRule()
    {
    }

    public static MerchantRule Create(Guid userId, MerchantKey merchant, Guid categoryId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(merchant);

        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category is required.");
        }

        return new MerchantRule
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Merchant = merchant,
            CategoryId = categoryId,
            CreatedAt = now,
        };
    }

    public void ChangeCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category is required.");
        }

        CategoryId = categoryId;
    }
}
