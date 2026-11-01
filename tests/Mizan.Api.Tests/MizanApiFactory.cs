using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Mizan.Application.Abstractions;
using Mizan.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Mizan.Api.Tests;

// Starts the real API against a real, throwaway PostgreSQL in Docker.
public sealed class MizanApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string WebhookSecret = "test-webhook-secret";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public FakeBankingProvider Bank { get; } = new();

    public FakeEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Mizan", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "100000");
        builder.UseSetting("Banking:EncryptionKey", "dGVzdHMtb25seS1pYmFuLWtleS0zMi1ieXRlcy0hISE=");

        // Tests run background work themselves (BankingJobRunner), so the timers stay off.
        builder.UseSetting("BackgroundJobs:Enabled", "false");
        builder.UseSetting("Lean:WebhookSecret", WebhookSecret);

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IBankingProvider>(Bank);
            services.AddSingleton<IEmailSender>(Emails);
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MizanDbContext>().Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<MizanApiFactory>
{
    public const string Name = "api";
}
