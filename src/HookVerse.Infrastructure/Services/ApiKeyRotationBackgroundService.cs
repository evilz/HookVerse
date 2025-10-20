using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HookVerse.Infrastructure.Services;

/// <summary>
/// Configuration options for the API key rotation background service
/// </summary>
public class ApiKeyRotationBackgroundServiceOptions
{
    /// <summary>
    /// Interval between rotation checks (default: 1 hour)
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Whether to run rotation check on startup
    /// </summary>
    public bool RunOnStartup { get; set; } = true;

    /// <summary>
    /// Whether the background service is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Background service that periodically checks and rotates expired API keys
/// </summary>
public class ApiKeyRotationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApiKeyRotationBackgroundService> _logger;
    private readonly ApiKeyRotationBackgroundServiceOptions _options;

    public ApiKeyRotationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ApiKeyRotationBackgroundService> logger,
        IOptions<ApiKeyRotationBackgroundServiceOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("API key rotation background service is disabled");
            return;
        }

        _logger.LogInformation(
            "API key rotation background service started. Check interval: {Interval}",
            _options.CheckInterval);

        // Run on startup if configured
        if (_options.RunOnStartup)
        {
            await RunRotationCheckAsync(stoppingToken);
        }

        // Run periodically
        using var timer = new PeriodicTimer(_options.CheckInterval);

        try
        {
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunRotationCheckAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("API key rotation background service is stopping");
        }
    }

    private async Task RunRotationCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Starting API key rotation check");

            using var scope = _serviceProvider.CreateScope();
            var rotationService = scope.ServiceProvider.GetRequiredService<ApiKeyRotationService>();

            // Auto-rotate expired keys
            var rotatedCount = await rotationService.AutoRotateExpiredKeysAsync(cancellationToken);
            
            if (rotatedCount > 0)
            {
                _logger.LogInformation("Auto-rotated {Count} expired API keys", rotatedCount);
            }

            // Revoke keys that have exceeded grace period
            var revokedCount = await rotationService.RevokeExpiredGracePeriodKeysAsync(cancellationToken);
            
            if (revokedCount > 0)
            {
                _logger.LogInformation("Revoked {Count} API keys after grace period expiration", revokedCount);
            }

            _logger.LogDebug("API key rotation check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during API key rotation check");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("API key rotation background service is stopping");
        return base.StopAsync(cancellationToken);
    }
}
