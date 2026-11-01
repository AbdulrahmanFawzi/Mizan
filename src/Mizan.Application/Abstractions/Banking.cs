using Mizan.Domain.Banking;
using Mizan.Domain.Categorization;
using Mizan.Domain.Importing;
using Mizan.Domain.Transactions;

namespace Mizan.Application.Abstractions;

// The only door to the bank data provider (Lean today). Everything here is in our own shape,
// so replacing Lean with another provider means writing one new implementation, nothing else.
public interface IBankingProvider
{
    string Name { get; }

    Task<string> CreateCustomerAsync(Guid userId, CancellationToken cancellationToken);

    // A short-lived token the app passes to the provider's link screen (BR-C12).
    Task<LinkSession> CreateLinkSessionAsync(string providerCustomerId, CancellationToken cancellationToken);

    // Polling fallback when webhooks can't reach us (local development).
    Task<IReadOnlyList<ProviderEntity>> ListEntitiesAsync(string providerCustomerId, CancellationToken cancellationToken);

    Task<ProviderEntity?> GetEntityAsync(string providerCustomerId, string entityId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BankAccountData>> GetAccountsAsync(string entityId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BankTransactionData>> GetTransactionsAsync(
        string entityId, string accountId, SyncWindow window, CancellationToken cancellationToken);

    // Throws ConsentExpiredException when the bank no longer allows access.
    Task RequestRefreshAsync(string entityId, CancellationToken cancellationToken);

    Task RevokeConsentAsync(string providerCustomerId, string entityId, CancellationToken cancellationToken);

    // Removes everything the provider holds for this customer (BR-C10).
    Task DeleteCustomerAsync(string providerCustomerId, CancellationToken cancellationToken);
}

public sealed record LinkSession(
    string AppToken,
    string CustomerId,
    string AccessToken,
    bool Sandbox,
    IReadOnlyList<string> Permissions,
    DateTimeOffset AccessTo,
    string SuccessRedirectUrl,
    string FailRedirectUrl);

public sealed record ProviderEntity(
    string EntityId,
    string BankIdentifier,
    string BankName,
    DateTimeOffset ConsentGrantedAt,
    DateTimeOffset ConsentExpiresAt);

public sealed record BankAccountData(
    string ProviderAccountId,
    AccountKind Kind,
    string Currency,
    string? Iban,
    string Nickname,
    decimal? Balance,
    DateTimeOffset? BalanceAsOf);

public sealed record BankTransactionData(
    string ProviderTransactionId,
    decimal Amount,
    string Currency,
    TransactionDirection Direction,
    BankStatus Status,
    DateOnly Date,
    string? MerchantName,
    string? ProviderCategoryCode,
    decimal? ProviderCategoryConfidence,
    BankSubType SubType,
    string? CounterpartyIban,
    decimal? Fee,
    bool IsCashWithdrawal);

public sealed class ConsentExpiredException() : Exception("The bank consent has expired or was revoked.");

public sealed class BankingProviderException(string message, Exception? inner = null) : Exception(message, inner);

public interface IBankingCustomerRepository
{
    Task<BankingCustomer?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<BankingCustomer?> GetByProviderIdAsync(string providerCustomerId, CancellationToken cancellationToken);
    void Add(BankingCustomer customer);
}

public interface IBankConnectionRepository
{
    Task<BankConnection?> GetAsync(Guid userId, Guid connectionId, CancellationToken cancellationToken);
    Task<BankConnection?> GetByIdAsync(Guid connectionId, CancellationToken cancellationToken);
    Task<BankConnection?> GetByEntityIdAsync(string entityId, CancellationToken cancellationToken);
    Task<BankConnection?> GetLatestForBankAsync(Guid userId, string bankIdentifier, CancellationToken cancellationToken);
    Task<IReadOnlyList<BankConnection>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> ListActiveIdsAsync(CancellationToken cancellationToken);
    void Add(BankConnection connection);
}

public interface IMerchantRuleRepository
{
    Task<IReadOnlyDictionary<MerchantKey, Guid>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<MerchantRule?> GetAsync(Guid userId, MerchantKey merchant, CancellationToken cancellationToken);
    void Add(MerchantRule rule);
}

public interface IRejectedMatchRepository
{
    Task<IReadOnlySet<RejectedMatch>> ListAsync(Guid userId, CancellationToken cancellationToken);
    void Add(RejectedMatch match);
}

// Bank-specific queries on transactions.
public interface IBankTransactionRepository
{
    Task<IReadOnlyDictionary<string, Transaction>> GetByProviderIdsAsync(
        Guid connectionId, IReadOnlyCollection<string> providerTransactionIds, CancellationToken cancellationToken);

    // Pending bank transactions of an account inside a date range (BR-X9).
    Task<IReadOnlyList<Transaction>> ListPendingAsync(Guid accountId, SyncWindow window, CancellationToken cancellationToken);

    // Incoming bank money into the user's accounts in a date range (transfer pairing, BR-R1).
    Task<IReadOnlyList<Transaction>> ListCreditsAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    // Manual expenses that may duplicate a bank transaction (BR-D1).
    Task<IReadOnlyList<Transaction>> ListUnmatchedManualAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    // Bank spending a new manual expense may duplicate (BR-D1, the other way round).
    Task<IReadOnlyList<Transaction>> ListUnmatchedBankSpendingAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> ListNeedingReviewAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> ListByConnectionAsync(Guid connectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken);

    // Bank transactions of one merchant in a date range whose category the user didn't pick (BR-K2).
    Task<IReadOnlyList<Transaction>> ListAutoCategorizedAsync(
        Guid userId, MerchantKey merchant, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}

// Work for an outside system, saved in the same database transaction and done later by a worker.
public interface IOutbox
{
    void Enqueue(string type, object payload);
}

public static class OutboxTypes
{
    public const string DeleteCustomer = "banking.delete-customer";
    public const string SendEmail = "email.send";       // payload: EmailMessage
}

public sealed record DeleteCustomerMessage(string ProviderCustomerId);

// Only one sync per bank connection at a time (scheduler, app open and webhooks can overlap).
public interface ISyncLock
{
    // Null when someone else holds the lock.
    Task<IAsyncDisposable?> TryAcquireAsync(Guid connectionId, CancellationToken cancellationToken);
}
