using Mizan.Domain.Banking;
using Mizan.Domain.Common;
using static Mizan.Domain.Tests.TestData;

namespace Mizan.Domain.Tests.Banking;

public class BankConnectionTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static BankConnection Connect(Consent? consent = null)
    {
        var connection = BankConnection.Connect(UserId, "entity-1", "LEA1_SAMAOB_SAU", "Mockbank 1",
            consent ?? Consent.ForDefaultPeriod(Now), Now);
        connection.ClearDomainEvents();
        return connection;
    }

    private static BankAccountSnapshot Snapshot(string id, AccountKind kind = AccountKind.Current, string currency = "SAR") =>
        new(id, kind, Currency.From(currency), null, $"{kind} account", Sar(100), Now);

    [Fact]
    public void Connect_IsActive_AndRaisesActivated()
    {
        var connection = BankConnection.Connect(UserId, "entity-1", "LEA1", "Mockbank 1", Consent.ForDefaultPeriod(Now), Now);

        Assert.Equal(ConnectionStatus.Active, connection.Status);
        Assert.Equal(new BankConnectionActivated(connection.Id, UserId), Assert.Single(connection.DomainEvents));
    }

    [Fact]
    public void Refresh_AtMostOnceEveryFourHours()
    {
        var connection = Connect();

        connection.MarkRefreshRequested(Now);

        Assert.False(connection.CanRequestRefresh(Now.AddHours(3)));
        Assert.Throws<DomainException>(() => connection.MarkRefreshRequested(Now.AddHours(3)));
        Assert.True(connection.CanRequestRefresh(Now.AddHours(4)));
    }

    [Fact]
    public void IsRefreshing_UntilTheNextSync_OrFifteenMinutes()
    {
        var connection = Connect();
        connection.MarkRefreshRequested(Now);

        Assert.True(connection.IsRefreshing(Now.AddMinutes(10)));
        Assert.False(connection.IsRefreshing(Now.AddMinutes(15)));

        connection.RecordSync(Now.AddMinutes(5), [], 0);
        Assert.False(connection.IsRefreshing(Now.AddMinutes(6)));
    }

    [Fact]
    public void FirstSync_ImportsThreeMonths_ThenContinuesFromLastSync()
    {
        var connection = Connect();
        Assert.Equal(SyncWindow.ForFirstLink(Today), connection.NextSyncWindow(Today));

        connection.RecordSync(Now, [], 0);

        Assert.Equal(SyncWindow.After(Today, Today.AddDays(2)), connection.NextSyncWindow(Today.AddDays(2)));
    }

    [Fact]
    public void RecordSync_RaisesOneImportEvent_MarkedInitialOnlyTheFirstTime()
    {
        var connection = Connect();
        var affected = new[] { new AffectedSpending(Guid.NewGuid(), Today) };

        connection.RecordSync(Now, affected, 2);
        connection.RecordSync(Now.AddHours(5), [], 0);

        var events = connection.DomainEvents.OfType<BankTransactionsImported>().ToList();
        Assert.True(events[0].IsInitialImport);
        Assert.Equal(2, events[0].NewReviewItems);
        Assert.False(events[1].IsInitialImport);
    }

    [Fact]
    public void SyncAccounts_SavingsAndOtherCurrencies_DontCountByDefault()
    {
        var connection = Connect();

        var accounts = connection.SyncAccounts(
            [Snapshot("a1"), Snapshot("a2", AccountKind.Savings), Snapshot("a3", AccountKind.CreditCard), Snapshot("a4", currency: "USD")],
            Currency.SAR);

        Assert.Equal([true, false, true, false], accounts.Select(a => a.IncludeInBudget));
    }

    [Fact]
    public void SyncAccounts_KeepsTheUsersChoice_AndDoesNotDuplicate()
    {
        var connection = Connect();
        var account = connection.SyncAccounts([Snapshot("a1")], Currency.SAR).Single();
        connection.SetAccountIncluded(account.Id, false, Currency.SAR);

        connection.SyncAccounts([Snapshot("a1") with { Nickname = "Renamed" }], Currency.SAR);

        var after = Assert.Single(connection.Accounts);
        Assert.False(after.IncludeInBudget);
        Assert.Equal("Renamed", after.Nickname);
    }

    [Fact]
    public void ForeignCurrencyAccount_CantBeIncluded()
    {
        var connection = Connect();
        var usd = connection.SyncAccounts([Snapshot("a1", currency: "USD")], Currency.SAR).Single();

        Assert.Throws<DomainException>(() => connection.SetAccountIncluded(usd.Id, true, Currency.SAR));
    }

    [Fact]
    public void CheckConsent_SendsEachReminderOnce()
    {
        var connection = Connect(Consent.Of(Now.AddMonths(-11), Now.AddDays(14)));

        connection.CheckConsent(Now, Today);
        connection.CheckConsent(Now.AddHours(1), Today);

        var reminder = Assert.Single(connection.DomainEvents.OfType<ConsentExpiringSoon>());
        Assert.Equal(14, reminder.DaysLeft);
    }

    [Fact]
    public void CheckConsent_AfterExpiry_ExpiresOnce()
    {
        var connection = Connect(Consent.Of(Now.AddMonths(-12), Now.AddMinutes(1)));

        connection.CheckConsent(Now.AddDays(1), Today.AddDays(1));
        connection.CheckConsent(Now.AddDays(2), Today.AddDays(2));

        Assert.Equal(ConnectionStatus.Expired, connection.Status);
        Assert.Single(connection.DomainEvents.OfType<BankConnectionExpired>());
    }

    [Fact]
    public void Expired_CantSyncOrRefresh()
    {
        var connection = Connect();
        connection.Expire();

        Assert.False(connection.CanRequestRefresh(Now));
        Assert.Throws<DomainException>(() => connection.RecordSync(Now, [], 0));
    }

    [Fact]
    public void Reconnect_ContinuesFromLastSync_AndResetsReminders()
    {
        var connection = Connect(Consent.Of(Now.AddMonths(-12), Now.AddDays(3)));
        connection.RecordSync(Now, [], 0);
        connection.CheckConsent(Now, Today);
        connection.Expire();

        connection.Reconnect("entity-2", Consent.ForDefaultPeriod(Now.AddDays(5)));

        Assert.True(connection.IsActive);
        Assert.Empty(connection.RemindersSent);
        Assert.Equal(SyncWindow.After(Today, Today.AddDays(5)), connection.NextSyncWindow(Today.AddDays(5)));
    }

    [Fact]
    public void Revoke_RaisesEventWithTheUsersChoice_Once()
    {
        var connection = Connect();

        connection.Revoke(keepTransactions: false);
        connection.Revoke(keepTransactions: false);

        Assert.Equal(new BankConnectionRevoked(connection.Id, UserId, false), Assert.Single(connection.DomainEvents));
        Assert.Throws<DomainException>(() => connection.Reconnect("entity-2", Consent.ForDefaultPeriod(Now)));
    }
}

public class BankConnectionPartialSyncTests
{
    [Fact]
    public void IncompleteFirstSync_KeepsTheFirstLinkWindow()
    {
        var connection = BankConnection.Connect(Guid.NewGuid(), "e", "LEA1", "Mockbank", Consent.ForDefaultPeriod(TestData.Now), TestData.Now);

        connection.RecordSync(TestData.Now, [], 0, complete: false);

        Assert.Equal(SyncWindow.ForFirstLink(TestData.Today), connection.NextSyncWindow(TestData.Today));
        Assert.False(connection.HistoryImported);
    }
}
