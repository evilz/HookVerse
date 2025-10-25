using System.Diagnostics;

namespace HookVerse.Api.Middleware;

/// <summary>
/// Middleware that ensures each request has a correlation ID for distributed tracing.
/// If the request doesn't have a correlation ID header, one is generated.
/// The correlation ID is added to the response headers and logging scope.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get correlation ID from request header, or generate a new one
        var correlationId = GetOrCreateCorrelationId(context);

        // Add correlation ID to response headers for client tracking
        context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);

        // Add correlation ID to Activity (OpenTelemetry trace)
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetTag("correlation_id", correlationId);
            activity.SetBaggage("correlation_id", correlationId);
        }

        // Add correlation ID to logging scope
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? string.Empty
        }))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // Check if correlation ID exists in request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) 
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        // Use OpenTelemetry trace ID if available, otherwise generate new GUID
        var activity = Activity.Current;
        return activity?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    }
}

/// <summary>
/// Extension methods for registering CorrelationIdMiddleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds correlation ID middleware to the application pipeline.
    /// Should be called early in the pipeline to ensure all requests have correlation IDs.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
