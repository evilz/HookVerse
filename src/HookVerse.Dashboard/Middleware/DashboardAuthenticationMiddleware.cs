namespace HookVerse.Dashboard.Middleware;

/// <summary>
/// Simple authentication middleware for dashboard access control.
/// Checks for a configured API key or allows access in development mode.
/// </summary>
public class DashboardAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DashboardAuthenticationMiddleware> _logger;
    private readonly string? _apiKey;
    private readonly bool _requireAuth;

    public DashboardAuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<DashboardAuthenticationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
        _apiKey = configuration["Dashboard:ApiKey"];
        _requireAuth = configuration.GetValue<bool>("Dashboard:RequireAuthentication", true);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for static assets and SignalR hub
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (path.StartsWith("/_framework") || 
            path.StartsWith("/_content") || 
            path.StartsWith("/css") || 
            path.StartsWith("/js") || 
            path.StartsWith("/webhookhub") ||
            path.Contains(".css") ||
            path.Contains(".js") ||
            path.Contains(".wasm") ||
            path.Contains(".png") ||
            path.Contains(".ico"))
        {
            await _next(context);
            return;
        }

        // Check if authentication is required
        if (!_requireAuth)
        {
            _logger.LogDebug("Authentication disabled - allowing access");
            await _next(context);
            return;
        }

        // Check for API key in header or query string
        var providedKey = context.Request.Headers["X-Dashboard-ApiKey"].FirstOrDefault() 
                         ?? context.Request.Query["apikey"].FirstOrDefault();

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("No Dashboard API key configured - allowing access");
            await _next(context);
            return;
        }

        if (string.IsNullOrEmpty(providedKey))
        {
            _logger.LogWarning("Dashboard access denied - no API key provided");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Authentication required. Provide X-Dashboard-ApiKey header or ?apikey query parameter." });
            return;
        }

        if (providedKey != _apiKey)
        {
            _logger.LogWarning("Dashboard access denied - invalid API key");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        _logger.LogDebug("Dashboard authentication successful");
        await _next(context);
    }
}

/// <summary>
/// Extension methods for dashboard authentication middleware.
/// </summary>
public static class DashboardAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseDashboardAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DashboardAuthenticationMiddleware>();
    }
}
