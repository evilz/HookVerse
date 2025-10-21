using HookVerse.Core.Interfaces;
using HookVerse.Infrastructure.Metrics;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace HookVerse.Worker.Workers;

/// <summary>
/// Background worker that performs automatic data retention and purging based on ExpiresAt timestamps.
/// </summary>
public class DataRetentionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly GdprMetrics _gdprMetrics;
    private readonly ILogger<DataRetentionWorker> _logger;
    private readonly IConfiguration _configuration;
    private TimeSpan _scheduleInterval;

    public DataRetentionWorker(
        IServiceProvider serviceProvider,
        GdprMetrics gdprMetrics,
        ILogger<DataRetentionWorker> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _gdprMetrics = gdprMetrics;
        _logger = logger;
        _configuration = configuration;

        // Default: run daily at midnight
        _scheduleInterval = TimeSpan.FromHours(24);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data Retention Worker starting");

        // Wait until midnight to start
        await WaitUntilMidnightAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformRetentionAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Data Retention Worker");
            }

            // Wait for next scheduled run (default: 24 hours)
            await Task.Delay(_scheduleInterval, stoppingToken);
        }

        _logger.LogInformation("Data Retention Worker stopping");
    }

    private async Task WaitUntilMidnightAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var nextMidnight = now.Date.AddDays(1);
        var delay = nextMidnight - now;

        _logger.LogInformation(
            "Data Retention Worker will start first run at {NextMidnight} UTC (in {Delay})",
            nextMidnight, delay);

        await Task.Delay(delay, cancellationToken);
    }

    private async Task PerformRetentionAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting data retention operation at {Timestamp}", DateTime.UtcNow);

        var stopwatch = Stopwatch.StartNew();

        using var scope = _serviceProvider.CreateScope();
        var webhookEventRepository = scope.ServiceProvider.GetRequiredService<IWebhookEventRepository>();

        try
        {
            // Get expired webhook events
            var cutoffDate = DateTime.UtcNow;
            var expiredEvents = await webhookEventRepository.GetExpiredEventsAsync(cutoffDate, cancellationToken);

            var expiredCount = expiredEvents.Count();

            if (expiredCount == 0)
            {
                _logger.LogInformation("No expired webhook events found");
                return;
            }

            _logger.LogInformation(
                "Found {Count} expired webhook events to purge",
                expiredCount);

            // Purge payloads and metadata while preserving audit trail
            int purgedCount = 0;
            foreach (var webhookEvent in expiredEvents)
            {
                try
                {
                    // Purge payload and metadata (keep event record for audit)
                    webhookEvent.Payload = "{\"purged\": true, \"purgedAt\": \"" + DateTime.UtcNow.ToString("o") + "\"}";
                    webhookEvent.Metadata = "{\"purged\": true}";
                    
                    await webhookEventRepository.UpdateAsync(webhookEvent, cancellationToken);
                    purgedCount++;

                    if (purgedCount % 100 == 0)
                    {
                        _logger.LogInformation("Purged {Count} of {Total} webhook events", purgedCount, expiredCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Error purging webhook event {WebhookEventId}",
                        webhookEvent.Id);
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Data retention operation completed. Purged {PurgedCount} webhook events in {Duration}s",
                purgedCount,
                stopwatch.Elapsed.TotalSeconds);

            _gdprMetrics.RecordRetentionPurge(purgedCount, stopwatch.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data retention operation");
            throw;
        }
    }
}
