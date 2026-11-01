using Microsoft.Extensions.DependencyInjection;
using Mizan.Application.Abstractions;
using Mizan.Application.Auth;
using Mizan.Application.Banking;
using Mizan.Application.Budgets;
using Mizan.Application.Categories;
using Mizan.Application.Common;
using Mizan.Application.Dashboard;
using Mizan.Application.EventHandlers;
using Mizan.Application.Goals;
using Mizan.Application.Notifications;
using Mizan.Application.Profile;
using Mizan.Application.Transactions;

namespace Mizan.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<Clock>();

        services.AddScoped<AuthService>();
        services.AddScoped<EmailCodes>();
        services.AddScoped<ProfileService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<BudgetService>();
        services.AddScoped<TransactionService>();
        services.AddScoped<GoalService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ReviewService>();
        services.AddScoped<BankConnectionService>();
        services.AddScoped<BankImportService>();

        services.AddScoped<BudgetProvisioner>();
        services.AddScoped<BudgetReader>();
        services.AddScoped<BudgetAlerts>();
        services.AddScoped<GoalCompletion>();

        services.AddScoped<DomainEventDispatcher>();
        AddDomainEventHandlers(services);

        return services;
    }

    private static void AddDomainEventHandlers(IServiceCollection services)
    {
        var handlers = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>))
                .Select(i => (Service: i, Implementation: t)));

        foreach (var (service, implementation) in handlers)
        {
            services.AddScoped(service, implementation);
        }
    }
}
