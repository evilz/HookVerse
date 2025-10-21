# Content Security Policy and Security Headers

HookVerse implements comprehensive security headers to protect against common web vulnerabilities including XSS, clickjacking, and MIME-type sniffing attacks.

## Overview

The security headers middleware adds the following HTTP headers to all responses:

- **Content-Security-Policy (CSP)**: Prevents XSS attacks by controlling resource loading
- **X-Frame-Options**: Prevents clickjacking by controlling iframe embedding
- **X-Content-Type-Options**: Prevents MIME-type sniffing
- **X-XSS-Protection**: Legacy XSS protection for older browsers
- **Referrer-Policy**: Controls referrer information in requests
- **Permissions-Policy**: Controls browser features and APIs
- **Strict-Transport-Security (HSTS)**: Enforces HTTPS connections

## Configuration

### Application Settings

Configure security headers in `appsettings.json`:

```json
{
  "SecurityHeaders": {
    "EnableSecurityHeaders": true,
    "ContentSecurityPolicy": "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self'; frame-ancestors 'none';",
    "CspReportOnly": false,
    "CspReportUri": null,
    "XFrameOptions": "DENY",
    "XContentTypeOptions": "nosniff",
    "XXssProtection": "1; mode=block",
    "ReferrerPolicy": "strict-origin-when-cross-origin",
    "PermissionsPolicy": "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()",
    "HstsMaxAge": 31536000,
    "HstsIncludeSubDomains": true,
    "HstsPreload": false
  }
}
```

### Environment-Specific Configuration

#### Development (`appsettings.Development.json`)

```json
{
  "SecurityHeaders": {
    "CspReportOnly": true,
    "ContentSecurityPolicy": "default-src 'self' 'unsafe-inline' 'unsafe-eval'; img-src 'self' data: https: http:;",
    "HstsMaxAge": 0
  }
}
```

#### Production (`appsettings.Production.json`)

```json
{
  "SecurityHeaders": {
    "CspReportOnly": false,
    "CspReportUri": "https://api.hookverse.com/api/v1/csp-report",
    "ContentSecurityPolicy": "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self' https://api.hookverse.com; frame-ancestors 'none'; upgrade-insecure-requests;",
    "HstsMaxAge": 31536000,
    "HstsIncludeSubDomains": true,
    "HstsPreload": true
  }
}
```

## Service Registration

Register security headers in `Program.cs`:

```csharp
using HookVerse.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add security headers configuration
builder.Services.AddSecurityHeaders(builder.Configuration);

var app = builder.Build();

// Use security headers middleware (add early in pipeline)
app.UseSecurityHeaders();

// Other middleware...
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.Run();
```

## Content Security Policy (CSP)

### CSP Directives

| Directive | Purpose | Example |
|-----------|---------|---------|
| `default-src` | Default source for all directives | `'self'` |
| `script-src` | Valid sources for JavaScript | `'self' https://cdn.example.com` |
| `style-src` | Valid sources for CSS | `'self' 'unsafe-inline'` |
| `img-src` | Valid sources for images | `'self' data: https:` |
| `font-src` | Valid sources for fonts | `'self' data:` |
| `connect-src` | Valid sources for fetch, XHR, WebSocket | `'self' https://api.example.com` |
| `frame-ancestors` | Valid parents that may embed this page | `'none'` or `'self'` |
| `form-action` | Valid endpoints for form submissions | `'self'` |
| `base-uri` | Valid URLs for `<base>` element | `'self'` |
| `object-src` | Valid sources for `<object>`, `<embed>` | `'none'` |

### CSP Keywords

- `'none'`: Blocks all sources
- `'self'`: Same origin as document
- `'unsafe-inline'`: Allows inline scripts/styles (avoid if possible)
- `'unsafe-eval'`: Allows `eval()` and similar (avoid if possible)
- `'strict-dynamic'`: Allows scripts loaded by trusted scripts
- `data:`: Allows data: URIs
- `https:`: Allows any HTTPS source

### API-Only CSP Configuration

For API-only services (no HTML frontend):

```json
{
  "SecurityHeaders": {
    "ContentSecurityPolicy": "default-src 'none'; frame-ancestors 'none';"
  }
}
```

### SPA (Single Page Application) CSP

For SPAs with external CDNs:

```json
{
  "SecurityHeaders": {
    "ContentSecurityPolicy": "default-src 'self'; script-src 'self' https://cdn.jsdelivr.net; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; font-src 'self' data:; img-src 'self' data: https:; connect-src 'self' https://api.hookverse.com;"
  }
}
```

## CSP Report-Only Mode

Test CSP policies without breaking functionality:

```json
{
  "SecurityHeaders": {
    "CspReportOnly": true,
    "CspReportUri": "/api/v1/csp-report"
  }
}
```

Create endpoint to receive CSP violation reports:

```csharp
[ApiController]
[Route("api/v1/csp-report")]
public class CspReportController : ControllerBase
{
    private readonly ILogger<CspReportController> _logger;

    [HttpPost]
    [Consumes("application/csp-report")]
    public IActionResult ReportViolation([FromBody] CspReport report)
    {
        _logger.LogWarning(
            "CSP Violation: {DocumentUri}, Blocked: {BlockedUri}, Violated: {ViolatedDirective}",
            report.CspReport?.DocumentUri,
            report.CspReport?.BlockedUri,
            report.CspReport?.ViolatedDirective);

        return NoContent();
    }
}

public class CspReport
{
    [JsonPropertyName("csp-report")]
    public CspReportDetails? CspReport { get; set; }
}

public class CspReportDetails
{
    [JsonPropertyName("document-uri")]
    public string? DocumentUri { get; set; }

    [JsonPropertyName("blocked-uri")]
    public string? BlockedUri { get; set; }

    [JsonPropertyName("violated-directive")]
    public string? ViolatedDirective { get; set; }

    [JsonPropertyName("effective-directive")]
    public string? EffectiveDirective { get; set; }

    [JsonPropertyName("original-policy")]
    public string? OriginalPolicy { get; set; }
}
```

## Security Headers Explained

### Strict-Transport-Security (HSTS)

Forces browsers to use HTTPS for all requests:

```
Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
```

- `max-age`: Time in seconds to enforce HTTPS (1 year = 31536000)
- `includeSubDomains`: Apply to all subdomains
- `preload`: Eligible for browser preload lists

**HSTS Preload Submission**: https://hstspreload.org/

### X-Frame-Options

Prevents clickjacking:

```
X-Frame-Options: DENY
```

Options:
- `DENY`: Cannot be embedded in any frame
- `SAMEORIGIN`: Can only be embedded in same-origin pages
- `ALLOW-FROM uri`: Can be embedded in specified URI (deprecated)

### X-Content-Type-Options

Prevents MIME-type sniffing:

```
X-Content-Type-Options: nosniff
```

Browsers will not interpret files as a different MIME type than declared.

### Referrer-Policy

Controls referrer information:

```
Referrer-Policy: strict-origin-when-cross-origin
```

Options:
- `no-referrer`: Never send referrer
- `same-origin`: Send referrer for same-origin requests only
- `strict-origin`: Send origin for HTTPS→HTTPS
- `strict-origin-when-cross-origin`: Full URL for same-origin, origin for cross-origin

### Permissions-Policy

Controls browser features:

```
Permissions-Policy: accelerometer=(), camera=(), geolocation=(), microphone=()
```

Disables unnecessary browser APIs to reduce attack surface.

## Testing

### Browser DevTools

Check security headers in browser DevTools:

1. Open DevTools (F12)
2. Navigate to **Network** tab
3. Select a request
4. View **Response Headers**

### cURL

```bash
curl -I https://api.hookverse.com/api/v1/webhooks
```

Expected output:
```
HTTP/2 200
content-type: application/json
content-security-policy: default-src 'self'; ...
x-frame-options: DENY
x-content-type-options: nosniff
x-xss-protection: 1; mode=block
referrer-policy: strict-origin-when-cross-origin
permissions-policy: accelerometer=(), camera=(), ...
strict-transport-security: max-age=31536000; includeSubDomains
```

### Security Headers Scanner

Use online tools to scan headers:

- https://securityheaders.com/
- https://observatory.mozilla.org/

### Automated Testing

```csharp
[Fact]
public async Task Api_ShouldReturnSecurityHeaders()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/v1/webhooks");

    // Assert
    response.Headers.Should().ContainKey("Content-Security-Policy");
    response.Headers.Should().ContainKey("X-Frame-Options");
    response.Headers.Should().ContainKey("X-Content-Type-Options");
    response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");
}
```

## Best Practices

### 1. Start with Report-Only Mode

```json
{
  "SecurityHeaders": {
    "CspReportOnly": true,
    "CspReportUri": "/api/v1/csp-report"
  }
}
```

Monitor violations before enforcing.

### 2. Use Strict CSP

Avoid `'unsafe-inline'` and `'unsafe-eval'` when possible:

```json
{
  "ContentSecurityPolicy": "default-src 'self'; script-src 'self'; style-src 'self';"
}
```

### 3. Enable HSTS Preload

After testing, submit to HSTS preload list:

```json
{
  "HstsMaxAge": 31536000,
  "HstsIncludeSubDomains": true,
  "HstsPreload": true
}
```

Submit at: https://hstspreload.org/

### 4. Regularly Review CSP Violations

Set up monitoring for CSP violation reports:

```promql
# CSP violation rate
rate(hookverse_csp_violations_total[5m])

# CSP violations by directive
sum(rate(hookverse_csp_violations_total[5m])) by (violated_directive)
```

### 5. Test Across Browsers

CSP behavior varies across browsers. Test on:
- Chrome/Edge
- Firefox
- Safari

## Common Issues

### Issue: CSP blocking inline scripts

**Solution**: Use nonces or hashes for inline scripts:

```html
<!-- Using nonce -->
<script nonce="random-nonce-value">
  console.log('Allowed');
</script>
```

```json
{
  "ContentSecurityPolicy": "script-src 'self' 'nonce-random-nonce-value';"
}
```

### Issue: Images not loading from external sources

**Solution**: Add source to `img-src`:

```json
{
  "ContentSecurityPolicy": "img-src 'self' https://images.example.com;"
}
```

### Issue: WebSockets blocked by CSP

**Solution**: Add WebSocket URL to `connect-src`:

```json
{
  "ContentSecurityPolicy": "connect-src 'self' wss://ws.example.com;"
}
```

### Issue: Third-party fonts not loading

**Solution**: Add font sources to `font-src`:

```json
{
  "ContentSecurityPolicy": "font-src 'self' https://fonts.gstatic.com;"
}
```

## Security Compliance

These security headers help meet compliance requirements:

- **OWASP Top 10**: Protects against A03:2021 - Injection (XSS)
- **PCI DSS**: Requirement 6.5.7 (XSS prevention)
- **GDPR**: Security measures for data protection
- **HIPAA**: Technical safeguards for PHI

## Related Documentation

- [API Security](./api-security.md)
- [Authentication](../api/authentication.md)
- [HTTPS Configuration](../deployment/https.md)
- [Security Best Practices](./best-practices.md)
