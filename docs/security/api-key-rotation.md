# API Key Rotation

HookVerse implements automatic API key rotation with graceful transition periods to enhance security while minimizing disruption to client applications.

## Overview

API key rotation is a security best practice that involves:

1. **Periodic Rotation**: Automatically rotating keys after a configurable lifetime (default: 90 days)
2. **Grace Period**: Keeping old keys valid during a transition period (default: 7 days)
3. **Automatic Revocation**: Revoking old keys after the grace period expires
4. **Proactive Warnings**: Alerting tenants about upcoming key expirations

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│           API Key Rotation Service                      │
│                                                         │
│  ┌─────────────────┐    ┌──────────────────┐          │
│  │  Rotation Check │───▶│  Auto-Rotate     │          │
│  │  (Hourly)       │    │  Expired Keys    │          │
│  └─────────────────┘    └──────────────────┘          │
│           │                      │                      │
│           │                      ▼                      │
│           │             ┌──────────────────┐           │
│           └────────────▶│  Revoke Keys     │           │
│                         │  After Grace     │           │
│                         └──────────────────┘           │
└─────────────────────────────────────────────────────────┘
```

## Configuration

### Application Settings

Configure API key rotation in `appsettings.json`:

```json
{
  "ApiKeyRotation": {
    "MaxKeyLifetime": "90.00:00:00",           // 90 days
    "RotationGracePeriod": "7.00:00:00",       // 7 days
    "ExpirationWarningPeriod": "14.00:00:00",  // 14 days
    "EnableAutomaticRotation": true,
    "AutoRevokeAfterGracePeriod": true
  },
  "ApiKeyRotationBackgroundService": {
    "CheckInterval": "01:00:00",               // 1 hour
    "RunOnStartup": true,
    "Enabled": true
  }
}
```

### Configuration Options

#### ApiKeyRotationOptions

| Option | Default | Description |
|--------|---------|-------------|
| `MaxKeyLifetime` | 90 days | Maximum lifetime of an API key before rotation |
| `RotationGracePeriod` | 7 days | Period where both old and new keys are valid |
| `ExpirationWarningPeriod` | 14 days | How far in advance to warn about expiration |
| `EnableAutomaticRotation` | true | Whether to automatically rotate expired keys |
| `AutoRevokeAfterGracePeriod` | true | Whether to automatically revoke old keys |

#### ApiKeyRotationBackgroundServiceOptions

| Option | Default | Description |
|--------|---------|-------------|
| `CheckInterval` | 1 hour | Interval between rotation checks |
| `RunOnStartup` | true | Whether to run rotation check on service startup |
| `Enabled` | true | Whether the background service is enabled |

## Usage

### Service Registration

Register the API key rotation services in `Program.cs`:

```csharp
// Configure rotation options
builder.Services.Configure<ApiKeyRotationOptions>(
    builder.Configuration.GetSection("ApiKeyRotation"));

builder.Services.Configure<ApiKeyRotationBackgroundServiceOptions>(
    builder.Configuration.GetSection("ApiKeyRotationBackgroundService"));

// Register services
builder.Services.AddScoped<ApiKeyRotationService>();
builder.Services.AddHostedService<ApiKeyRotationBackgroundService>();
```

### Manual Key Rotation

#### Via API

**Rotate an API Key**:
```http
POST /api/v1/apikeyrotation/{apiKeyId}/rotate
```

Response:
```json
{
  "success": true,
  "newApiKey": "abc123...xyz789",
  "newKeyId": "550e8400-e29b-41d4-a716-446655440002",
  "oldKeyId": "550e8400-e29b-41d4-a716-446655440001",
  "gracePeriodEndsAt": "2025-10-27T12:00:00Z",
  "message": "API key rotated successfully",
  "warnings": [
    "The old API key will remain valid until 2025-10-27 12:00:00 UTC",
    "Update your applications to use the new API key before the grace period ends"
  ]
}
```

#### Via Code

```csharp
public class ExampleService
{
    private readonly ApiKeyRotationService _rotationService;

    public async Task RotateKeyExample()
    {
        var apiKeyId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");
        
        var result = await _rotationService.RotateApiKeyAsync(apiKeyId);
        
        if (result.Success)
        {
            Console.WriteLine($"New API Key: {result.NewApiKey}");
            Console.WriteLine($"Grace period ends: {result.GracePeriodEndsAt}");
            
            // Store the new key securely
            // Update configuration in client applications
        }
        else
        {
            Console.WriteLine($"Rotation failed: {result.Message}");
        }
    }
}
```

### Check Rotation Status

**Get rotation status for a tenant**:
```http
GET /api/v1/apikeyrotation/status/{tenantId}
```

Response:
```json
[
  {
    "apiKeyId": "550e8400-e29b-41d4-a716-446655440001",
    "keyName": "Production API Key",
    "keyPrefix": "hvk_prod",
    "createdAt": "2025-07-20T12:00:00Z",
    "lastUsedAt": "2025-10-20T10:30:00Z",
    "age": "92.00:00:00",
    "timeUntilExpiration": "-2.00:00:00",
    "requiresRotation": true,
    "inGracePeriod": false,
    "urgency": "Critical"
  },
  {
    "apiKeyId": "550e8400-e29b-41d4-a716-446655440003",
    "keyName": "Development API Key",
    "keyPrefix": "hvk_dev_",
    "createdAt": "2025-09-15T08:00:00Z",
    "lastUsedAt": "2025-10-20T11:00:00Z",
    "age": "35.00:00:00",
    "timeUntilExpiration": "55.00:00:00",
    "requiresRotation": false,
    "inGracePeriod": false,
    "urgency": "None"
  }
]
```

### Validate Tenant Keys

**Get validation and recommendations**:
```http
GET /api/v1/apikeyrotation/validate/{tenantId}
```

Response:
```json
{
  "isValid": true,
  "tenantId": "550e8400-e29b-41d4-a716-446655440000",
  "totalKeys": 3,
  "keysRequiringRotation": 1,
  "criticalKeys": 1,
  "highUrgencyKeys": 0,
  "recommendations": [
    "URGENT: 1 API key(s) have exceeded maximum lifetime and should be rotated immediately.",
    "INFO: 1 API key(s) are currently in grace period. Update your applications to use the new keys before the grace period ends."
  ]
}
```

## Rotation Urgency Levels

| Level | Description | Action Required |
|-------|-------------|-----------------|
| **None** | Key is healthy | No action needed |
| **Low** | Key is aging but not urgent | Plan for future rotation |
| **Medium** | Key approaching expiration | Schedule rotation soon |
| **High** | Key should be rotated soon | Rotate within days |
| **Critical** | Key exceeded maximum lifetime | Rotate immediately |

## Automatic Rotation

The background service automatically:

1. **Checks every hour** (configurable) for keys that need rotation
2. **Rotates expired keys** that have exceeded `MaxKeyLifetime`
3. **Revokes old keys** after the grace period expires

### Background Service Logs

```
[2025-10-20 12:00:00] API key rotation background service started. Check interval: 01:00:00
[2025-10-20 12:00:05] Starting API key rotation check
[2025-10-20 12:00:10] Auto-rotating expired API key 550e8400-... (Production API Key) for tenant 123e4567-...
[2025-10-20 12:00:15] Successfully auto-rotated API key 550e8400-.... New key: 660e9511-...
[2025-10-20 12:00:20] Auto-rotated 1 expired API keys
[2025-10-20 12:00:25] Revoked 2 API keys after grace period expiration
[2025-10-20 12:00:30] API key rotation check completed
```

## Best Practices

### 1. Client Application Updates

When rotating keys:

```csharp
// Example: Configuration update flow
public class ApiKeyUpdateService
{
    public async Task UpdateClientConfiguration(string newApiKey)
    {
        // 1. Store new key in secure configuration
        await _configService.SetSecretAsync("HookVerse:ApiKey", newApiKey);
        
        // 2. Test new key
        var testResult = await TestApiKey(newApiKey);
        if (!testResult.Success)
        {
            throw new Exception("New API key validation failed");
        }
        
        // 3. Gradually roll out to production instances
        await _deploymentService.RollingUpdateAsync(
            configKey: "HookVerse:ApiKey",
            newValue: newApiKey);
        
        // 4. Monitor for errors during grace period
        await _monitoringService.WatchForErrors(
            duration: TimeSpan.FromDays(7));
    }
}
```

### 2. Monitoring and Alerts

Set up alerts for:

- Keys approaching expiration (14 days)
- Keys in critical state (exceeded lifetime)
- Failed rotation attempts
- Grace period nearing expiration

**Prometheus Alert Example**:
```yaml
- alert: ApiKeyExpirationWarning
  expr: hookverse_api_key_days_until_expiration < 14
  for: 1d
  labels:
    severity: warning
  annotations:
    summary: "API key expiring soon"
    description: "API key {{ $labels.key_id }} will expire in {{ $value }} days"

- alert: ApiKeyExpirationCritical
  expr: hookverse_api_key_days_until_expiration < 0
  for: 1h
  labels:
    severity: critical
  annotations:
    summary: "API key expired"
    description: "API key {{ $labels.key_id }} has exceeded maximum lifetime"
```

### 3. Zero-Downtime Rotation

To achieve zero-downtime rotation:

1. **Rotate key** via API or automatic rotation
2. **Update configuration** in client applications during grace period
3. **Deploy gradually** using rolling updates or blue-green deployment
4. **Monitor traffic** on both old and new keys
5. **Verify switchover** before grace period ends

### 4. Emergency Rotation

For security incidents requiring immediate rotation:

```bash
# Trigger immediate rotation
curl -X POST https://api.hookverse.com/api/v1/apikeyrotation/{keyId}/rotate \
  -H "Authorization: Bearer {admin-token}"

# Manually revoke old key (bypassing grace period)
curl -X POST https://api.hookverse.com/api/v1/apikeys/{oldKeyId}/revoke \
  -H "Authorization: Bearer {admin-token}"
```

## Metrics

The rotation service exposes metrics for monitoring:

```promql
# Keys by urgency level
hookverse_api_keys_by_urgency{urgency="critical"} 0
hookverse_api_keys_by_urgency{urgency="high"} 2
hookverse_api_keys_by_urgency{urgency="medium"} 5
hookverse_api_keys_by_urgency{urgency="low"} 10
hookverse_api_keys_by_urgency{urgency="none"} 50

# Rotation operations
rate(hookverse_api_key_rotations_total[5m])
rate(hookverse_api_key_rotation_failures_total[5m])

# Grace period keys
hookverse_api_keys_in_grace_period 3

# Average key age
hookverse_api_key_age_days_avg 45
```

## Security Considerations

1. **Key Storage**: Never store plain-text API keys; only hashed values are stored in the database
2. **Secure Transmission**: Always transmit new keys over HTTPS
3. **Audit Logging**: All rotation operations are logged for audit purposes
4. **Access Control**: Rotation endpoints should require appropriate authentication
5. **Rate Limiting**: Apply rate limits to rotation endpoints to prevent abuse

## Troubleshooting

### Issue: Rotation fails with "API key not found"

**Solution**: Verify the API key ID is correct and the key is active:
```bash
curl https://api.hookverse.com/api/v1/apikeys/{keyId}
```

### Issue: Old key not revoked after grace period

**Solution**: Check background service logs:
```bash
kubectl logs -l app=hookverse-api -n hookverse | grep "grace period"
```

Verify `AutoRevokeAfterGracePeriod` is set to `true` in configuration.

### Issue: Automatic rotation not running

**Solution**: Verify background service is enabled:
```json
{
  "ApiKeyRotationBackgroundService": {
    "Enabled": true
  }
}
```

Check service health:
```bash
kubectl get pods -l app=hookverse-api -n hookverse
kubectl logs {pod-name} | grep "rotation background service"
```

### Issue: Client applications failing after rotation

**Solution**: Check if grace period has expired:
```http
GET /api/v1/apikeyrotation/status/{tenantId}
```

If old key was revoked prematurely, generate a new key:
```http
POST /api/v1/apikeys
{
  "tenantId": "...",
  "name": "Emergency Key"
}
```

## Related Documentation

- [API Key Management](./api-keys.md)
- [Authentication Middleware](../api/authentication.md)
- [Security Best Practices](../security/best-practices.md)
- [Operations Runbook](../operations/runbook.md)
