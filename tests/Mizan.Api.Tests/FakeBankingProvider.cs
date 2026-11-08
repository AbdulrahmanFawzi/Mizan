using System.Collections.Concurrent;
using Mizan.Application.Abstractions;
using Mizan.Domain.Banking;
using Mizan.Domain.Transactions;

namespace Mizan.Api.Tests;

// A bank the tests control. Each test works with its own customer, so tests don't see each other's data.
public sealed class FakeBankingProvider : IBankingProvider
{
    private readonly ConcurrentDictionary<string, Customer> _customers = new();

    public string Name => "fake";

    public ConcurrentBag<string> DeletedCustomers { get; } = [];

    public sealed class Customer
    {
        public List<ProviderEntity> Entities { get; } = [];
        public Dictionary<string, List<BankAccountData>> Accounts { get; } = [];
        public Dictionary<string, List<BankTransactionData>> Transactions { get; } = [];
        public HashSet<string> ExpiredEntities { get; } = [];
        public HashSet<string> RevokedEntities { get; } = [];
        public int RefreshRequests { get; set; }
    }

    public Customer For(string customerId) => _customers.GetOrAdd(customerId, _ => new Customer());

    // Simulates the user finishing the link screen.
    public string Link(string customerId, IEnumerable<(BankAccountData Account, BankTransactionData[] Transactions)> accounts, string bank = "MOCK1")
    {
        var customer = For(customerId);
        var entityId = $"entity-{Guid.NewGuid():N}";

        customer.Entities.Add(new ProviderEntity(entityId, bank, $"Mock {bank}", DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMonths(12)));
        customer.Accounts[entityId] = [];
        foreach (var (account, transactions) in accounts)
        {
            customer.Accounts[entityId].Add(account);
            customer.Transactions[account.ProviderAccountId] = [.. transactions];
        }

        return entityId;
    }

    private Customer Owner(string entityId) =>
        _customers.Values.Single(c => c.Entities.Any(e => e.EntityId == entityId));

    public Task<string> CreateCustomerAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult($"cust-{userId:N}");

    public Task<LinkSession> CreateLinkSessionAsync(string providerCustomerId, CancellationToken cancellationToken) =>
        Task.FromResult(new LinkSession("app-token", providerCustomerId, "customer-token", true,
            ["accounts", "balance", "transactions"], DateTimeOffset.UtcNow.AddMonths(12), "https://ok", "https://fail"));

    public Task<IReadOnlyList<ProviderEntity>> ListEntitiesAsync(string providerCustomerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProviderEntity>>(For(providerCustomerId).Entities.ToList());

    public Task<ProviderEntity?> GetEntityAsync(string providerCustomerId, string entityId, CancellationToken cancellationToken) =>
        Task.FromResult(For(providerCustomerId).Entities.SingleOrDefault(e => e.EntityId == entityId));

    public Task<IReadOnlyList<BankAccountData>> GetAccountsAsync(string entityId, CancellationToken cancellationToken)
    {
        var customer = Owner(entityId);
        if (customer.ExpiredEntities.Contains(entityId))
        {
            throw new ConsentExpiredException();
        }

        return Task.FromResult<IReadOnlyList<BankAccountData>>(customer.Accounts[entityId].ToList());
    }

    public Task<IReadOnlyList<BankTransactionData>> GetTransactionsAsync(
        string entityId, string accountId, SyncWindow window, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<BankTransactionData>>(
            Owner(entityId).Transactions[accountId].Where(t => window.Contains(t.Date)).ToList());

    public Task RequestRefreshAsync(string entityId, CancellationToken cancellationToken)
    {
        var customer = Owner(entityId);
        if (customer.ExpiredEntities.Contains(entityId))
        {
            throw new ConsentExpiredException();
        }

        customer.RefreshRequests++;
        return Task.CompletedTask;
    }

    public Task RevokeConsentAsync(string providerCustomerId, string entityId, CancellationToken cancellationToken)
    {
        For(providerCustomerId).RevokedEntities.Add(entityId);
        return Task.CompletedTask;
    }

    public Task DeleteCustomerAsync(string providerCustomerId, CancellationToken cancellationToken)
    {
        DeletedCustomers.Add(providerCustomerId);
        return Task.CompletedTask;
    }

    // Helpers to build bank data in tests.
    public static BankAccountData Account(string id, AccountKind kind = AccountKind.Current, string? iban = null, string currency = "SAR") =>
        new(id, kind, currency, iban, $"{kind} account", 5_000, DateTimeOffset.UtcNow);

    public static BankTransactionData Debit(
        string id, decimal amount, DateOnly date, string? merchant = "Shop", string? category = null, decimal confidence = 0.9m,
        string? counterpartyIban = null, decimal? fee = null, bool atm = false, BankStatus status = BankStatus.Booked, string currency = "SAR") =>
        new(id, amount, currency, TransactionDirection.Debit, status, date, merchant, category, category is null ? null : confidence,
            BankSubType.None, counterpartyIban, fee, atm);

    public static BankTransactionData Credit(
        string id, decimal amount, DateOnly date, string? merchant = null, string? category = null, BankSubType subType = BankSubType.None,
        string? counterpartyIban = null) =>
        new(id, amount, "SAR", TransactionDirection.Credit, BankStatus.Booked, date, merchant, category, category is null ? null : 0.9m,
            subType, counterpartyIban, null, false);
}
