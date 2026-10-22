using Mizan.Application.Abstractions;
using Mizan.Application.Budgets;
using Mizan.Application.Common;
using Mizan.Application.Goals;
using Mizan.Domain.Categories;
using Mizan.Domain.Goals;
using Mizan.Domain.Insights;

namespace Mizan.Application.Dashboard;

public sealed record TopSpendingDto(Guid CategoryId, string Name, MoneyDto Spent);

public sealed record InsightDto(string Type, Guid CategoryId, string CategoryName, string Message);

public sealed record DashboardDto(
    PeriodDto Period,
    MoneyDto MonthlyIncome,
    MoneyDto TotalSpent,
    MoneyDto Available,
    decimal? BudgetUsagePercent,
    IReadOnlyList<TopSpendingDto> TopSpending,
    IReadOnlyList<GoalDto> Goals,
    IReadOnlyList<InsightDto> Insights,
    int UnreadNotifications);

// UC-50, UC-52
public sealed class DashboardService(
    IUserRepository users,
    BudgetProvisioner provisioner,
    BudgetReader reader,
    GoalService goalService,
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    Clock clock)
{
    public const int TopSpendingCount = 3;

    public async Task<DashboardDto> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var today = clock.Today;
        var user = await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User");
        var budget = await provisioner.GetOrCreateCurrentAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var snapshot = await reader.ReadAsync(budget, cancellationToken);
        var totalSpent = snapshot.TotalSpent;

        var topSpending = snapshot.SpentByCategory
            .Where(s => snapshot.Categories.TryGetValue(s.Key, out var c) && c.Kind != CategoryKind.Savings && s.Value.IsPositive)
            .OrderByDescending(s => s.Value.Amount)
            .Take(TopSpendingCount)
            .Select(s => new TopSpendingDto(s.Key, snapshot.Categories[s.Key].Name.Value, MoneyDto.From(s.Value)))
            .ToList();

        var insights = SpendingAnalysis.Generate(budget, snapshot.SpentByCategory, snapshot.Kinds, today)
            .Where(i => snapshot.Categories.ContainsKey(i.CategoryId))
            .Select(i => InsightText.ToDto(i, snapshot.Categories[i.CategoryId].Name.Value))
            .ToList();

        var goals = (await goalService.ListAsync(userId, includeArchived: false, cancellationToken))
            .Where(g => g.Status is GoalStatus.Active or GoalStatus.Overdue)
            .ToList();

        var allocated = budget.TotalAllocated;
        decimal? usage = allocated.IsZero
            ? null
            : decimal.Round(totalSpent.Amount / allocated.Amount * 100m, 0, MidpointRounding.AwayFromZero);

        var period = budget.Period;
        return new DashboardDto(
            new PeriodDto(period.Start, period.End, period.LengthInDays, period.DaysElapsed(today)),
            MoneyDto.From(budget.PlannedIncome),
            MoneyDto.From(totalSpent),
            MoneyDto.From(budget.PlannedIncome - totalSpent),
            usage,
            topSpending,
            goals,
            insights,
            await notifications.CountUnreadAsync(userId, cancellationToken));
    }
}

internal static class InsightText
{
    public static InsightDto ToDto(Insight insight, string categoryName) => insight switch
    {
        OverBudgetInsight { PercentOver: { } percent } i => new(
            "OverBudget", i.CategoryId, categoryName,
            $"أنفقت {percent}% أكثر من ميزانيتك على {categoryName} في هذه الفترة."),

        OverBudgetInsight i => new(
            "OverBudget", i.CategoryId, categoryName,
            $"صرفت {MoneyFormat.Riyals(i.OverBy)} على {categoryName} وميزانيتها صفر."),

        ProjectedOverBudgetInsight i => new(
            "ProjectedOverBudget", i.CategoryId, categoryName,
            $"بناءً على معدل إنفاقك الحالي، من المتوقع أن تصرف {MoneyFormat.Riyals(i.Projected)} على {categoryName} وتتجاوز ميزانيتها ({MoneyFormat.Riyals(i.Limit)}) قبل نهاية الفترة."),

        _ => throw new ArgumentOutOfRangeException(nameof(insight)),
    };
}
