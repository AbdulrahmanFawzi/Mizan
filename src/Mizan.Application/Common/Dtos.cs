using Mizan.Domain.Common;

namespace Mizan.Application.Common;

public sealed record MoneyDto(decimal Amount, string Currency)
{
    public static MoneyDto From(Money money) => new(money.Amount, money.Currency.Code);
}

public sealed record PeriodDto(DateOnly Start, DateOnly End, int LengthInDays, int DaysElapsed);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
