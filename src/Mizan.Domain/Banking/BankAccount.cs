using Mizan.Domain.Common;

namespace Mizan.Domain.Banking;

// What the bank tells us about one account, every sync.
public sealed record BankAccountSnapshot(
    string ProviderAccountId,
    AccountKind Kind,
    Currency Currency,
    Iban? Iban,
    string Nickname,
    Money? Balance,
    DateTimeOffset? BalanceAsOf);

// One account inside a bank connection. It has no life outside its connection.
public sealed class BankAccount
{
    public const int NicknameMaxLength = 100;

    public Guid Id { get; private set; }
    public Guid ConnectionId { get; private set; }
    public string ProviderAccountId { get; private set; } = null!;
    public AccountKind Kind { get; private set; }
    public Currency Currency { get; private set; } = null!;
    public Iban? Iban { get; private set; }
    public string Nickname { get; private set; } = null!;
    public bool IncludeInBudget { get; private set; }
    public DateTimeOffset? BalanceAsOf { get; private set; }

    // Stored as one number in the account's own currency.
    private decimal? _balanceAmount;

    public Money? Balance => _balanceAmount is { } amount ? Money.Of(amount, Currency) : null;

    private BankAccount()
    {
    }

    internal static BankAccount Create(Guid connectionId, BankAccountSnapshot snapshot, Currency budgetCurrency)
    {
        var account = new BankAccount
        {
            Id = Guid.CreateVersion7(),
            ConnectionId = connectionId,
            ProviderAccountId = snapshot.ProviderAccountId,
            Kind = snapshot.Kind,
            Currency = snapshot.Currency,

            // BR-X10 and BR-X3: the default comes from the kind; other currencies never count.
            IncludeInBudget = snapshot.Kind.CountsTowardBudgetByDefault() && snapshot.Currency == budgetCurrency,
        };

        account.Refresh(snapshot);
        return account;
    }

    // The user's choice (IncludeInBudget) is kept; only bank facts change.
    internal void Refresh(BankAccountSnapshot snapshot)
    {
        Kind = snapshot.Kind;
        Currency = snapshot.Currency;
        Iban = snapshot.Iban ?? Iban;
        Nickname = Text.Truncate(snapshot.Nickname, NicknameMaxLength) ?? Kind.ToString();
        if (snapshot.Balance is { } balance && balance.Currency == Currency)
        {
            _balanceAmount = balance.Amount;
            BalanceAsOf = snapshot.BalanceAsOf ?? BalanceAsOf;
        }
    }

    internal void SetIncludeInBudget(bool include, Currency budgetCurrency)
    {
        if (include && Currency != budgetCurrency)
        {
            throw new DomainException($"Accounts in {Currency} can't be part of a {budgetCurrency} budget.");
        }

        IncludeInBudget = include;
    }
}
