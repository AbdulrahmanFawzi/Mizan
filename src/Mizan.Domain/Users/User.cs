using Mizan.Domain.Budgets;
using Mizan.Domain.Common;

namespace Mizan.Domain.Users;

public sealed class User : AggregateRoot
{
    // BR-M1: SAR only in MVP 0.
    public static IReadOnlyList<Currency> SupportedCurrencies { get; } = [Currency.SAR];

    public PersonName Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public Currency Currency { get; private set; } = null!;
    public Money MonthlyIncome { get; private set; } = null!;
    public PeriodStartDay PeriodStartDay { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private User()
    {
    }

    public static User Register(
        PersonName name,
        Email email,
        string passwordHash,
        Money monthlyIncome,
        PeriodStartDay periodStartDay,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentNullException.ThrowIfNull(monthlyIncome);
        ArgumentNullException.ThrowIfNull(periodStartDay);

        if (!SupportedCurrencies.Contains(monthlyIncome.Currency))
        {
            throw new DomainException($"Currency {monthlyIncome.Currency} is not supported.");
        }

        EnsurePositive(monthlyIncome);

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            Currency = monthlyIncome.Currency,
            MonthlyIncome = monthlyIncome,
            PeriodStartDay = periodStartDay,
            CreatedAt = now,
        };

        user.Raise(new UserRegistered(user.Id));
        return user;
    }

    public void Rename(PersonName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }

    public void ChangeMonthlyIncome(Money monthlyIncome)
    {
        ArgumentNullException.ThrowIfNull(monthlyIncome);

        if (monthlyIncome.Currency != Currency)
        {
            throw new DomainException($"Monthly income must be in {Currency}.");
        }

        EnsurePositive(monthlyIncome);

        if (monthlyIncome == MonthlyIncome)
        {
            return;
        }

        MonthlyIncome = monthlyIncome;
        Raise(new MonthlyIncomeChanged(Id, monthlyIncome));
    }

    // BR-P4: the current budget keeps its own period; the new day applies when the next one is created.
    public void ChangePeriodStartDay(PeriodStartDay periodStartDay)
    {
        ArgumentNullException.ThrowIfNull(periodStartDay);
        PeriodStartDay = periodStartDay;
    }

    // BR-I1
    private static void EnsurePositive(Money monthlyIncome)
    {
        if (!monthlyIncome.IsPositive)
        {
            throw new DomainException("Monthly income must be greater than zero.");
        }
    }
}
