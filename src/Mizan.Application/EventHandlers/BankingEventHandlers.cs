using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Domain.Banking;
using Mizan.Domain.Notifications;
using Mizan.Domain.Transactions;

namespace Mizan.Application.EventHandlers;

// BR-H3: one evaluation per category for the whole import, then one "please review" notice.
public sealed class EvaluateBudgetOnBankImport(BudgetAlerts alerts, INotificationRepository notifications, Clock clock)
    : IDomainEventHandler<BankTransactionsImported>
{
    public async Task HandleAsync(BankTransactionsImported e, CancellationToken cancellationToken)
    {
        // The latest date per category is enough: BudgetAlerts only looks at the current period (BR-A5).
        foreach (var spending in e.Affected.GroupBy(a => a.CategoryId).Select(g => g.MaxBy(a => a.Date)!))
        {
            await alerts.EvaluateAsync(e.UserId, spending.CategoryId, spending.Date, cancellationToken);
        }

        if (e.NewReviewItems > 0)
        {
            notifications.Add(Notification.ReviewNeeded(e.UserId, e.NewReviewItems, clock.Now));
        }
    }
}

public sealed class NotifyConsentExpiring(INotificationRepository notifications, Clock clock)
    : IDomainEventHandler<ConsentExpiringSoon>
{
    public Task HandleAsync(ConsentExpiringSoon e, CancellationToken cancellationToken)
    {
        notifications.Add(Notification.ConsentExpiring(e.UserId, e.ConnectionId, e.BankName, e.DaysLeft, e.ExpiresOn, clock.Now));
        return Task.CompletedTask;
    }
}

public sealed class NotifyConsentExpired(INotificationRepository notifications, Clock clock)
    : IDomainEventHandler<BankConnectionExpired>
{
    public Task HandleAsync(BankConnectionExpired e, CancellationToken cancellationToken)
    {
        notifications.Add(Notification.ConsentExpired(e.UserId, e.ConnectionId, e.BankName, clock.Now));
        return Task.CompletedTask;
    }
}

// BR-C9: the user removed a bank and chose to delete what it imported.
public sealed class DeleteTransactionsOfRevokedConnection(
    ITransactionRepository transactions,
    IBankTransactionRepository bankTransactions,
    Clock clock)
    : IDomainEventHandler<BankConnectionRevoked>
{
    public async Task HandleAsync(BankConnectionRevoked e, CancellationToken cancellationToken)
    {
        if (e.KeepTransactions)
        {
            return;
        }

        foreach (var transaction in await bankTransactions.ListByConnectionAsync(e.ConnectionId, cancellationToken))
        {
            // A manual expense waiting next to it goes back to counting normally.
            if (transaction is { Review: ReviewReason.PossibleDuplicate, ReviewCandidateId: { } manualId }
                && await transactions.GetAsync(e.UserId, manualId, cancellationToken) is { } manual)
            {
                transaction.RejectDuplicate(manual, clock.Now);
            }

            transaction.Discard();
            transactions.Remove(transaction);
        }
    }
}
