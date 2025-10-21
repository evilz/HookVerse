using System.Diagnostics.Metrics;

namespace HookVerse.Infrastructure.Metrics;

/// <summary>
/// OpenTelemetry metrics for GDPR operations
/// </summary>
public class GdprMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _requestsTotal;
    private readonly Histogram<double> _processingDurationSeconds;
    private readonly Histogram<long> _exportFileSizeBytes;
    private readonly Gauge<int> _pendingRequests;
    private readonly Counter<long> _retentionPurgedEventsTotal;
    private readonly Histogram<double> _retentionDurationSeconds;
    private int _currentPendingRequests;

    public GdprMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("HookVerse.Gdpr");

        _requestsTotal = _meter.CreateCounter<long>(
            "gdpr_requests_total",
            description: "Total number of GDPR requests");

        _processingDurationSeconds = _meter.CreateHistogram<double>(
            "gdpr_processing_duration_seconds",
            unit: "s",
            description: "Duration of GDPR request processing");

        _exportFileSizeBytes = _meter.CreateHistogram<long>(
            "gdpr_export_file_size_bytes",
            unit: "bytes",
            description: "Size of GDPR export files");

        _pendingRequests = _meter.CreateGauge<int>(
            "gdpr_pending_requests",
            description: "Current number of pending GDPR requests");

        _retentionPurgedEventsTotal = _meter.CreateCounter<long>(
            "data_retention_purged_events_total",
            description: "Total number of webhook events purged by retention policy");

        _retentionDurationSeconds = _meter.CreateHistogram<double>(
            "data_retention_duration_seconds",
            unit: "s",
            description: "Duration of data retention operations");
    }

    /// <summary>
    /// Record a GDPR request creation
    /// </summary>
    public void RecordRequestCreated(string requestType)
    {
        _requestsTotal.Add(1, new KeyValuePair<string, object?>("request_type", requestType), new KeyValuePair<string, object?>("status", "created"));
        Interlocked.Increment(ref _currentPendingRequests);
    }

    /// <summary>
    /// Record GDPR request processing completion
    /// </summary>
    public void RecordRequestCompleted(string requestType, string status, double durationSeconds, long? fileSizeBytes = null)
    {
        _requestsTotal.Add(1, new KeyValuePair<string, object?>("request_type", requestType), new KeyValuePair<string, object?>("status", status));
        _processingDurationSeconds.Record(durationSeconds, new KeyValuePair<string, object?>("request_type", requestType), new KeyValuePair<string, object?>("status", status));
        
        if (fileSizeBytes.HasValue && requestType == "Export")
        {
            _exportFileSizeBytes.Record(fileSizeBytes.Value);
        }

        Interlocked.Decrement(ref _currentPendingRequests);
    }

    /// <summary>
    /// Record data retention purge operation
    /// </summary>
    public void RecordRetentionPurge(int purgedCount, double durationSeconds)
    {
        _retentionPurgedEventsTotal.Add(purgedCount, new KeyValuePair<string, object?>("date", DateTime.UtcNow.ToString("yyyy-MM-dd")));
        _retentionDurationSeconds.Record(durationSeconds);
    }

    /// <summary>
    /// Update pending requests gauge
    /// </summary>
    public void UpdatePendingRequestsGauge(int pendingCount)
    {
        _currentPendingRequests = pendingCount;
    }

    /// <summary>
    /// Get current pending requests count for gauge observation
    /// </summary>
    public int GetPendingRequestsCount() => _currentPendingRequests;
}
