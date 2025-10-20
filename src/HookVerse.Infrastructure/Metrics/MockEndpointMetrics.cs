using System.Diagnostics.Metrics;

namespace HookVerse.Infrastructure.Metrics;

/// <summary>
/// OpenTelemetry metrics for mock endpoint usage tracking.
/// </summary>
public class MockEndpointMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _requestsCounter;
    private readonly Histogram<double> _responseTimeHistogram;
    private readonly ObservableGauge<int> _activeEndpointsGauge;

    private Func<int>? _activeEndpointsProvider;

    public MockEndpointMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("HookVerse.MockEndpoints");

        _requestsCounter = _meter.CreateCounter<long>(
            name: "mock_endpoint_requests_total",
            unit: "requests",
            description: "Total number of requests to mock endpoints");

        _responseTimeHistogram = _meter.CreateHistogram<double>(
            name: "mock_endpoint_response_time",
            unit: "ms",
            description: "Response time for mock endpoint requests");

        _activeEndpointsGauge = _meter.CreateObservableGauge<int>(
            name: "mock_endpoint_active_count",
            observeValue: () => _activeEndpointsProvider?.Invoke() ?? 0,
            unit: "endpoints",
            description: "Number of active mock endpoints");
    }

    /// <summary>
    /// Record a request to a mock endpoint.
    /// </summary>
    /// <param name="endpointId">The mock endpoint ID</param>
    /// <param name="method">HTTP method</param>
    /// <param name="statusCode">Response status code</param>
    public void RecordRequest(Guid endpointId, string method, int statusCode)
    {
        _requestsCounter.Add(1, new KeyValuePair<string, object?>("endpoint_id", endpointId.ToString()),
                                 new KeyValuePair<string, object?>("method", method),
                                 new KeyValuePair<string, object?>("status_code", statusCode));
    }

    /// <summary>
    /// Record response time for a mock endpoint request.
    /// </summary>
    /// <param name="endpointId">The mock endpoint ID</param>
    /// <param name="durationMs">Duration in milliseconds</param>
    public void RecordResponseTime(Guid endpointId, double durationMs)
    {
        _responseTimeHistogram.Record(durationMs, new KeyValuePair<string, object?>("endpoint_id", endpointId.ToString()));
    }

    /// <summary>
    /// Set the provider function for active endpoints count.
    /// </summary>
    /// <param name="provider">Function that returns the current count of active endpoints</param>
    public void SetActiveEndpointsProvider(Func<int> provider)
    {
        _activeEndpointsProvider = provider;
    }
}
