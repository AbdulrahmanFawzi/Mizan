using Mizan.Domain.Banking;
using Mizan.Domain.Categorization;
using Mizan.Domain.Common;

namespace Mizan.Domain.Transactions;

// Which bank transaction this is: the connection, the account, and the provider's id (BR-S5).
public sealed record BankReference(Guid ConnectionId, Guid AccountId, string ProviderTransactionId);

// What the bank tells us about one transaction, already in our own shape (not Lean's).
public sealed record BankTransactionFacts(
    string ProviderTransactionId,
    Money Amount,
    TransactionDirection Direction,
    BankStatus Status,
    DateOnly Date,
    string? MerchantName,
    ProviderCategory? ProviderCategory,
    BankSubType SubType,
    Iban? CounterpartyIban,
    Money? Fee,
    bool IsCashWithdrawal)
{
    public MerchantKey? MerchantKey => MerchantKey.From(MerchantName);
}
