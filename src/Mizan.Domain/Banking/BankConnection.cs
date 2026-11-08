using Mizan.Domain.Common;

namespace Mizan.Domain.Banking;

// One link between a user and one bank (a Lean "entity"), with the accounts inside it.
public sealed class BankConnection : AggregateRoot
{
    public const int BankNameMaxLength = 100;

    // BR-S2: at most one refresh request per connection in this time.
    public static readonly TimeSpan MinRefreshInterval = TimeSpan.FromHours(4);

    // After a refresh request the provider rebuilds its data and its data calls time out (seen in the sandbox).
    // Until it says "data ready", or this much time passes, we don't read from it.
    public static readonly TimeSpan RefreshGracePeriod = TimeSpan.FromMinutes(15);

    private readonly List<BankAccount> _accounts = [];
    private List<int> _remindersSent = [];

    public Guid UserId { get; private set; }
    public string ProviderEntityId { get; private set; } = null!;
    public string BankIdentifier { get; private set; } = null!;
    public string BankName { get; private set; } = null!;
    public ConnectionStatus Status { get; private set; }
    public Consent Consent { get; private set; } = null!;
    public DateTimeOffset? LastSyncedAt { get; private set; }
    public DateTimeOffset? LastRefreshRequestedAt { get; private set; }
    public bool HistoryImported { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<BankAccount> Accounts => _accounts.AsReadOnly();
    public IReadOnlyList<int> RemindersSent => _remindersSent.AsReadOnly();

    private BankConnection()
    {
    }

    // UC-71: Lean told us the user finished linking.
    public static BankConnection Connect(
        Guid userId,
        string providerEntityId,
        string bankIdentifier,
        string bankName,
        Consent consent,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEntityId);
        ArgumentNullException.ThrowIfNull(consent);

        var connection = new BankConnection
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            ProviderEntityId = providerEntityId,
            BankIdentifier = Text.Required(bankIdentifier, BankNameMaxLength, "Bank identifier"),
            BankName = Text.Truncate(bankName, BankNameMaxLength) ?? bankIdentifier,
            Status = ConnectionStatus.Active,
            Consent = consent,
            CreatedAt = now,
        };

        connection.Raise(new BankConnectionActivated(connection.Id, userId));
        return connection;
    }

    public bool IsActive => Status == ConnectionStatus.Active;

    // BR-S2
    public bool CanRequestRefresh(DateTimeOffset now) =>
        IsActive && (LastRefreshRequestedAt is null || now - LastRefreshRequestedAt >= MinRefreshInterval);

    // True while the provider is still collecting the data we asked for.
    public bool IsRefreshing(DateTimeOffset now) =>
        LastRefreshRequestedAt is { } requested
        && now - requested < RefreshGracePeriod
        && (LastSyncedAt is null || LastSyncedAt < requested);

    public void MarkRefreshRequested(DateTimeOffset now)
    {
        if (!CanRequestRefresh(now))
        {
            throw new DomainException("This bank was refreshed recently.");
        }

        LastRefreshRequestedAt = now;
    }

    // BR-H1 and BR-S6. A reconnect continues from the last sync (BR-C7).
    public SyncWindow NextSyncWindow(DateOnly today) =>
        LastSyncedAt is { } last && HistoryImported
            ? SyncWindow.After(DateOnly.FromDateTime(last.UtcDateTime), today)
            : SyncWindow.ForFirstLink(today);

    // Adds new accounts and refreshes known ones. Returns the accounts in the order given.
    public IReadOnlyList<BankAccount> SyncAccounts(IEnumerable<BankAccountSnapshot> snapshots, Currency budgetCurrency)
    {
        EnsureActive();
        var result = new List<BankAccount>();

        foreach (var snapshot in snapshots)
        {
            var account = _accounts.SingleOrDefault(a => a.ProviderAccountId == snapshot.ProviderAccountId);
            if (account is null)
            {
                account = BankAccount.Create(Id, snapshot, budgetCurrency);
                _accounts.Add(account);
            }
            else
            {
                account.Refresh(snapshot);
            }

            result.Add(account);
        }

        return result;
    }

    // BR-X10: the user decides which accounts count.
    public void SetAccountIncluded(Guid accountId, bool include, Currency budgetCurrency)
    {
        var account = _accounts.SingleOrDefault(a => a.Id == accountId)
            ?? throw new DomainException("Account not found in this connection.");

        account.SetIncludeInBudget(include, budgetCurrency);
    }

    public void RememberTransfersIn(Guid accountId, bool areSpending)
    {
        var account = _accounts.SingleOrDefault(a => a.Id == accountId)
            ?? throw new DomainException("Account not found in this connection.");

        account.RememberTransfersIn(areSpending);
    }

    // `complete` is false when some accounts failed or the first import came back empty:
    // the window then doesn't move forward, so the next sync fetches the same dates again.
    public void RecordSync(DateTimeOffset now, IReadOnlyList<AffectedSpending> affected, int newReviewItems, bool complete = true)
    {
        EnsureActive();
        var isInitial = !HistoryImported;

        if (complete)
        {
            LastSyncedAt = now;
            HistoryImported = true;
        }

        Raise(new BankTransactionsImported(Id, UserId, isInitial, affected, newReviewItems));
    }

    // BR-C5 and BR-C6, run by the daily check.
    public void CheckConsent(DateTimeOffset now, DateOnly today)
    {
        if (!IsActive)
        {
            return;
        }

        if (Consent.IsExpired(now))
        {
            Expire();
            return;
        }

        if (Consent.DueReminder(today, _remindersSent) is { } day)
        {
            // Mark every reminder this one replaces, so a late check never sends an older one afterwards.
            _remindersSent = [.. _remindersSent.Union(Consent.ReminderDays.Where(d => d >= day))];
            Raise(new ConsentExpiringSoon(Id, UserId, BankName, Consent.DaysUntilExpiry(today), Consent.ExpiresOn));
        }
    }

    // UC-75: consent ended at the bank, or a refresh answered "consent expired". Notified once (BR-C6).
    public void Expire()
    {
        if (!IsActive)
        {
            return;
        }

        Status = ConnectionStatus.Expired;
        Raise(new BankConnectionExpired(Id, UserId, BankName));
    }

    // UC-73: the same connection continues with a new consent (BR-C7).
    public void Reconnect(string providerEntityId, Consent consent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEntityId);
        ArgumentNullException.ThrowIfNull(consent);

        if (Status == ConnectionStatus.Revoked)
        {
            throw new DomainException("A removed bank can't be reconnected. Link it again instead.");
        }

        ProviderEntityId = providerEntityId;
        Consent = consent;
        Status = ConnectionStatus.Active;
        _remindersSent = [];
    }

    // UC-74 (BR-C8, BR-C9): called after the provider confirmed the consent is revoked.
    public void Revoke(bool keepTransactions)
    {
        if (Status == ConnectionStatus.Revoked)
        {
            return;
        }

        Status = ConnectionStatus.Revoked;
        Raise(new BankConnectionRevoked(Id, UserId, keepTransactions));
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainException("This bank connection is not active.");
        }
    }
}
