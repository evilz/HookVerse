# Dashboard Authentication Guide

## Overview

The HookVerse Dashboard includes a simple authentication middleware that controls access to the portal.

## Configuration

### Development Mode (Default)

In development, authentication is **disabled** by default for easier testing:

```json
// appsettings.Development.json
{
  "Dashboard": {
    "RequireAuthentication": false
  }
}
```

### Production Mode

For production deployments, enable authentication and set a secure API key:

```json
// appsettings.json or appsettings.Production.json
{
  "Dashboard": {
    "RequireAuthentication": true,
    "ApiKey": "your-secure-random-api-key-here"
  }
}
```

**Important**: Change the default API key before deploying to production!

## Accessing the Dashboard

### Without Authentication (Development)

Simply navigate to the dashboard URL:

```
https://localhost:7002/
https://localhost:7002/dashboard
https://localhost:7002/webhooks
```

### With Authentication (Production)

Provide the API key in one of two ways:

#### Option 1: HTTP Header

```bash
curl -H "X-Dashboard-ApiKey: your-api-key" https://yourdomain.com/dashboard
```

#### Option 2: Query Parameter

```
https://yourdomain.com/dashboard?apikey=your-api-key
```

For browser access, use the query parameter method:

```
https://yourdomain.com/?apikey=your-api-key
```

The API key will be required for all dashboard pages but not for:
- Static assets (CSS, JS, images)
- SignalR WebSocket connections
- Framework files

## Environment Variables

You can also set the API key via environment variables:

```bash
# PowerShell
$env:Dashboard__ApiKey="your-secure-key"
$env:Dashboard__RequireAuthentication="true"

# Bash
export Dashboard__ApiKey="your-secure-key"
export Dashboard__RequireAuthentication="true"
```

## Security Recommendations

1. **Use Strong Keys**: Generate random API keys with at least 32 characters
2. **HTTPS Only**: Always use HTTPS in production
3. **Rotate Keys**: Change API keys periodically
4. **Limit Access**: Only share the API key with authorized personnel
5. **Environment Specific**: Use different keys for staging and production

## Generating Secure API Keys

### PowerShell

```powershell
# Generate a secure random API key
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

### Bash/Linux

```bash
# Generate a secure random API key
openssl rand -base64 32
```

### .NET

```csharp
// Generate a secure random API key
var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
```

## Future Enhancements

For more advanced authentication scenarios, consider:

- **ASP.NET Core Identity**: Full user management with roles
- **OAuth 2.0/OpenID Connect**: Enterprise SSO integration
- **JWT Tokens**: Token-based authentication
- **Azure AD**: Microsoft identity platform integration

The current implementation provides a simple, effective authentication layer suitable for small to medium deployments.
