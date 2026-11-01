namespace Mizan.Domain.Transactions;

public enum TransactionSource
{
    Manual,
    Bank,
}

// BR-X1. Manual expenses are always Debit.
public enum TransactionDirection
{
    Debit,
    Credit,
}

public enum BankStatus
{
    Pending,
    Booked,
}

// How a transaction affects the budget. The budget asks this instead of adding up every amount.
public enum Treatment
{
    Spending,       // counts (BR-X1)
    Refund,         // lowers its category's spending (BR-F2)
    OwnTransfer,    // money moved between the user's own accounts (BR-R)
    Incoming,       // money in that isn't a refund (BR-F7)
    OutsideBudget,  // from an account that doesn't count, or another currency (BR-X3, BR-X10)
}

// Where the category came from (BR-K1, BR-K6).
public enum CategorySource
{
    User,
    MerchantRule,
    Provider,
    Fallback,
}

public enum ReviewReason
{
    PossibleDuplicate,  // OB-1
    PossibleTransfer,   // OB-3
}

// What the bank says about the kind of money movement, when it says anything (BR-F3).
public enum BankSubType
{
    None,
    Refund,
    Reversal,
}
