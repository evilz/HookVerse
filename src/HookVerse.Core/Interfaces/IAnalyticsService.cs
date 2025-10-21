namespace HookVerse.Core.Interfaces;

/// <summary>
/// Service interface for analytics and reporting.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Gets dashboard analytics for a subscriber within a date range.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard analytics data.</returns>
    Task<DashboardAnalyticsDto> GetDashboardAnalyticsAsync(
        Guid subscriberId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Dashboard analytics data transfer object.
/// </summary>
public class DashboardAnalyticsDto
{
    /// <summary>
    /// Total number of webhooks created.
    /// </summary>
    public int TotalWebhooks { get; set; }

    /// <summary>
    /// Total number of successful deliveries.
    /// </summary>
    public int SuccessfulDeliveries { get; set; }

    /// <summary>
    /// Total number of failed deliveries.
    /// </summary>
    public int FailedDeliveries { get; set; }

    /// <summary>
    /// Overall success rate percentage (0-100).
    /// </summary>
    public decimal SuccessRate { get; set; }

    /// <summary>
    /// Average response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Top event types by volume.
    /// </summary>
    public List<EventTypeStatsDto> TopEventTypes { get; set; } = new();

    /// <summary>
    /// Daily trend data for the date range.
    /// </summary>
    public List<DailyTrendDto> DailyTrend { get; set; } = new();
}

/// <summary>
/// Event type statistics.
/// </summary>
public class EventTypeStatsDto
{
    /// <summary>
    /// Event type ID.
    /// </summary>
    public Guid EventTypeId { get; set; }

    /// <summary>
    /// Event type name.
    /// </summary>
    public string EventTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Total number of webhooks for this event type.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Success rate for this event type (0-100).
    /// </summary>
    public decimal SuccessRate { get; set; }
}

/// <summary>
/// Daily trend statistics.
/// </summary>
public class DailyTrendDto
{
    /// <summary>
    /// Date of the statistics.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Total webhooks created on this date.
    /// </summary>
    public int TotalWebhooks { get; set; }

    /// <summary>
    /// Successful deliveries on this date.
    /// </summary>
    public int SuccessfulDeliveries { get; set; }

    /// <summary>
    /// Failed deliveries on this date.
    /// </summary>
    public int FailedDeliveries { get; set; }

    /// <summary>
    /// Success rate for this date (0-100).
    /// </summary>
    public decimal SuccessRate { get; set; }
}
