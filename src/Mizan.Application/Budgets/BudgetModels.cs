using Mizan.Application.Common;
using Mizan.Domain.Categories;

namespace Mizan.Application.Budgets;

public sealed record SetLimitCommand(decimal Limit);

public sealed record CategoryStatusDto(
    Guid CategoryId,
    string Name,
    CategoryKind Kind,
    MoneyDto Limit,
    MoneyDto Spent,
    MoneyDto Remaining,
    decimal? PercentUsed,
    bool IsOverBudget);

public sealed record BudgetDto(
    Guid Id,
    PeriodDto Period,
    MoneyDto PlannedIncome,
    MoneyDto TotalAllocated,
    MoneyDto Unallocated,
    bool IsOverAllocated,
    MoneyDto TotalSpent,
    IReadOnlyList<CategoryStatusDto> Categories);
