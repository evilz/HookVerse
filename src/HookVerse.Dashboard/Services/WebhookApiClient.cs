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

    public async Task<List<MockEndpointResponse>> GetMockEndpointsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/mock-endpoints", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<List<MockEndpointResponse>>(_jsonOptions, cancellationToken);
            return result ?? new List<MockEndpointResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mock endpoints");
            return new List<MockEndpointResponse>();
        }
    }

    public async Task<MockEndpointResponse?> GetMockEndpointAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/mock-endpoints/{id}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MockEndpointResponse>(_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mock endpoint {Id}", id);
            return null;
        }
    }

    public async Task<MockEndpointResponse> CreateMockEndpointAsync(
        CreateMockEndpointRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/mock-endpoints", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<MockEndpointResponse>(_jsonOptions, cancellationToken);
            return result ?? throw new Exception("Failed to create mock endpoint");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating mock endpoint");
            throw;
        }
    }

    public async Task<MockEndpointResponse> UpdateMockEndpointAsync(
        Guid id,
        UpdateMockEndpointRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/v1/mock-endpoints/{id}", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<MockEndpointResponse>(_jsonOptions, cancellationToken);
            return result ?? throw new Exception("Failed to update mock endpoint");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating mock endpoint {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteMockEndpointAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/mock-endpoints/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting mock endpoint {Id}", id);
            return false;
        }
    }

    public async Task<TriggerMockWebhookResponse?> TriggerMockWebhookAsync(
        Guid id,
        TriggerMockWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/mock-endpoints/{id}/trigger", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<TriggerMockWebhookResponse>(_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering mock webhook for endpoint {Id}", id);
            return null;
        }
    }

    public async Task<List<GdprRequestResponse>> GetGdprRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/gdpr/requests", cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<GdprRequestResponse>>(_jsonOptions, cancellationToken)
                ?? new List<GdprRequestResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting GDPR requests");
            return new List<GdprRequestResponse>();
        }
    }

    public async Task<GdprRequestResponse> CreateGdprExportRequestAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/gdpr/export", new { }, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<GdprRequestResponse>(_jsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Failed to create GDPR export request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GDPR export request");
            throw;
        }
    }

    public async Task<GdprRequestResponse> CreateGdprDeleteRequestAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new { Confirmed = true };
            var response = await _httpClient.PostAsJsonAsync("/api/v1/gdpr/delete", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<GdprRequestResponse>(_jsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Failed to create GDPR delete request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GDPR delete request");
            throw;
        }
    }

    public async Task<(byte[] Data, string FileName)?> DownloadGdprExportAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/gdpr/export/{id}/download", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var fileName = "gdpr-export.json";

            // Try to get filename from Content-Disposition header
            if (response.Content.Headers.ContentDisposition?.FileName != null)
            {
                fileName = response.Content.Headers.ContentDisposition.FileName.Trim('"');
            }

            return (data, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading GDPR export {Id}", id);
            return null;
        }
    }

    private class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
    }
}
