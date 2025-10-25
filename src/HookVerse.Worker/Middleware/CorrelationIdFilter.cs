using System.Diagnostics;
using MassTransit;

namespace HookVerse.Worker.Middleware;

/// <summary>
/// MassTransit filter that adds correlation ID tracking to message processing.
/// Extracts or generates correlation IDs for distributed tracing across message handlers.
/// </summary>
public class CorrelationIdFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    private readonly ILogger<CorrelationIdFilter<T>> _logger;

    public CorrelationIdFilter(ILogger<CorrelationIdFilter<T>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        // Get correlation ID from message headers, or use ConversationId, or generate new one
        var correlationId = context.Headers.Get<string>("X-Correlation-ID")
            ?? context.ConversationId?.ToString("N")
            ?? Guid.NewGuid().ToString("N");

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
            ["ConversationId"] = context.ConversationId?.ToString() ?? string.Empty,
            ["MessageId"] = context.MessageId?.ToString() ?? string.Empty,
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? string.Empty
        }))
        {
            _logger.LogDebug("Processing message with correlation ID: {CorrelationId}", correlationId);
            await next.Send(context);
        }
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("correlationId");
    }
}
