using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HookVerse.Api.Middleware;

/// <summary>
/// Configuration options for security headers
/// </summary>
public class SecurityHeadersOptions
{
    /// <summary>
    /// Content Security Policy directives
    /// </summary>
    public string ContentSecurityPolicy { get; set; } = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none';";

    /// <summary>
    /// Whether to enable CSP report-only mode (for testing)
    /// </summary>
    public bool CspReportOnly { get; set; } = false;

    /// <summary>
    /// URI to send CSP violation reports to
    /// </summary>
    public string? CspReportUri { get; set; }

    /// <summary>
    /// X-Frame-Options value (DENY, SAMEORIGIN, or ALLOW-FROM uri)
    /// </summary>
    public string XFrameOptions { get; set; } = "DENY";

    /// <summary>
    /// X-Content-Type-Options value
    /// </summary>
    public string XContentTypeOptions { get; set; } = "nosniff";

    /// <summary>
    /// X-XSS-Protection value
    /// </summary>
    public string XXssProtection { get; set; } = "1; mode=block";

    /// <summary>
    /// Referrer-Policy value
    /// </summary>
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// Permissions-Policy (formerly Feature-Policy)
    /// </summary>
    public string PermissionsPolicy { get; set; } = 
        "accelerometer=(), " +
        "camera=(), " +
        "geolocation=(), " +
        "gyroscope=(), " +
        "magnetometer=(), " +
        "microphone=(), " +
        "payment=(), " +
        "usb=()";

    /// <summary>
    /// Strict-Transport-Security max-age in seconds (default: 1 year)
    /// </summary>
    public int HstsMaxAge { get; set; } = 31536000;

    /// <summary>
    /// Whether to include subdomains in HSTS
    /// </summary>
    public bool HstsIncludeSubDomains { get; set; } = true;

    /// <summary>
    /// Whether to enable HSTS preload
    /// </summary>
    public bool HstsPreload { get; set; } = false;

    /// <summary>
    /// Whether to add security headers to API responses
    /// </summary>
    public bool EnableSecurityHeaders { get; set; } = true;
}

/// <summary>
/// Middleware to add security headers to HTTP responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IOptions<SecurityHeadersOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_options.EnableSecurityHeaders)
        {
            AddSecurityHeaders(context);
        }

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Content Security Policy
        if (!string.IsNullOrWhiteSpace(_options.ContentSecurityPolicy))
        {
            var cspHeaderName = _options.CspReportOnly 
                ? "Content-Security-Policy-Report-Only" 
                : "Content-Security-Policy";

            var cspValue = _options.ContentSecurityPolicy;

            // Add report-uri if configured
            if (!string.IsNullOrWhiteSpace(_options.CspReportUri))
            {
                cspValue += $" report-uri {_options.CspReportUri};";
            }

            headers[cspHeaderName] = cspValue;
        }

        // X-Frame-Options
        if (!string.IsNullOrWhiteSpace(_options.XFrameOptions))
        {
            headers["X-Frame-Options"] = _options.XFrameOptions;
        }

        // X-Content-Type-Options
        if (!string.IsNullOrWhiteSpace(_options.XContentTypeOptions))
        {
            headers["X-Content-Type-Options"] = _options.XContentTypeOptions;
        }

        // X-XSS-Protection
        if (!string.IsNullOrWhiteSpace(_options.XXssProtection))
        {
            headers["X-XSS-Protection"] = _options.XXssProtection;
        }

        // Referrer-Policy
        if (!string.IsNullOrWhiteSpace(_options.ReferrerPolicy))
        {
            headers["Referrer-Policy"] = _options.ReferrerPolicy;
        }

        // Permissions-Policy
        if (!string.IsNullOrWhiteSpace(_options.PermissionsPolicy))
        {
            headers["Permissions-Policy"] = _options.PermissionsPolicy;
        }

        // Strict-Transport-Security (only on HTTPS)
        if (context.Request.IsHttps)
        {
            var hstsValue = $"max-age={_options.HstsMaxAge}";
            
            if (_options.HstsIncludeSubDomains)
            {
                hstsValue += "; includeSubDomains";
            }
            
            if (_options.HstsPreload)
            {
                hstsValue += "; preload";
            }

            headers["Strict-Transport-Security"] = hstsValue;
        }

        // Remove server header for security
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
        headers.Remove("X-AspNet-Version");
        headers.Remove("X-AspNetMvc-Version");

        _logger.LogDebug("Security headers added to response for {Path}", context.Request.Path);
    }
}

/// <summary>
/// Extension methods for registering security headers middleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds security headers middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }

    /// <summary>
    /// Adds security headers services and configuration
    /// </summary>
    public static IServiceCollection AddSecurityHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SecurityHeadersOptions>(
            configuration.GetSection("SecurityHeaders"));

        return services;
    }

    /// <summary>
    /// Adds security headers services with custom configuration
    /// </summary>
    public static IServiceCollection AddSecurityHeaders(
        this IServiceCollection services,
        Action<SecurityHeadersOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return services;
    }
}
