using System.Globalization;

namespace Mizan.Domain.Common;

public sealed record Money
{
    private const int DecimalPlaces = 2;

    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Of(decimal amount, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        if (decimal.Round(amount, DecimalPlaces) != amount)
        {
            throw new DomainException(
                $"Amount {amount} has more than {DecimalPlaces} decimal places.");
        }

        return new Money(amount, currency);
    }

    public static Money Zero(Currency currency) => Of(0m, currency);

    public bool IsZero => Amount == 0m;
    public bool IsNegative => Amount < 0m;
    public bool IsPositive => Amount > 0m;

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public Money DivideRoundingUpToWhole(int divisor)
    {
        if (divisor <= 0)
        {
            throw new DomainException($"Cannot divide money by {divisor}.");
        }

        return new Money(Math.Ceiling(Amount / divisor), Currency);
    }

    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right) => !(left < right);

    public static bool operator <=(Money left, Money right) => !(left > right);

    public override string ToString() =>
        $"{Amount.ToString("0.00", CultureInfo.InvariantCulture)} {Currency.Code}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Currency != right.Currency)
        {
            throw new DomainException(
                $"Cannot combine {left.Currency} with {right.Currency}.");
        }
    }
}
