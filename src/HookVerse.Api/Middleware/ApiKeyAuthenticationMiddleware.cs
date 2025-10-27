using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HookVerse.Core.Interfaces;

namespace HookVerse.Api.Middleware;

/// <summary>
/// Middleware for API key authentication
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
    private const string ApiKeyHeaderName = "X-API-Key";

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
    {
        // Skip authentication for health checks, Swagger, and Subscribers endpoint
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (path.StartsWith("/health") || 
            path.StartsWith("/swagger") || 
            path.StartsWith("/api-docs") ||
            path.StartsWith("/api/v1/subscribers"))
        {
            await _next(context);
            return;
        }

        // Check if API key is provided
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValue))
        {
            _logger.LogWarning("API key missing from request to {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API key is required" });
            return;
        }

        var apiKey = apiKeyValue.ToString();

        // Validate API key
        var validationResult = await apiKeyService.ValidateApiKeyAsync(apiKey, context.RequestAborted);
        
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Invalid API key attempt from {RemoteIp}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        // Store subscriber ID in context for downstream use
        // Note: TenantId and SubscriberId are the same concept in this system
        context.Items["SubscriberId"] = validationResult.TenantId;
        context.Items["TenantId"] = validationResult.TenantId;
        context.Items["ApiKeyId"] = validationResult.ApiKeyId;

        _logger.LogDebug("Authenticated request for subscriber {SubscriberId}", validationResult.TenantId);

        await _next(context);
    }
}
