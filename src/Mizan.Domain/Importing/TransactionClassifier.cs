using Mizan.Domain.Banking;
using Mizan.Domain.Common;
using Mizan.Domain.Transactions;

namespace Mizan.Domain.Importing;

// IsSaving: money moved into (or out of) the user's savings account. It belongs in the Savings category (OB-13).
public sealed record Classification(Treatment Treatment, ReviewReason? Review, bool IsSaving = false);

// The account a bank transaction came from, as far as the budget cares.
public sealed record AccountContext(bool IncludeInBudget, Currency Currency);

// The other side of the transaction, when it is one of the user's own linked accounts.
// TransfersInAreSpending: the user's remembered answer for transfers into it (BR-R6), null = ask.
public sealed record CounterpartyAccount(Guid AccountId, AccountKind Kind, bool? TransfersInAreSpending = null)
{
    public static CounterpartyAccount Of(BankAccount account) => new(account.Id, account.Kind, account.TransfersInAreSpending);
}

// Decides how a bank transaction affects the budget. Pure: no database, no network.
// It only uses facts we keep on the transaction, so it can run again later (BR-X13).
public static class TransactionClassifier
{
    public static Classification Classify(
        TransactionDirection direction,
        BankSubType subType,
        Currency currency,
        AccountContext account,
        CounterpartyAccount? counterparty,
        Currency budgetCurrency)
    {
        ArgumentNullException.ThrowIfNull(account);

        // BR-X3 and BR-X10: accounts that don't count, and other currencies, stay out of the budget.
        if (!account.IncludeInBudget || account.Currency != budgetCurrency || currency != budgetCurrency)
        {
            return new Classification(Treatment.OutsideBudget, null);
        }

        var fromOrToSavings = counterparty?.Kind == AccountKind.Savings;

        if (direction == TransactionDirection.Credit)
        {
            // BR-F1, BR-F2, BR-F3, BR-F7. Money in never counts as spending.
            if (subType is BankSubType.Refund or BankSubType.Reversal)
            {
                return new Classification(Treatment.Refund, null);
            }

            // OB-13: taking money back out of savings lowers what was saved.
            if (fromOrToSavings)
            {
                return new Classification(Treatment.Refund, null, IsSaving: true);
            }

            // The incoming side of an own transfer has nothing to decide: it never counts anyway.
            return new Classification(counterparty is null ? Treatment.Incoming : Treatment.OwnTransfer, null);
        }

        // OB-13: moving money into the savings account is saving. No question needed.
        if (fromOrToSavings)
        {
            return new Classification(Treatment.Spending, null, IsSaving: true);
        }

        // OB-3 (BR-R3): ask the user, unless they asked us to remember (BR-R6). Card payments land here too (BR-R2).
        if (counterparty is not null)
        {
            return counterparty.TransfersInAreSpending switch
            {
                true => new Classification(Treatment.Spending, null),
                false => new Classification(Treatment.OwnTransfer, null),
                null => new Classification(Treatment.Spending, ReviewReason.PossibleTransfer),
            };
        }

        // BR-R5 and BR-X12: transfers to other people and ATM withdrawals are spending.
        return new Classification(Treatment.Spending, null);
    }
}
