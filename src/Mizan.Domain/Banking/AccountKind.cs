namespace Mizan.Domain.Banking;

public enum AccountKind
{
    Current,
    Savings,
    CreditCard,
    ChargeCard,
    Prepaid,
    EMoney,
    Other,
}

public static class AccountKindExtensions
{
    // BR-X10: spending from these accounts counts by default. Savings only shows its balance.
    public static bool CountsTowardBudgetByDefault(this AccountKind kind) =>
        kind is not (AccountKind.Savings or AccountKind.Other);
}
