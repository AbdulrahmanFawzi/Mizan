namespace Mizan.Domain.Banking;

// The user's customer record at the bank data provider. Kept apart from User so User knows nothing about Lean.
public sealed class BankingCustomer
{
    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = null!;
    public string ProviderCustomerId { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private BankingCustomer()
    {
    }

    public static BankingCustomer Create(Guid userId, string provider, string providerCustomerId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerCustomerId);

        return new BankingCustomer
        {
            UserId = userId,
            Provider = provider,
            ProviderCustomerId = providerCustomerId,
            CreatedAt = now,
        };
    }
}
