using HookVerse.Core.Entities;
using HookVerse.Core.Enums;
using HookVerse.Core.Interfaces;

namespace HookVerse.Core.Services;

/// <summary>
/// Service for generating analytics and reporting data.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly IWebhookEventRepository _webhookEventRepository;

    public AnalyticsService(IWebhookEventRepository webhookEventRepository)
    {
        _webhookEventRepository = webhookEventRepository;
    }

    /// <inheritdoc />
    public async Task<DashboardAnalyticsDto> GetDashboardAnalyticsAsync(
        Guid subscriberId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {

        // Get all webhook events for the subscriber within the date range
        var (events, _) = await _webhookEventRepository.SearchAsync(
            subscriberId,
            startDate,
            endDate,
            eventTypeId: null,
            skip: 0,
            take: int.MaxValue, // Get all events for analytics
            cancellationToken);

        var eventsList = events.ToList();

        if (!eventsList.Any())
        {
            return new DashboardAnalyticsDto
            {
                TotalWebhooks = 0,
                SuccessfulDeliveries = 0,
                FailedDeliveries = 0,
                SuccessRate = 0,
                AverageResponseTimeMs = 0,
                TopEventTypes = new List<EventTypeStatsDto>(),
                DailyTrend = new List<DailyTrendDto>()
            };
        }

        // Calculate overall statistics
        var totalWebhooks = eventsList.Count;
        var successfulDeliveries = eventsList.Count(e => 
            e.DeliveryAttempts.Any(a => a.Status == DeliveryStatus.Delivered));
        var failedDeliveries = eventsList.Count(e =>
            e.DeliveryAttempts.Any() && e.DeliveryAttempts.All(a => a.Status == DeliveryStatus.Failed));
        var successRate = totalWebhooks > 0 
            ? (decimal)successfulDeliveries / totalWebhooks * 100 
            : 0;

        // Calculate average response time from successful attempts
        var successfulAttempts = eventsList
            .SelectMany(e => e.DeliveryAttempts)
            .Where(a => a.Status == DeliveryStatus.Delivered && a.DurationMs.HasValue)
            .ToList();

        var averageResponseTimeMs = successfulAttempts.Any()
            ? successfulAttempts.Average(a => a.DurationMs!.Value)
            : 0;

        // Calculate top event types
        var topEventTypes = eventsList
            .GroupBy(e => new { e.EventTypeId, e.EventType?.Name })
            .Select(g => new EventTypeStatsDto
            {
                EventTypeId = g.Key.EventTypeId,
                EventTypeName = g.Key.Name ?? "Unknown",
                Count = g.Count(),
                SuccessRate = g.Count() > 0
                    ? (decimal)g.Count(e => e.DeliveryAttempts.Any(a => a.Status == DeliveryStatus.Delivered)) / g.Count() * 100
                    : 0
            })
            .OrderByDescending(s => s.Count)
            .Take(10)
            .ToList();

        // Calculate daily trend
        var dailyGroups = eventsList
            .GroupBy(e => e.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .ToList();

        var dailyTrend = dailyGroups.Select(g => new DailyTrendDto
        {
            Date = g.Key,
            TotalWebhooks = g.Count(),
            SuccessfulDeliveries = g.Count(e => 
                e.DeliveryAttempts.Any(a => a.Status == DeliveryStatus.Delivered)),
            FailedDeliveries = g.Count(e =>
                e.DeliveryAttempts.Any() && e.DeliveryAttempts.All(a => a.Status == DeliveryStatus.Failed)),
            SuccessRate = g.Count() > 0
                ? (decimal)g.Count(e => e.DeliveryAttempts.Any(a => a.Status == DeliveryStatus.Delivered)) / g.Count() * 100
                : 0
        }).ToList();

        return new DashboardAnalyticsDto
        {
            TotalWebhooks = totalWebhooks,
            SuccessfulDeliveries = successfulDeliveries,
            FailedDeliveries = failedDeliveries,
            SuccessRate = successRate,
            AverageResponseTimeMs = averageResponseTimeMs,
            TopEventTypes = topEventTypes,
            DailyTrend = dailyTrend
        };
    }
}
