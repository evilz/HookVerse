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

    /// <summary>
    /// Get all mock endpoints
    /// </summary>
    Task<List<MockEndpointResponse>> GetMockEndpointsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get mock endpoint by ID
    /// </summary>
    Task<MockEndpointResponse?> GetMockEndpointAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new mock endpoint
    /// </summary>
    Task<MockEndpointResponse> CreateMockEndpointAsync(
        CreateMockEndpointRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a mock endpoint
    /// </summary>
    Task<MockEndpointResponse> UpdateMockEndpointAsync(
        Guid id,
        UpdateMockEndpointRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a mock endpoint
    /// </summary>
    Task<bool> DeleteMockEndpointAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Trigger a test webhook to a mock endpoint
    /// </summary>
    Task<TriggerMockWebhookResponse?> TriggerMockWebhookAsync(
        Guid id,
        TriggerMockWebhookRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all GDPR requests for the subscriber
    /// </summary>
    Task<List<GdprRequestResponse>> GetGdprRequestsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a GDPR data export request
    /// </summary>
    Task<GdprRequestResponse> CreateGdprExportRequestAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a GDPR data deletion request
    /// </summary>
    Task<GdprRequestResponse> CreateGdprDeleteRequestAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a GDPR export file
    /// </summary>
    Task<(byte[] Data, string FileName)?> DownloadGdprExportAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
