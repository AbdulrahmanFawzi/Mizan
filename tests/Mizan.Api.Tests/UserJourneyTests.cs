using System.Net;
using Mizan.Application.Budgets;
using Mizan.Application.Categories;
using Mizan.Application.Dashboard;
using Mizan.Application.Goals;
using Mizan.Application.Notifications;
using Mizan.Application.Transactions;
using Mizan.Domain.Categories;
using Mizan.Domain.Goals;
using Mizan.Domain.Notifications;

namespace Mizan.Api.Tests;

// The MVP 0 Definition of Done, end to end through HTTP and PostgreSQL.
[Collection(ApiCollection.Name)]
public sealed class UserJourneyTests(MizanApiFactory factory)
{
    [Fact]
    public async Task NewUser_StartsWithSystemCategoriesAndCurrentBudget()
    {
        var api = await ApiClient.RegisterAsync(factory);

        var categories = await api.GetAsync<List<CategoryDto>>("/api/categories");
        var budget = await api.GetAsync<BudgetDto>("/api/budgets/current");

        Assert.Equal([CategoryKind.Other, CategoryKind.Savings], categories.Select(c => c.Kind));
        Assert.Equal(10_000m, budget.PlannedIncome.Amount);
        Assert.True(budget.Period.Start <= ApiClient.Today && ApiClient.Today <= budget.Period.End);
        Assert.Equal(2, budget.Categories.Count);
    }

    [Fact]
    public async Task Expenses_UpdateBudget_AndRaiseThresholdAlerts()
    {
        var api = await ApiClient.RegisterAsync(factory);
        var fuel = await api.PostAsync<CategoryDto>("/api/categories", new { name = "الوقود", monthlyLimit = 500 });

        // PRD example: 50 SAR on a 500 SAR fuel budget leaves 450.
        await Spend(api, fuel.Id, 50);
        var budget = await api.GetAsync<BudgetDto>("/api/budgets/current");
        var fuelStatus = budget.Categories.Single(c => c.CategoryId == fuel.Id);
        Assert.Equal(450m, fuelStatus.Remaining.Amount);
        Assert.Equal(10m, fuelStatus.PercentUsed);
        Assert.Empty(await api.GetAsync<List<NotificationDto>>("/api/notifications"));

        await Spend(api, fuel.Id, 350); // 80%
        await Spend(api, fuel.Id, 10);  // 82%: no new alert
        await Spend(api, fuel.Id, 140); // 110%: jumps over 90, only the highest (100) is sent

        var notifications = await api.GetAsync<List<NotificationDto>>("/api/notifications");
        Assert.Equal(["100", "80"], notifications.Select(n => n.Data["threshold"]));
        Assert.All(notifications, n => Assert.Equal(NotificationType.BudgetThreshold, n.Type));

        var dashboard = await api.GetAsync<DashboardDto>("/api/dashboard");
        Assert.Equal(550m, dashboard.TotalSpent.Amount);
        Assert.Equal(2, dashboard.UnreadNotifications);
        Assert.Equal("الوقود", Assert.Single(dashboard.TopSpending).Name);
    }

    [Fact]
    public async Task OverBudget_ShowsPlannedVsActualInsight()
    {
        var api = await ApiClient.RegisterAsync(factory);
        var food = await api.PostAsync<CategoryDto>("/api/categories", new { name = "Food", monthlyLimit = 1000 });

        await Spend(api, food.Id, 1270);

        var dashboard = await api.GetAsync<DashboardDto>("/api/dashboard");
        var insight = Assert.Single(dashboard.Insights);
        Assert.Equal("OverBudget", insight.Type);
        Assert.Contains("27%", insight.Message);
    }

    [Fact]
    public async Task GoalContribution_CountsAsSavings_AndCompletesTheGoal()
    {
        var api = await ApiClient.RegisterAsync(factory);
        var goal = await api.PostAsync<GoalDto>("/api/goals", new
        {
            name = "Travel",
            targetAmount = 10_000,
            initialAmount = 3_500,
            targetDate = ApiClient.Today.AddYears(1),
        });
        Assert.Equal(6_500m, goal.Remaining.Amount);
        Assert.True(goal.RequiredPerPeriod.Amount > 0);

        await api.PostAsync<TransactionDto>($"/api/goals/{goal.Id}/contributions", new { amount = 6_500 });

        var updated = await api.GetAsync<GoalDto>($"/api/goals/{goal.Id}");
        Assert.Equal(GoalStatus.Completed, updated.Status);
        Assert.Equal(10_000m, updated.CurrentAmount.Amount);

        // D4: the contribution is spending in the Savings category, without a "budget exceeded" alert (BR-A4).
        var budget = await api.GetAsync<BudgetDto>("/api/budgets/current");
        Assert.Equal(6_500m, budget.Categories.Single(c => c.Kind == CategoryKind.Savings).Spent.Amount);
        var notification = Assert.Single(await api.GetAsync<List<NotificationDto>>("/api/notifications"));
        Assert.Equal(NotificationType.GoalCompleted, notification.Type);
    }

    [Fact]
    public async Task DeletingCategory_MovesItsTransactionsToOther()
    {
        var api = await ApiClient.RegisterAsync(factory);
        var shopping = await api.PostAsync<CategoryDto>("/api/categories", new { name = "Shopping", monthlyLimit = 800 });
        var transaction = await Spend(api, shopping.Id, 120);

        var response = await api.Http.DeleteAsync($"/api/categories/{shopping.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var other = (await api.GetAsync<List<CategoryDto>>("/api/categories")).Single(c => c.Kind == CategoryKind.Other);
        Assert.Equal(other.Id, (await api.GetAsync<TransactionDto>($"/api/transactions/{transaction.Id}")).CategoryId);
    }

    [Fact]
    public async Task SystemCategory_CannotBeDeleted()
    {
        var api = await ApiClient.RegisterAsync(factory);
        var other = (await api.GetAsync<List<CategoryDto>>("/api/categories")).Single(c => c.Kind == CategoryKind.Other);

        var response = await api.Http.DeleteAsync($"/api/categories/{other.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EditingAndDeletingTransactions_RecalculatesBudget()
    {
        var api = await ApiClient.RegisterAsync(factory);
        var fuel = await api.PostAsync<CategoryDto>("/api/categories", new { name = "Fuel", monthlyLimit = 500 });
        var transaction = await Spend(api, fuel.Id, 100);

        await api.PutAsync<TransactionDto>($"/api/transactions/{transaction.Id}", new
        {
            amount = 150,
            categoryId = fuel.Id,
            date = ApiClient.Today,
        });
        Assert.Equal(150m, (await FuelStatus(api, fuel.Id)).Spent.Amount);

        await api.Http.DeleteAsync($"/api/transactions/{transaction.Id}");
        Assert.Equal(0m, (await FuelStatus(api, fuel.Id)).Spent.Amount);
    }

    [Fact]
    public async Task ChangingIncome_UpdatesCurrentBudget()
    {
        var api = await ApiClient.RegisterAsync(factory);

        await api.PutAsync<object>("/api/me", new { name = "Test User", monthlyIncome = 12_000, periodStartDay = 1 });

        Assert.Equal(12_000m, (await api.GetAsync<BudgetDto>("/api/budgets/current")).PlannedIncome.Amount);
    }

    private static Task<TransactionDto> Spend(ApiClient api, Guid categoryId, decimal amount) =>
        api.PostAsync<TransactionDto>("/api/transactions", new
        {
            amount,
            categoryId,
            date = ApiClient.Today,
            merchant = "Test merchant",
        });

    private static async Task<CategoryStatusDto> FuelStatus(ApiClient api, Guid fuelId) =>
        (await api.GetAsync<BudgetDto>("/api/budgets/current")).Categories.Single(c => c.CategoryId == fuelId);
}
