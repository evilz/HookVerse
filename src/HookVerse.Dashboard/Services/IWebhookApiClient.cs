using HookVerse.Dashboard.Models;

namespace HookVerse.Dashboard.Services;

/// <summary>
/// Interface for webhook API client
/// </summary>
public interface IWebhookApiClient
{
    /// <summary>
    /// Search webhooks with filtering and pagination
    /// </summary>
    Task<WebhookSearchResult> SearchWebhooksAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? eventTypeId = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get webhook details by ID
    /// </summary>
    Task<WebhookDetailResponse?> GetWebhookDetailsAsync(
        Guid webhookId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get dashboard analytics
    /// </summary>
    Task<DashboardAnalytics?> GetDashboardAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get event types for subscriber
    /// </summary>
    Task<List<EventTypeInfo>> GetEventTypesAsync(
        CancellationToken cancellationToken = default);
}
