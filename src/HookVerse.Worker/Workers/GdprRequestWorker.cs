using HookVerse.Core.Interfaces;
using HookVerse.Core.ValueObjects;
using HookVerse.Infrastructure.Metrics;
using System.Diagnostics;

namespace HookVerse.Worker.Workers;

/// <summary>
/// Background worker that processes pending GDPR requests.
/// </summary>
public class GdprRequestWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly GdprMetrics _gdprMetrics;
    private readonly ILogger<GdprRequestWorker> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(1);

    public GdprRequestWorker(
        IServiceProvider serviceProvider,
        GdprMetrics gdprMetrics,
        ILogger<GdprRequestWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _gdprMetrics = gdprMetrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GDPR Request Worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingRequestsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GDPR Request Worker");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("GDPR Request Worker stopping");
    }

    private async Task ProcessPendingRequestsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var gdprRequestRepository = scope.ServiceProvider.GetRequiredService<IGdprRequestRepository>();
        var gdprService = scope.ServiceProvider.GetRequiredService<IGdprService>();

        var pendingRequests = await gdprRequestRepository.GetPendingRequestsAsync(cancellationToken);

        _gdprMetrics.UpdatePendingRequestsGauge(pendingRequests.Count());

        foreach (var request in pendingRequests)
        {
            var stopwatch = Stopwatch.StartNew();
            var status = "Completed";
            long? fileSizeBytes = null;

            try
            {
                _logger.LogInformation(
                    "Processing GDPR request {RequestId} of type {RequestType} for subscriber {SubscriberId}",
                    request.Id, request.RequestType, request.SubscriberId);

                if (request.RequestType == GdprRequestType.Export)
                {
                    await gdprService.ProcessExportRequestAsync(request.Id, cancellationToken);
                    
                    // Get updated request to retrieve file size
                    var updatedRequest = await gdprRequestRepository.GetByIdAsync(request.Id, cancellationToken);
                    fileSizeBytes = updatedRequest?.ExportFileSizeBytes;
                }
                else if (request.RequestType == GdprRequestType.Delete)
                {
                    await gdprService.ProcessDeleteRequestAsync(request.Id, cancellationToken);
                }

                _logger.LogInformation(
                    "Successfully processed GDPR request {RequestId}",
                    request.Id);
            }
            catch (Exception ex)
            {
                status = "Failed";
                _logger.LogError(ex, 
                    "Error processing GDPR request {RequestId}",
                    request.Id);
            }
            finally
            {
                stopwatch.Stop();
                _gdprMetrics.RecordRequestCompleted(
                    request.RequestType.ToString(),
                    status,
                    stopwatch.Elapsed.TotalSeconds,
                    fileSizeBytes);
            }
        }
    }
}
