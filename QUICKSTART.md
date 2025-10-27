# HookVerse - Quick Start Guide

## Creating Your First Subscriber and API Key

### Option 1: Using the API (Recommended)

#### 1. Start the application with Aspire

```powershell
dotnet run --project src/HookVerse.AppHost
```

#### 2. Create a subscriber

```powershell
$createSubscriberBody = @{
    name = "Development Team"
    email = "dev@hookverse.local"
    retentionDays = 90
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:7001/api/v1/subscribers" `
    -Method Post `
    -Body $createSubscriberBody `
    -ContentType "application/json"

# Save the API key - THIS IS THE ONLY TIME YOU'LL SEE IT!
$apiKey = $response.apiKey
$subscriberId = $response.id

Write-Host "✅ Subscriber created successfully!" -ForegroundColor Green
Write-Host "Subscriber ID: $subscriberId" -ForegroundColor Yellow
Write-Host "API Key: $apiKey" -ForegroundColor Yellow
Write-Host "⚠️  SAVE THIS API KEY - You won't see it again!" -ForegroundColor Red
```

**Response Example:**
```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "name": "Development Team",
  "email": "dev@hookverse.local",
  "isActive": true,
  "retentionDays": 90,
  "createdAt": "2025-10-27T09:00:00Z",
  "updatedAt": null,
  "apiKey": "xY8kP3mN5qR2wE9tU7oI6pA4sD1fG0hJ2kL5zX",
  "warning": "This API key will only be shown once. Please save it securely."
}
```

#### 3. Test the API key

```powershell
# Create an event type using your new API key
$createEventTypeBody = @{
    name = "user.created"
    description = "Fired when a new user signs up"
} | ConvertTo-Json

$headers = @{
    "X-API-Key" = $apiKey
}

$eventType = Invoke-RestMethod -Uri "http://localhost:7001/api/v1/event-types" `
    -Method Post `
    -Headers $headers `
    -Body $createEventTypeBody `
    -ContentType "application/json"

Write-Host "✅ Event type created: $($eventType.name)" -ForegroundColor Green
```

### Option 2: Using Swagger UI

1. Open http://localhost:7001/swagger
2. Navigate to **Subscribers** section
3. Click `POST /api/v1/subscribers`
4. Click **Try it out**
5. Fill in the request body:
```json
{
  "name": "Development Team",
  "email": "dev@hookverse.local",
  "retentionDays": 90
}
```
6. Click **Execute**
7. **Copy and save the API key from the response** (you won't see it again!)

---

## Configuring the Dashboard

The Dashboard needs an API key to communicate with the API.

### 1. Save your API key to appsettings

Edit `src/HookVerse.Dashboard/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ApiSettings": {
    "BaseUrl": "http://api",
    "ApiKey": "YOUR_API_KEY_HERE"
  }
}
```

### 2. Update Dashboard to use the API key

The Dashboard will automatically include the API key in all requests to the API.

---

## Managing API Keys

### List all API keys for a subscriber

```powershell
$headers = @{
    "X-API-Key" = $apiKey
}

$keys = Invoke-RestMethod -Uri "http://localhost:7001/api/v1/subscribers/$subscriberId/api-keys" `
    -Method Get `
    -Headers $headers

$keys | Format-Table Id, Name, KeyPrefix, IsActive, CreatedAt, LastUsedAt
```

### Create an additional API key

```powershell
$createKeyBody = @{
    name = "Production Key"
} | ConvertTo-Json

$newKey = Invoke-RestMethod -Uri "http://localhost:7001/api/v1/subscribers/$subscriberId/api-keys" `
    -Method Post `
    -Headers $headers `
    -Body $createKeyBody `
    -ContentType "application/json"

Write-Host "New API Key: $($newKey.apiKey)" -ForegroundColor Yellow
```

### Revoke an API key

```powershell
$keyIdToRevoke = "key-id-here"

Invoke-RestMethod -Uri "http://localhost:7001/api/v1/subscribers/$subscriberId/api-keys/$keyIdToRevoke" `
    -Method Delete `
    -Headers $headers

Write-Host "✅ API key revoked" -ForegroundColor Green
```

---

## Subscriber Management

### List all subscribers

```powershell
# No authentication required for listing subscribers
$subscribers = Invoke-RestMethod -Uri "http://localhost:7001/api/v1/subscribers?page=1&pageSize=20" `
    -Method Get

$subscribers.items | Format-Table Id, Name, Email, IsActive, RetentionDays, CreatedAt
```

### Get a specific subscriber

```powershell
$subscriber = Invoke-RestMethod -Uri "http://localhost:7001/api/v1/subscribers/$subscriberId" `
    -Method Get

$subscriber
```

### Update a subscriber

```powershell
$updateBody = @{
    retentionDays = 120
} | ConvertTo-Json

$updated = Invoke-RestMethod -Uri "http://localhost:7001/api/v1/subscribers/$subscriberId" `
    -Method Put `
    -Body $updateBody `
    -ContentType "application/json"

Write-Host "✅ Subscriber updated - Retention: $($updated.retentionDays) days" -ForegroundColor Green
```

### Delete a subscriber

```powershell
Invoke-RestMethod -Uri "http://localhost:7001/api/v1/subscribers/$subscriberId" `
    -Method Delete

Write-Host "✅ Subscriber deleted" -ForegroundColor Green
```

---

## Security Best Practices

1. **Never commit API keys to source control**
   - Use `appsettings.Development.json` (already in .gitignore)
   - Use User Secrets for local development
   - Use Azure Key Vault in production

2. **Rotate API keys regularly**
   - Create a new API key
   - Update all services
   - Revoke the old key

3. **Use different keys for different environments**
   - Development: One subscriber with one key
   - Staging: Separate subscriber
   - Production: Separate subscriber with rotated keys

4. **Monitor API key usage**
   - Check `LastUsedAt` field
   - Revoke unused keys

---

## Troubleshooting

### "Invalid API key" error

- Check that you copied the API key correctly (no extra spaces)
- Verify the key is active: `GET /api/v1/subscribers/{id}/api-keys`
- Ensure you're including the header: `X-API-Key: your-key-here`

### "API key is required" error

- You're trying to access a protected endpoint without authentication
- Add the `X-API-Key` header to your request

### Lost your API key?

- You cannot retrieve a lost API key (it's hashed in the database)
- Create a new API key for the subscriber
- Revoke the old key if possible

---

## Next Steps

1. **Create Event Types** - Define the events you'll send
2. **Create Subscriptions** - Set up webhook endpoints
3. **Send Webhooks** - Start delivering events
4. **Monitor in Dashboard** - View delivery status and metrics

See [API-GUIDE.md](./API-GUIDE.md) for complete API documentation.
