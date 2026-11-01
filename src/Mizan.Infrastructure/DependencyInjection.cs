using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mizan.Application.Abstractions;
using Mizan.Infrastructure.Background;
using Mizan.Infrastructure.Banking;
using Mizan.Infrastructure.Banking.Lean;
using Mizan.Infrastructure.Messaging;
using Mizan.Infrastructure.Persistence;
using Mizan.Infrastructure.Persistence.Repositories;
using Mizan.Infrastructure.Security;

namespace Mizan.Infrastructure;

public static class DependencyInjection
{
    public const int MinSigningKeyBytes = 32;

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Mizan")
            ?? throw new InvalidOperationException("Connection string 'Mizan' is not configured.");

        services.AddDbContext<MizanDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => System.Text.Encoding.UTF8.GetByteCount(o.SigningKey) >= MinSigningKeyBytes,
                $"Jwt:SigningKey must be at least {MinSigningKeyBytes} bytes.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        AddBanking(services, configuration);

        return services;
    }

    private static void AddBanking(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BankingSecurityOptions>()
            .Bind(configuration.GetSection(BankingSecurityOptions.SectionName))
            .Validate(o => IsBase64Key(o.EncryptionKey), "Banking:EncryptionKey must be 32 random bytes in base64.")
            .ValidateOnStart();
        services.AddSingleton<IbanProtector>();

        services.AddScoped<IBankingCustomerRepository, BankingCustomerRepository>();
        services.AddScoped<IBankConnectionRepository, BankConnectionRepository>();
        services.AddScoped<IMerchantRuleRepository, MerchantRuleRepository>();
        services.AddScoped<IRejectedMatchRepository, RejectedMatchRepository>();
        services.AddScoped<IBankTransactionRepository, BankTransactionRepository>();
        services.AddScoped<IOutbox, EfOutbox>();
        services.AddScoped<ISyncLock, PostgresSyncLock>();
        services.AddScoped<WebhookInbox>();

        var lean = configuration.GetSection(LeanOptions.SectionName);
        services.Configure<LeanOptions>(lean);
        services.AddSingleton<LeanWebhooks>();

        if (lean.Get<LeanOptions>() is { IsConfigured: true } options)
        {
            services.AddHttpClient(LeanTokenCache.HttpClientName).AddStandardResilienceHandler();
            services.AddSingleton<LeanTokenCache>();

            services.AddHttpClient<IBankingProvider, LeanBankingProvider>(http => http.BaseAddress = new Uri(options.ApiUrl))
                .AddStandardResilienceHandler(resilience =>
                {
                    // Lean may take up to ~50 seconds before answering PENDING.
                    resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                    resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
                    resilience.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(3);

                    // One retry is enough: Lean answers 504 while it rebuilds data, and the next sync picks it up.
                    resilience.Retry.MaxRetryAttempts = 1;
                });
        }
        else
        {
            services.AddScoped<IBankingProvider, NotConfiguredBankingProvider>();
        }

        services.Configure<BackgroundJobOptions>(configuration.GetSection(BackgroundJobOptions.SectionName));
        services.AddSingleton<BankingJobRunner>();
        if (configuration.GetSection(BackgroundJobOptions.SectionName).Get<BackgroundJobOptions>()?.Enabled ?? true)
        {
            services.AddHostedService<MessageProcessorService>();
            services.AddHostedService<DailySyncService>();
        }
    }

    private static bool IsBase64Key(string value)
    {
        Span<byte> buffer = stackalloc byte[64];
        return Convert.TryFromBase64String(value, buffer, out var written) && written == 32;
    }
}
