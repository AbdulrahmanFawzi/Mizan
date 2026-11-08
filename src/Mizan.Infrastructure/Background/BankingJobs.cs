using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mizan.Application.Abstractions;
using Mizan.Application.Banking;
using Mizan.Application.Common;
using Mizan.Infrastructure.Banking.Lean;
using Mizan.Infrastructure.Messaging;
using Mizan.Infrastructure.Persistence;

namespace Mizan.Infrastructure.Background;

public sealed class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    public bool Enabled { get; init; } = true;

    // BR-S3: the daily bank refresh, Riyadh time.
    public int DailySyncHour { get; init; } = 6;

    public int PollSeconds { get; init; } = 5;
}

// The work behind the background services, kept separate so tests can run it directly.
public sealed class BankingJobRunner(IServiceScopeFactory scopes, Clock clock, ILogger<BankingJobRunner> logger)
{
    private const int BatchSize = 20;
    private const int MaxAttempts = 10;

    // Webhooks saved by the API endpoint (UC-71, UC-75, UC-80).
    public async Task<int> ProcessInboxAsync(CancellationToken cancellationToken)
    {
        List<string> due;
        using (var scope = scopes.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MizanDbContext>();
            due = await db.Inbox
                .Where(m => m.ProcessedAt == null && m.NextAttemptAt <= clock.Now)
                .OrderBy(m => m.ReceivedAt)
                .Select(m => m.EventId)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
        }

        foreach (var eventId in due)
        {
            // A fresh scope per message: one failure never leaves half-saved changes for the next.
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MizanDbContext>();
            var message = await db.Inbox.SingleAsync(m => m.EventId == eventId, cancellationToken);

            await RunAsync(message.Type, async () =>
            {
                if (LeanWebhooks.ToSignal(message.Type, message.Payload) is { } signal)
                {
                    await scope.ServiceProvider.GetRequiredService<BankConnectionService>().HandleSignalAsync(signal, cancellationToken);
                }
            }, error => Record(message, error), cancellationToken);

            db.ChangeTracker.Clear();
            db.Inbox.Update(message);
            await db.SaveChangesAsync(cancellationToken);
        }

        return due.Count;
    }

    // Calls to the provider that must not be lost (BR-C10).
    public async Task<int> ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MizanDbContext>();
        var provider = scope.ServiceProvider.GetRequiredService<IBankingProvider>();

        var due = await db.Outbox
            .Where(m => m.ProcessedAt == null && m.NextAttemptAt <= clock.Now)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in due)
        {
            await RunAsync(message.Type, async () =>
            {
                switch (message.Type)
                {
                    case OutboxTypes.DeleteCustomer:
                        var payload = JsonSerializer.Deserialize<DeleteCustomerMessage>(message.Payload, JsonSerializerOptions.Web)!;
                        await provider.DeleteCustomerAsync(payload.ProviderCustomerId, cancellationToken);
                        break;
                    case OutboxTypes.SendEmail:
                        var email = JsonSerializer.Deserialize<EmailMessage>(message.Payload, JsonSerializerOptions.Web)!;
                        await scope.ServiceProvider.GetRequiredService<IEmailSender>().SendAsync(email, cancellationToken);
                        message.Payload = "{}";     // the code inside isn't kept once it's sent
                        break;
                    default:
                        logger.LogWarning("Unknown outbox message type {Type}", message.Type);
                        break;
                }
            }, error => Record(message, error), cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return due.Count;
    }

    // UC-81 and BR-C5, for every active connection. One bank failing doesn't stop the rest.
    public async Task RunDailyAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> ids;
        using (var scope = scopes.CreateScope())
        {
            ids = await scope.ServiceProvider.GetRequiredService<IBankConnectionRepository>().ListActiveIdsAsync(cancellationToken);
        }

        foreach (var id in ids)
        {
            using var scope = scopes.CreateScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<BankConnectionService>().RunDailyAsync(id, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Daily sync failed for connection {ConnectionId}", id);
            }
        }
    }

    private async Task RunAsync(string type, Func<Task> work, Action<string?> record, CancellationToken cancellationToken)
    {
        try
        {
            await work();
            record(null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Background message {Type} failed", type);
            record(exception.Message);
        }
    }

    private void Record(InboxMessage message, string? error)
    {
        message.Attempts++;
        message.LastError = error;
        message.ProcessedAt = error is null || message.Attempts >= MaxAttempts ? clock.Now : null;
        message.NextAttemptAt = clock.Now.Add(Backoff(message.Attempts));
    }

    private void Record(OutboxMessage message, string? error)
    {
        message.Attempts++;
        message.LastError = error;
        message.ProcessedAt = error is null || message.Attempts >= MaxAttempts ? clock.Now : null;
        message.NextAttemptAt = clock.Now.Add(Backoff(message.Attempts));
    }

    // 1, 2, 4, 8 ... minutes, at most an hour.
    private static TimeSpan Backoff(int attempts) => TimeSpan.FromMinutes(Math.Min(60, Math.Pow(2, attempts - 1)));
}

public sealed class MessageProcessorService(
    BankingJobRunner runner, IOptions<BackgroundJobOptions> options, TimeProvider time, ILogger<MessageProcessorService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(options.Value.PollSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await runner.ProcessInboxAsync(stoppingToken);
                await runner.ProcessOutboxAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Message processing failed");
            }

            await Task.Delay(interval, time, stoppingToken);
        }
    }
}

public sealed class DailySyncService(
    BankingJobRunner runner, IOptions<BackgroundJobOptions> options, Clock clock, TimeProvider time, ILogger<DailySyncService> logger)
    : BackgroundService
{
    private static readonly TimeZoneInfo Riyadh = TimeZoneInfo.FindSystemTimeZoneById("Asia/Riyadh");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var wait = UntilNextRun(clock.Now, options.Value.DailySyncHour);
            logger.LogInformation("Next daily bank sync in {Wait}", wait);
            await Task.Delay(wait, time, stoppingToken);

            await runner.RunDailyAsync(stoppingToken);
        }
    }

    public static TimeSpan UntilNextRun(DateTimeOffset now, int hour)
    {
        var local = TimeZoneInfo.ConvertTime(now, Riyadh);
        var next = new DateTimeOffset(local.Year, local.Month, local.Day, hour, 0, 0, local.Offset);
        if (next <= local)
        {
            next = next.AddDays(1);
        }

        return next - local;
    }
}
