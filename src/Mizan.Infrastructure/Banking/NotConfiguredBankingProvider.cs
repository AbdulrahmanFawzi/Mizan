using Mizan.Application.Abstractions;
using Mizan.Domain.Banking;

namespace Mizan.Infrastructure.Banking;

// Used when no provider keys are set, so the rest of the app still runs (and explains why linking fails).
internal sealed class NotConfiguredBankingProvider : IBankingProvider
{
    private static Exception Fail() => new BankingProviderException("Bank linking isn't configured on this server (Lean:AppToken / Lean:ClientSecret).");

    public string Name => "none";

    public Task<string> CreateCustomerAsync(Guid userId, CancellationToken cancellationToken) => throw Fail();
    public Task<LinkSession> CreateLinkSessionAsync(string providerCustomerId, CancellationToken cancellationToken) => throw Fail();
    public Task<IReadOnlyList<ProviderEntity>> ListEntitiesAsync(string providerCustomerId, CancellationToken cancellationToken) => throw Fail();
    public Task<ProviderEntity?> GetEntityAsync(string providerCustomerId, string entityId, CancellationToken cancellationToken) => throw Fail();
    public Task<IReadOnlyList<BankAccountData>> GetAccountsAsync(string entityId, CancellationToken cancellationToken) => throw Fail();
    public Task<IReadOnlyList<BankTransactionData>> GetTransactionsAsync(string entityId, string accountId, SyncWindow window, CancellationToken cancellationToken) => throw Fail();
    public Task RequestRefreshAsync(string entityId, CancellationToken cancellationToken) => throw Fail();
    public Task RevokeConsentAsync(string providerCustomerId, string entityId, CancellationToken cancellationToken) => throw Fail();
    public Task DeleteCustomerAsync(string providerCustomerId, CancellationToken cancellationToken) => throw Fail();
}
