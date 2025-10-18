using HookVerse.Core.Entities;
using HookVerse.Core.Enums;
using HookVerse.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HookVerse.Infrastructure.Services;

/// <summary>
/// Service for webhook delivery with retry logic and SSRF protection.
/// </summary>
public class DeliveryService : IDeliveryService
{
    private const int RequestTimeoutSeconds = 30;
    private const int MaxResponseBodyBytes = 10000; // 10KB

    private readonly HttpClient _httpClient;
    private readonly ISignatureService _signatureService;
    private readonly IDeliveryAttemptRepository _deliveryAttemptRepository;
    private readonly ILogger<DeliveryService> _logger;

    // Blocked IP ranges for SSRF protection
    private static readonly string[] BlockedIpRanges = new[]
    {
        "127.0.0.1",
        "::1",
        "localhost",
        "169.254.", // Link-local
        "10.",      // Private Class A
        "172.16.",  // Private Class B (16-31)
        "192.168.", // Private Class C
    };

    public DeliveryService(
        HttpClient httpClient,
        ISignatureService signatureService,
        IDeliveryAttemptRepository deliveryAttemptRepository,
        ILogger<DeliveryService> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
        _signatureService = signatureService;
        _deliveryAttemptRepository = deliveryAttemptRepository;
        _logger = logger;
    }

    public async Task<DeliveryAttempt> DeliverWebhookAsync(
        WebhookEvent webhookEvent, 
        Subscription subscription, 
        int attemptNumber, 
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        var attempt = new DeliveryAttempt
        {
            Id = Guid.NewGuid(),
            WebhookEventId = webhookEvent.Id,
            SubscriptionId = subscription.Id,
            AttemptNumber = attemptNumber,
            Status = DeliveryStatus.Delivering,
            StartedAt = startedAt,
            TraceId = webhookEvent.TraceId,
            RequestBody = webhookEvent.Payload,
            CreatedAt = startedAt,
            UpdatedAt = startedAt
        };

        try
        {
            // Validate endpoint URL (SSRF protection)
            if (!IsEndpointSafe(subscription.EndpointUrl))
            {
                attempt.Status = DeliveryStatus.Rejected;
                attempt.ErrorMessage = "Endpoint URL blocked by SSRF protection";
                _logger.LogWarning("Blocked delivery to unsafe endpoint: {EndpointUrl}", subscription.EndpointUrl);
                return await FinalizeAttempt(attempt, stopwatch, cancellationToken);
            }

            // Generate signature
            attempt.Signature = _signatureService.GenerateSignature(webhookEvent.Payload, subscription.Secret);

            // Build HTTP request
            var request = new HttpRequestMessage(HttpMethod.Post, subscription.EndpointUrl);
            request.Content = new StringContent(webhookEvent.Payload, Encoding.UTF8, "application/json");

            // Add standard webhook headers
            request.Headers.Add("X-Webhook-Signature", attempt.Signature);
            request.Headers.Add("X-Webhook-Id", webhookEvent.Id.ToString());
            request.Headers.Add("X-Webhook-Event-Type", webhookEvent.EventType?.Name ?? "unknown");
            request.Headers.Add("X-Webhook-Delivery-Id", attempt.Id.ToString());
            request.Headers.Add("X-Webhook-Attempt", attemptNumber.ToString());
            request.Headers.Add("X-Webhook-Timestamp", webhookEvent.CreatedAt.ToString("O"));
            request.Headers.Add("X-Trace-Id", webhookEvent.TraceId);

            // Add custom headers if configured
            if (!string.IsNullOrWhiteSpace(subscription.CustomHeaders))
            {
                var customHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(subscription.CustomHeaders);
                if (customHeaders != null)
                {
                    foreach (var header in customHeaders)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            // Add authentication based on subscription auth type
            AddAuthentication(request, subscription);

            // Capture request headers
            attempt.RequestHeaders = JsonSerializer.Serialize(
                request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
            );

            _logger.LogInformation("Delivering webhook {WebhookEventId} attempt {AttemptNumber} to {EndpointUrl}", 
                webhookEvent.Id, attemptNumber, subscription.EndpointUrl);

            // Send HTTP request
            var response = await _httpClient.SendAsync(request, cancellationToken);

            // Capture response
            attempt.ResponseStatus = (int)response.StatusCode;
            attempt.ResponseHeaders = JsonSerializer.Serialize(
                response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
            );

            // Read response body (truncated)
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (responseBody.Length > MaxResponseBodyBytes)
            {
                responseBody = responseBody.Substring(0, MaxResponseBodyBytes) + "... (truncated)";
            }
            attempt.ResponseBody = responseBody;

            // Determine status based on HTTP status code
            if (response.IsSuccessStatusCode)
            {
                attempt.Status = DeliveryStatus.Delivered;
                _logger.LogInformation("Successfully delivered webhook {WebhookEventId} attempt {AttemptNumber}", 
                    webhookEvent.Id, attemptNumber);
            }
            else
            {
                attempt.Status = DeliveryStatus.Failed;
                attempt.ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                _logger.LogWarning("Failed to deliver webhook {WebhookEventId} attempt {AttemptNumber}: {StatusCode}", 
                    webhookEvent.Id, attemptNumber, response.StatusCode);
            }
        }
        catch (TaskCanceledException ex)
        {
            attempt.Status = DeliveryStatus.Timeout;
            attempt.ErrorMessage = $"Request timeout after {RequestTimeoutSeconds} seconds: {ex.Message}";
            _logger.LogWarning(ex, "Webhook delivery timeout for {WebhookEventId} attempt {AttemptNumber}", 
                webhookEvent.Id, attemptNumber);
        }
        catch (HttpRequestException ex)
        {
            attempt.Status = DeliveryStatus.Failed;
            attempt.ErrorMessage = $"HTTP request failed: {ex.Message}";
            _logger.LogError(ex, "Webhook delivery HTTP error for {WebhookEventId} attempt {AttemptNumber}", 
                webhookEvent.Id, attemptNumber);
        }
        catch (BrokenCircuitException ex)
        {
            attempt.Status = DeliveryStatus.CircuitOpen;
            attempt.ErrorMessage = $"Circuit breaker open: {ex.Message}";
            _logger.LogWarning(ex, "Circuit breaker open for {WebhookEventId} attempt {AttemptNumber}", 
                webhookEvent.Id, attemptNumber);
        }
        catch (Exception ex)
        {
            attempt.Status = DeliveryStatus.Failed;
            attempt.ErrorMessage = $"Unexpected error: {ex.Message}";
            _logger.LogError(ex, "Unexpected error delivering webhook {WebhookEventId} attempt {AttemptNumber}", 
                webhookEvent.Id, attemptNumber);
        }

        return await FinalizeAttempt(attempt, stopwatch, cancellationToken);
    }

    public bool IsEndpointSafe(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        // Only allow HTTP/HTTPS
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        // Resolve hostname to IP address
        try
        {
            var hostEntry = Dns.GetHostEntry(uri.Host);
            foreach (var address in hostEntry.AddressList)
            {
                var ipString = address.ToString();
                
                // Check against blocked ranges
                foreach (var blocked in BlockedIpRanges)
                {
                    if (ipString.StartsWith(blocked, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                // Block all 172.16.x.x - 172.31.x.x
                if (ipString.StartsWith("172."))
                {
                    var parts = ipString.Split('.');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out var secondOctet))
                    {
                        if (secondOctet >= 16 && secondOctet <= 31)
                        {
                            return false;
                        }
                    }
                }
            }
        }
        catch
        {
            // DNS resolution failed - block it
            return false;
        }

        return true;
    }

    private void AddAuthentication(HttpRequestMessage request, Subscription subscription)
    {
        switch (subscription.AuthType)
        {
            case AuthType.None:
                break;

            case AuthType.BearerToken:
                if (!string.IsNullOrWhiteSpace(subscription.AuthConfig))
                {
                    var authConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(subscription.AuthConfig);
                    if (authConfig != null && authConfig.TryGetValue("token", out var token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                }
                break;

            case AuthType.BasicAuth:
                if (!string.IsNullOrWhiteSpace(subscription.AuthConfig))
                {
                    var authConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(subscription.AuthConfig);
                    if (authConfig != null && 
                        authConfig.TryGetValue("username", out var username) && 
                        authConfig.TryGetValue("password", out var password))
                    {
                        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                    }
                }
                break;

            case AuthType.CustomHeaders:
                // Custom headers already added from subscription.CustomHeaders
                break;
        }
    }

    private async Task<DeliveryAttempt> FinalizeAttempt(
        DeliveryAttempt attempt, 
        Stopwatch stopwatch, 
        CancellationToken cancellationToken)
    {
        stopwatch.Stop();
        attempt.CompletedAt = DateTime.UtcNow;
        attempt.DurationMs = (int)stopwatch.ElapsedMilliseconds;
        attempt.UpdatedAt = DateTime.UtcNow;

        await _deliveryAttemptRepository.AddAsync(attempt, cancellationToken);
        await _deliveryAttemptRepository.SaveChangesAsync(cancellationToken);

        return attempt;
    }
}
