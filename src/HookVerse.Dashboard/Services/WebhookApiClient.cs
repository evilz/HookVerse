using HookVerse.Dashboard.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace HookVerse.Dashboard.Services;

/// <summary>
/// Implementation of webhook API client
/// </summary>
public class WebhookApiClient : IWebhookApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public WebhookApiClient(
        HttpClient httpClient,
        ILogger<WebhookApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<WebhookSearchResult> SearchWebhooksAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? eventTypeId = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (startDate.HasValue)
                queryParams.Add($"startDate={startDate.Value:yyyy-MM-ddTHH:mm:ss}");
            if (endDate.HasValue)
                queryParams.Add($"endDate={endDate.Value:yyyy-MM-ddTHH:mm:ss}");
            if (eventTypeId.HasValue)
                queryParams.Add($"eventTypeId={eventTypeId.Value}");
            if (!string.IsNullOrEmpty(status))
                queryParams.Add($"status={status}");

            var query = string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"/api/v1/webhooks/search?{query}", cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<WebhookSearchResult>(_jsonOptions, cancellationToken);
            return result ?? new WebhookSearchResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching webhooks");
            return new WebhookSearchResult();
        }
    }

    public async Task<WebhookDetailResponse?> GetWebhookDetailsAsync(
        Guid webhookId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/webhooks/{webhookId}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<WebhookDetailResponse>(_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook details for {WebhookId}", webhookId);
            return null;
        }
    }

    public async Task<DashboardAnalytics?> GetDashboardAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();

            if (startDate.HasValue)
                queryParams.Add($"startDate={startDate.Value:yyyy-MM-ddTHH:mm:ss}");
            if (endDate.HasValue)
                queryParams.Add($"endDate={endDate.Value:yyyy-MM-ddTHH:mm:ss}");

            var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await _httpClient.GetAsync($"/api/v1/analytics/dashboard{query}", cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<DashboardAnalytics>(_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard analytics");
            return null;
        }
    }

    public async Task<List<EventTypeInfo>> GetEventTypesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/event-types", cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PaginatedResult<EventTypeInfo>>(_jsonOptions, cancellationToken);
            return result?.Items ?? new List<EventTypeInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event types");
            return new List<EventTypeInfo>();
        }
    }

    private class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
    }
}
