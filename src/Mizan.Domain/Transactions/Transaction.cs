using Mizan.Domain.Common;

namespace Mizan.Domain.Transactions;

public sealed class Transaction : AggregateRoot
{
    public const int MerchantMaxLength = 100;

    public Guid UserId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Merchant { get; private set; }
    public Guid? GoalId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Transaction()
    {
    }

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
        };

        transaction.Apply(amount, categoryId, date, merchant, today, now);
        transaction.Raise(new TransactionRecorded(transaction.Id, userId, categoryId, date, goalId));
        return transaction;
    }

    // BR-T5: listeners re-evaluate the new category and period.
    public void Update(Money amount, Guid categoryId, DateOnly date, string? merchant, DateOnly today, DateTimeOffset now)
    {
        Apply(amount, categoryId, date, merchant, today, now);
        Raise(new TransactionUpdated(Id, UserId, CategoryId, Date, GoalId));
    }

    public void Delete() => Raise(new TransactionDeleted(Id, UserId, CategoryId, Date, GoalId));

    // BR-B4: used when the category is deleted.
    public void MoveToCategory(Guid categoryId, DateTimeOffset now)
    {
        CategoryId = categoryId;
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
        UpdatedAt = now;
    }
}
