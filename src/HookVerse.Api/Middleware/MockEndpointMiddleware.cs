using HookVerse.Core.Interfaces;
using System.Text;

namespace HookVerse.Api.Middleware;

/// <summary>
/// Middleware that handles requests to mock webhook endpoints.
/// </summary>
public class MockEndpointMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MockEndpointMiddleware> _logger;

    public MockEndpointMiddleware(RequestDelegate next, ILogger<MockEndpointMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IMockEndpointService mockEndpointService)
    {
        // Check if the request is for a mock endpoint (starts with /mock/)
        if (!context.Request.Path.StartsWithSegments("/mock"))
        {
            await _next(context);
            return;
        }

        try
        {
            // Extract request details
            var urlPath = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method;
            var queryString = context.Request.QueryString.Value;
            
            // Extract headers
            var headers = new Dictionary<string, string>();
            foreach (var header in context.Request.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value!);
            }

            // Read body
            string? body = null;
            if (context.Request.ContentLength > 0)
            {
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
                body = await reader.ReadToEndAsync();
            }

            var contentType = context.Request.ContentType;
            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();

            // Handle the mock request
            var (status, responseBody, responseContentType, responseHeaders, delayMs) =
                await mockEndpointService.HandleMockRequestAsync(
                    urlPath,
                    method,
                    queryString,
                    headers,
                    body,
                    contentType,
                    clientIp,
                    userAgent,
                    context.RequestAborted);

            // Apply configured delay
            if (delayMs > 0)
            {
                await Task.Delay(delayMs, context.RequestAborted);
            }

            // Set response status and content type
            context.Response.StatusCode = status;
            context.Response.ContentType = responseContentType;

            // Add custom response headers
            if (responseHeaders != null)
            {
                foreach (var header in responseHeaders)
                {
                    context.Response.Headers[header.Key] = header.Value;
                }
            }

            // Write response body
            if (!string.IsNullOrEmpty(responseBody))
            {
                await context.Response.WriteAsync(responseBody, context.RequestAborted);
            }

            _logger.LogInformation(
                "Mock endpoint request handled: {Method} {Path} -> {Status}",
                method, urlPath, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling mock endpoint request: {Path}", context.Request.Path);
            
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                "{\"error\":\"Internal server error while processing mock endpoint request\"}",
                context.RequestAborted);
        }
    }
}
