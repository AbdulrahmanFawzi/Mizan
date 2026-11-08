using Microsoft.EntityFrameworkCore;
using Mizan.Application.Abstractions;
using Mizan.Domain.Banking;
using Mizan.Domain.Categorization;
using Mizan.Domain.Importing;
using Mizan.Domain.Transactions;

namespace Mizan.Infrastructure.Persistence.Repositories;

internal sealed class BankingCustomerRepository(MizanDbContext db) : IBankingCustomerRepository
{
    public Task<BankingCustomer?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        db.BankingCustomers.SingleOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public Task<BankingCustomer?> GetByProviderIdAsync(string providerCustomerId, CancellationToken cancellationToken) =>
        db.BankingCustomers.SingleOrDefaultAsync(c => c.ProviderCustomerId == providerCustomerId, cancellationToken);

    public void Add(BankingCustomer customer) => db.BankingCustomers.Add(customer);
}

internal sealed class BankConnectionRepository(MizanDbContext db) : IBankConnectionRepository
{
    public Task<BankConnection?> GetAsync(Guid userId, Guid connectionId, CancellationToken cancellationToken) =>
        db.BankConnections.SingleOrDefaultAsync(c => c.UserId == userId && c.Id == connectionId, cancellationToken);

    public Task<BankConnection?> GetByIdAsync(Guid connectionId, CancellationToken cancellationToken) =>
        db.BankConnections.SingleOrDefaultAsync(c => c.Id == connectionId, cancellationToken);

    public Task<BankConnection?> GetByEntityIdAsync(string entityId, CancellationToken cancellationToken) =>
        db.BankConnections.SingleOrDefaultAsync(c => c.ProviderEntityId == entityId, cancellationToken);

    public Task<BankConnection?> GetLatestForBankAsync(Guid userId, string bankIdentifier, CancellationToken cancellationToken) =>
        db.BankConnections
            .Where(c => c.UserId == userId && c.BankIdentifier == bankIdentifier && c.Status != ConnectionStatus.Revoked)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<BankConnection>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.BankConnections.Where(c => c.UserId == userId).OrderBy(c => c.CreatedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListActiveIdsAsync(CancellationToken cancellationToken) =>
        await db.BankConnections.Where(c => c.Status == ConnectionStatus.Active).Select(c => c.Id).ToListAsync(cancellationToken);

    public void Add(BankConnection connection) => db.BankConnections.Add(connection);
}

internal sealed class MerchantRuleRepository(MizanDbContext db) : IMerchantRuleRepository
{
    public async Task<IReadOnlyDictionary<MerchantKey, Guid>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        (await db.MerchantRules.AsNoTracking().Where(r => r.UserId == userId).ToListAsync(cancellationToken))
        .ToDictionary(r => r.Merchant, r => r.CategoryId);

    public Task<MerchantRule?> GetAsync(Guid userId, MerchantKey merchant, CancellationToken cancellationToken) =>
        db.MerchantRules.SingleOrDefaultAsync(r => r.UserId == userId && r.Merchant == merchant, cancellationToken);

    public void Add(MerchantRule rule) => db.MerchantRules.Add(rule);
}

internal sealed class RejectedMatchRepository(MizanDbContext db) : IRejectedMatchRepository
{
    public async Task<IReadOnlySet<RejectedMatch>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        (await db.RejectedMatches
            .Where(r => db.Transactions.Any(t => t.Id == r.BankTransactionId && t.UserId == userId))
            .ToListAsync(cancellationToken))
        .ToHashSet();

    public void Add(RejectedMatch match) => db.RejectedMatches.Add(match);
}

internal sealed class BankTransactionRepository(MizanDbContext db) : IBankTransactionRepository
{
    private const string ConnectionId = "_bankConnectionId";
    private const string AccountId = "_bankAccountId";
    private const string ProviderId = "_providerTransactionId";

    public async Task<IReadOnlyDictionary<string, Transaction>> GetByProviderIdsAsync(
        Guid connectionId, IReadOnlyCollection<string> providerTransactionIds, CancellationToken cancellationToken)
    {
        if (providerTransactionIds.Count == 0)
        {
            return new Dictionary<string, Transaction>();
        }

        var found = await db.Transactions
            .Where(t => EF.Property<Guid?>(t, ConnectionId) == connectionId
                && providerTransactionIds.Contains(EF.Property<string>(t, ProviderId)))
            .ToListAsync(cancellationToken);

        return found.ToDictionary(t => t.Bank!.ProviderTransactionId);
    }

    public async Task<IReadOnlyList<Transaction>> ListPendingAsync(Guid accountId, SyncWindow window, CancellationToken cancellationToken) =>
        await db.Transactions
            .Where(t => EF.Property<Guid?>(t, AccountId) == accountId
                && t.BankStatus == BankStatus.Pending
                && t.Date >= window.From && t.Date <= window.To)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Transaction>> ListCreditsAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        await db.Transactions
            .Where(t => t.UserId == userId
                && t.Source == TransactionSource.Bank
                && t.Direction == TransactionDirection.Credit
                && EF.Property<Guid?>(t, AccountId) != null
                && t.Date >= from && t.Date <= to)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Transaction>> ListUnmatchedManualAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        await db.Transactions
            .Where(t => t.UserId == userId
                && t.Source == TransactionSource.Manual
                && t.PossibleDuplicateOf == null
                && t.Date >= from && t.Date <= to)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Transaction>> ListUnmatchedBankSpendingAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        await db.Transactions
            .Where(t => t.UserId == userId
                && t.Source == TransactionSource.Bank
                && t.Direction == TransactionDirection.Debit
                && t.Treatment == Treatment.Spending
                && t.Review == null
                && !t.IsExcluded
                && t.Date >= from && t.Date <= to)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Transaction>> ListNeedingReviewAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.Transactions
            .Where(t => t.UserId == userId && t.Review != null)
            .OrderByDescending(t => t.Date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Transaction>> ListByConnectionAsync(Guid connectionId, CancellationToken cancellationToken) =>
        await db.Transactions.Where(t => EF.Property<Guid?>(t, ConnectionId) == connectionId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Transaction>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        await db.Transactions.Where(t => EF.Property<Guid?>(t, AccountId) == accountId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Transaction>> ListAutoCategorizedAsync(
        Guid userId, MerchantKey merchant, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        await db.Transactions
            .Where(t => t.UserId == userId
                && t.Source == TransactionSource.Bank
                && t.MerchantKey == merchant
                && t.CategorySource != CategorySource.User
                && t.Date >= from && t.Date <= to)
            .ToListAsync(cancellationToken);
}
