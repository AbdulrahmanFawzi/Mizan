using Mizan.Domain.Categorization;
using Mizan.Domain.Common;
using Mizan.Domain.Importing;

namespace Mizan.Domain.Transactions;

// An expense the user recorded, or a transaction imported from their bank.
// Both are the same entity so the budget, alerts and "safe to spend" keep working unchanged;
// they only ask BudgetEffect instead of Amount.
public sealed class Transaction : AggregateRoot
{
    public const int MerchantMaxLength = 100;
    public const int NoteMaxLength = 200;

    public Guid UserId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Merchant { get; private set; }
    public MerchantKey? MerchantKey { get; private set; }
    public Guid? GoalId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public TransactionSource Source { get; private set; }
    public TransactionDirection Direction { get; private set; }
    public Treatment Treatment { get; private set; }
    public CategorySource CategorySource { get; private set; }
    public string? Note { get; private set; }

    // Bank transactions only. Stored as plain columns; the properties put them back together.
    private Guid? _bankConnectionId;
    private Guid? _bankAccountId;
    private string? _providerTransactionId;
    private decimal? _feeAmount;

    public BankReference? Bank => _bankConnectionId is { } connectionId && _bankAccountId is { } accountId && _providerTransactionId is { } id
        ? new BankReference(connectionId, accountId, id)
        : null;

    public BankStatus? BankStatus { get; private set; }
    public BankSubType? BankSubType { get; private set; }

    // When the other side is one of the user's own linked accounts (BR-R1). Kept so the transaction
    // can be classified again later (BR-X13); the other side's IBAN itself is never stored.
    public Guid? CounterpartyAccountId { get; private set; }
    public Money? Fee => _feeAmount is { } fee ? Money.Of(fee, Amount.Currency) : null;
    public bool IsCashWithdrawal { get; private set; }
    public bool IsExcluded { get; private set; }
    public ReviewReason? Review { get; private set; }
    public Guid? ReviewCandidateId { get; private set; }

    // Manual expenses only: "this looks like that bank transaction" while the user decides (OB-10).
    public Guid? PossibleDuplicateOf { get; private set; }

    private Transaction()
    {
    }

    public bool IsFromBank => Source == TransactionSource.Bank;

    // What this transaction adds to its category's spending. Can be negative for a refund.
    // The database computes the same value in a generated column; an integration test keeps them equal.
    public Money BudgetEffect
    {
        get
        {
            var zero = Money.Zero(Amount.Currency);

            // OB-10: while waiting for the user, only the bank side counts, and a possible transfer doesn't.
            if (IsExcluded || Review == ReviewReason.PossibleTransfer || PossibleDuplicateOf is not null)
            {
                return zero;
            }

            return Treatment switch
            {
                Treatment.Spending => Fee is null ? Amount : Amount + Fee, // BR-X11
                Treatment.Refund => zero - Amount,                          // BR-F2
                _ => zero,
            };
        }
    }

    // UC-30
    public static Transaction Record(
        Guid userId,
        Money amount,
        Guid categoryId,
        DateOnly date,
        string? merchant,
        Guid? goalId,
        DateOnly today,
        DateTimeOffset now)
    {
        var transaction = new Transaction
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            GoalId = goalId,
            CreatedAt = now,
            Source = TransactionSource.Manual,
            Direction = TransactionDirection.Debit,
            Treatment = Treatment.Spending,
            CategorySource = CategorySource.User,
        };

        transaction.Apply(amount, categoryId, date, merchant, today, now);
        transaction.Raise(new TransactionRecorded(transaction.Id, userId, categoryId, date, goalId));
        return transaction;
    }

    // UC-80. No event per transaction: the import raises one for the whole batch (BR-H3).
    public static Transaction ImportFromBank(
        Guid userId,
        BankReference bank,
        BankTransactionFacts facts,
        Treatment treatment,
        ReviewReason? review,
        Guid categoryId,
        CategorySource categorySource,
        DateTimeOffset now,
        Guid? counterpartyAccountId = null)
    {
        ArgumentNullException.ThrowIfNull(bank);
        ArgumentNullException.ThrowIfNull(facts);

        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category is required.");
        }

        var transaction = new Transaction
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            CreatedAt = now,
            Source = TransactionSource.Bank,
            _bankConnectionId = bank.ConnectionId,
            _bankAccountId = bank.AccountId,
            _providerTransactionId = bank.ProviderTransactionId,
            Treatment = treatment,
            Review = review,
            CategoryId = categoryId,
            CategorySource = categorySource,
            CounterpartyAccountId = counterpartyAccountId,
        };

        transaction.ApplyBankFacts(facts, now);
        return transaction;
    }

    // UC-31 (manual only: bank amounts and dates come from the bank, BR-X4).
    public void Update(Money amount, Guid categoryId, DateOnly date, string? merchant, DateOnly today, DateTimeOffset now)
    {
        EnsureManual("edited");
        Apply(amount, categoryId, date, merchant, today, now);
        RaiseUpdated();
    }

    // UC-32 (manual only: a deleted bank transaction would come back on the next sync, BR-X5).
    public void Delete()
    {
        EnsureManual("deleted");
        RaiseDeleted();
    }

    // UC-83: the bank changed this transaction. The user's choices stay (BR-X7).
    // Returns true when the budget is affected.
    public bool ApplyBankUpdate(BankTransactionFacts facts, DateTimeOffset now)
    {
        EnsureBank();

        var before = (Amount, Date, BankStatus, Fee);
        ApplyBankFacts(facts, now);

        var changed = before != (Amount, Date, BankStatus, Fee);
        if (changed)
        {
            RaiseUpdated();
        }

        return changed;
    }

    // BR-X9 (a pending transaction disappeared) or BR-C9 (the user removed the bank and its data).
    public void Discard()
    {
        EnsureBank();
        RaiseDeleted();
    }

    // UC-91
    public void Recategorize(Guid categoryId, CategorySource source, DateTimeOffset now)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category is required.");
        }

        if (categoryId == CategoryId && source == CategorySource)
        {
            return;
        }

        CategoryId = categoryId;
        CategorySource = source;
        UpdatedAt = now;
        RaiseUpdated();
    }

    // BR-B4: used when the category is deleted.
    public void MoveToCategory(Guid categoryId, DateTimeOffset now)
    {
        CategoryId = categoryId;
        UpdatedAt = now;
    }

    public void SetNote(string? note, DateTimeOffset now)
    {
        Note = Text.Optional(note, NoteMaxLength, "Note");
        UpdatedAt = now;
    }

    // UC-92 / BR-X5
    public void Exclude(DateTimeOffset now) => SetExcluded(true, now);

    public void Include(DateTimeOffset now) => SetExcluded(false, now);

    // BR-D4: a manual expense this bank transaction may duplicate.
    public void FlagPossibleDuplicate(Transaction manual, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(manual);
        EnsureBank();
        manual.EnsureManual("matched");

        if (Review is not null || manual.PossibleDuplicateOf is not null)
        {
            throw new DomainException("One of these transactions is already waiting for review.");
        }

        Review = ReviewReason.PossibleDuplicate;
        ReviewCandidateId = manual.Id;
        manual.PossibleDuplicateOf = Id;
        UpdatedAt = now;
        manual.UpdatedAt = now;
        manual.RaiseUpdated();
    }

    // BR-D5: the manual expense is the same purchase. Its category moves here and it goes away.
    public void ConfirmDuplicate(Transaction manual, DateTimeOffset now)
    {
        EnsureReviewOf(manual);

        CategoryId = manual.CategoryId;
        CategorySource = CategorySource.User;
        GoalId ??= manual.GoalId;
        Note ??= manual.Merchant;
        ClearReview(now);

        manual.PossibleDuplicateOf = null;
        manual.RaiseDeleted();
        RaiseUpdated();
    }

    // BR-D6: two real purchases. Both count from now on.
    public void RejectDuplicate(Transaction manual, DateTimeOffset now)
    {
        EnsureReviewOf(manual);

        ClearReview(now);
        manual.PossibleDuplicateOf = null;
        manual.UpdatedAt = now;
        manual.RaiseUpdated();
    }

    // BR-R1 fallback: the import found the same amount arriving in another of the user's accounts.
    public void LinkCounterparty(Guid accountId, DateTimeOffset now)
    {
        EnsureBank();
        CounterpartyAccountId = accountId;
        UpdatedAt = now;
    }

    // BR-X13: the budget rules for this transaction changed (its account was included or left out,
    // or the other side turned out to be the user's savings account). The user's own category stays.
    public void Reclassify(Classification classification, Guid savingsCategoryId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(classification);
        EnsureBank();

        var before = (Treatment, Review, CategoryId);

        Treatment = classification.Treatment;

        if (classification.Treatment == Treatment.OutsideBudget)
        {
            // Nothing to decide about a transaction that doesn't count. (A waiting duplicate is released by the caller first.)
            Review = null;
            ReviewCandidateId = null;
        }
        else if (classification.Review is { } review)
        {
            Review ??= review;
        }
        else if (Review == ReviewReason.PossibleTransfer)
        {
            Review = null;
        }

        // OB-13: saving goes to the Savings category, unless the user picked a category themselves.
        if (classification.IsSaving && CategorySource != CategorySource.User)
        {
            CategoryId = savingsCategoryId;
            CategorySource = CategorySource.Fallback;
        }

        if (before != (Treatment, Review, CategoryId))
        {
            UpdatedAt = now;
            RaiseUpdated();
        }
    }

    // OB-3: the user said whether this is a transfer between their accounts or real spending.
    public void ResolveTransfer(bool isOwnTransfer, DateTimeOffset now)
    {
        if (Review != ReviewReason.PossibleTransfer)
        {
            throw new DomainException("This transaction is not waiting for a transfer decision.");
        }

        Treatment = isOwnTransfer
            ? Treatment.OwnTransfer
            : Direction == TransactionDirection.Debit ? Treatment.Spending : Treatment.Incoming;

        ClearReview(now);
        RaiseUpdated();
    }

    private void SetExcluded(bool excluded, DateTimeOffset now)
    {
        EnsureBank();
        if (IsExcluded == excluded)
        {
            return;
        }

        IsExcluded = excluded;
        UpdatedAt = now;
        RaiseUpdated();
    }

    private void ClearReview(DateTimeOffset now)
    {
        Review = null;
        ReviewCandidateId = null;
        UpdatedAt = now;
    }

    private void EnsureReviewOf(Transaction manual)
    {
        ArgumentNullException.ThrowIfNull(manual);

        if (Review != ReviewReason.PossibleDuplicate || ReviewCandidateId != manual.Id)
        {
            throw new DomainException("These transactions are not waiting to be compared.");
        }
    }

    private void EnsureManual(string action)
    {
        if (IsFromBank)
        {
            throw new DomainException($"A bank transaction can't be {action}. Change its category or exclude it instead.");
        }
    }

    private void EnsureBank()
    {
        if (!IsFromBank)
        {
            throw new DomainException("This only applies to bank transactions.");
        }
    }

    private void ApplyBankFacts(BankTransactionFacts facts, DateTimeOffset now)
    {
        if (!facts.Amount.IsPositive)
        {
            throw new DomainException("Amount must be greater than zero.");
        }

        Amount = facts.Amount;
        Direction = facts.Direction;
        Date = facts.Date;                       // BR-T3 doesn't apply to bank dates
        BankStatus = facts.Status;
        BankSubType = facts.SubType;
        _feeAmount = facts.Fee is { IsPositive: true } fee && fee.Currency == facts.Amount.Currency ? fee.Amount : null;
        IsCashWithdrawal = facts.IsCashWithdrawal;
        Merchant = Text.Truncate(facts.MerchantName, MerchantMaxLength);
        MerchantKey = facts.MerchantKey;
        UpdatedAt = now;
    }

    private void Apply(Money amount, Guid categoryId, DateOnly date, string? merchant, DateOnly today, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(amount);

        // BR-T1
        if (!amount.IsPositive)
        {
            throw new DomainException("Amount must be greater than zero.");
        }

        // BR-T2
        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category is required.");
        }

        // BR-T3
        if (date > today)
        {
            throw new DomainException("Date cannot be in the future.");
        }

        Amount = amount;
        CategoryId = categoryId;
        Date = date;
        Merchant = Text.Optional(merchant, MerchantMaxLength, "Merchant");
        MerchantKey = MerchantKey.From(Merchant);
        UpdatedAt = now;
    }

    private void RaiseUpdated() => Raise(new TransactionUpdated(Id, UserId, CategoryId, Date, GoalId));

    private void RaiseDeleted() => Raise(new TransactionDeleted(Id, UserId, CategoryId, Date, GoalId));
}
