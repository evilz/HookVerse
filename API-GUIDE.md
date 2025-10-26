# HookVerse API - Complete Usage Guide

**Last Updated**: October 19, 2025  
**API Version**: 1.0

## Table of Contents

1. [Getting Started](#getting-started)
2. [Complete Workflow Example](#complete-workflow-example)
3. [API Reference](#api-reference)
4. [Authentication](#authentication)
5. [Error Handling](#error-handling)
6. [Rate Limiting](#rate-limiting)
7. [Service Discovery & Cross-Service Communication](#service-discovery--cross-service-communication)
8. [Best Practices](#best-practices)

---

## Getting Started

### Base URL

```
Production: https://api.hookverse.com (not yet deployed)
Development: https://localhost:7001
```

### Authentication

Currently using development placeholder. Full API key authentication will be implemented in later phases.

**Headers:**
```http
X-API-Key: your-api-key-here
Content-Type: application/json
```

---

## Complete Workflow Example

This section shows a complete end-to-end workflow from setup to webhook delivery.

### Step 1: Create a Subscriber

A subscriber is the entity that will receive webhooks (your application/customer).

**Request:**
```http
POST /api/v1/subscribers HTTP/1.1
Host: localhost:7001
Content-Type: application/json

{
  "name": "Acme Corporation",
  "apiKeyPrefix": "acme",
  "webhookSecret": "sup3r-s3cr3t-k3y-32-ch4rs-l0ng",
  "retentionDays": 30,
  "description": "Production environment"
}
```

**Response:** `201 Created`
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Acme Corporation",
  "apiKey": "acme_live_abc123def456ghi789jkl012mno345",
  "apiKeyPrefix": "acme",
  "isActive": true,
  "retentionDays": 30,
  "description": "Production environment",
  "createdAt": "2025-10-19T10:00:00Z",
  "updatedAt": null
}
```

**Save this information:**
- `id`: 550e8400-e29b-41d4-a716-446655440000
- `apiKey`: acme_live_abc123def456ghi789jkl012mno345

### Step 2: Create Event Types

Define the types of events you'll send.

**Request:**
```http
POST /api/v1/event-types HTTP/1.1
Host: localhost:7001
Content-Type: application/json

{
  "name": "user.created",
  "description": "Fired when a new user signs up",
  "subscriberId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response:** `201 Created`
```json
{
  "id": "660e8400-e29b-41d4-a716-446655440001",
  "name": "user.created",
  "description": "Fired when a new user signs up",
  "subscriberId": "550e8400-e29b-41d4-a716-446655440000",
  "hasSchema": false,
  "createdAt": "2025-10-19T10:01:00Z",
  "updatedAt": null
}
```

**Create more event types:**
```json
// user.updated
{
  "name": "user.updated",
  "description": "Fired when user data changes",
  "subscriberId": "550e8400-e29b-41d4-a716-446655440000"
}

// order.created
{
  "name": "order.created",
  "description": "Fired when a new order is placed",
  "subscriberId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Save event type IDs:**
- `user.created`: 660e8400-e29b-41d4-a716-446655440001
- `user.updated`: 660e8400-e29b-41d4-a716-446655440002
- `order.created`: 660e8400-e29b-41d4-a716-446655440003

### Step 3: Attach JSON Schema (Optional but Recommended)

Define the structure of webhook payloads.

**Request:**
```http
PUT /api/v1/event-types/user.created/schema HTTP/1.1
Host: localhost:7001
Content-Type: application/json

{
  "format": "JsonSchema",
  "content": "{\"$schema\":\"http://json-schema.org/draft-07/schema#\",\"type\":\"object\",\"properties\":{\"userId\":{\"type\":\"string\",\"format\":\"uuid\"},\"email\":{\"type\":\"string\",\"format\":\"email\"},\"name\":{\"type\":\"string\"},\"createdAt\":{\"type\":\"string\",\"format\":\"date-time\"}},\"required\":[\"userId\",\"email\",\"name\",\"createdAt\"]}",
  "description": "User creation event schema v1",
  "version": 1,
  "isActive": true
}
```

**Formatted schema content:**
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "userId": {
      "type": "string",
      "format": "uuid"
    },
    "email": {
      "type": "string",
      "format": "email"
    },
    "name": {
      "type": "string"
    },
    "createdAt": {
      "type": "string",
      "format": "date-time"
    }
  },
  "required": ["userId", "email", "name", "createdAt"]
}
```

**Response:** `201 Created`
```json
{
  "id": "770e8400-e29b-41d4-a716-446655440004",
  "eventTypeId": "660e8400-e29b-41d4-a716-446655440001",
  "format": "JsonSchema",
  "content": "{\"$schema\":...}",
  "contentHash": "a8f5f167f44f4964e6c998dee827110c",
  "version": 1,
  "isActive": true,
  "description": "User creation event schema v1",
  "lastValidatedAt": null,
  "createdAt": "2025-10-19T10:02:00Z",
  "updatedAt": null
}
```

### Step 4: Create Subscriptions

Set up webhook endpoints that will receive events.

**Request:**
```http
POST /api/v1/subscriptions HTTP/1.1
Host: localhost:7001
Content-Type: application/json

{
  "eventTypeId": "660e8400-e29b-41d4-a716-446655440001",
  "endpointUrl": "https://api.acme.com/webhooks/user-events",
  "secret": "webhook-signing-secret-32-chars!",
  "authType": "None",
  "description": "Production user events endpoint",
  "timeoutSeconds": 30,
  "maxRetries": 5
}
```

**Auth Types:**
- `None` - No authentication
- `BasicAuth` - HTTP Basic Authentication
- `BearerToken` - Bearer token in Authorization header
- `ApiKey` - Custom API key header

**Response:** `201 Created`
```json
{
  "id": "880e8400-e29b-41d4-a716-446655440005",
  "subscriberId": "550e8400-e29b-41d4-a716-446655440000",
  "eventTypeId": "660e8400-e29b-41d4-a716-446655440001",
  "eventTypeName": "user.created",
  "endpointUrl": "https://api.acme.com/webhooks/user-events",
  "authType": "None",
  "description": "Production user events endpoint",
  "timeoutSeconds": 30,
  "maxRetries": 5,
  "isActive": true,
  "createdAt": "2025-10-19T10:03:00Z",
  "updatedAt": null,
  "pausedAt": null
}
```

### Step 5: Send Webhooks

Trigger webhook delivery to subscribed endpoints.

**Request:**
```http
POST /api/v1/webhooks HTTP/1.1
Host: localhost:7001
Content-Type: application/json

{
  "eventTypeId": "660e8400-e29b-41d4-a716-446655440001",
  "subscriberId": "550e8400-e29b-41d4-a716-446655440000",
  "payload": "{\"userId\":\"990e8400-e29b-41d4-a716-446655440006\",\"email\":\"john.doe@acme.com\",\"name\":\"John Doe\",\"createdAt\":\"2025-10-19T10:04:00Z\"}",
  "scheduledFor": null,
  "metadata": "{\"source\":\"registration-api\",\"ipAddress\":\"192.168.1.100\"}"
}
```

**Response:** `202 Accepted`
```json
{
  "id": "aa0e8400-e29b-41d4-a716-446655440007",
  "eventTypeId": "660e8400-e29b-41d4-a716-446655440001",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "createdAt": "2025-10-19T10:04:00Z",
  "scheduledFor": null,
  "expiresAt": "2025-11-18T10:04:00Z",
  "payloadSizeBytes": 156,
  "status": "accepted"
}
```

### Step 6: Check Delivery Status

Query webhook delivery attempts.

**Request:**
```http
GET /api/v1/webhooks/aa0e8400-e29b-41d4-a716-446655440007 HTTP/1.1
Host: localhost:7001
```

**Response:** `200 OK`
```json
{
  "id": "aa0e8400-e29b-41d4-a716-446655440007",
  "eventTypeId": "660e8400-e29b-41d4-a716-446655440001",
  "eventTypeName": "user.created",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "payload": "{\"userId\":\"990e8400-e29b-41d4-a716-446655440006\",\"email\":\"john.doe@acme.com\",\"name\":\"John Doe\",\"createdAt\":\"2025-10-19T10:04:00Z\"}",
  "payloadHash": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
  "payloadSizeBytes": 156,
  "createdAt": "2025-10-19T10:04:00Z",
  "scheduledFor": null,
  "expiresAt": "2025-11-18T10:04:00Z",
  "metadata": "{\"source\":\"registration-api\",\"ipAddress\":\"192.168.1.100\"}",
  "deliveryAttempts": [
    {
      "id": "bb0e8400-e29b-41d4-a716-446655440008",
      "subscriptionId": "880e8400-e29b-41d4-a716-446655440005",
      "endpointUrl": "https://api.acme.com/webhooks/user-events",
      "attemptNumber": 1,
      "responseStatusCode": 200,
      "responseBody": "{\"success\":true}",
      "errorMessage": null,
      "responseTimeMs": 245,
      "attemptedAt": "2025-10-19T10:04:01Z",
      "status": "Delivered"
    }
  ]
}
```

---

## API Reference

### Subscribers

#### List Subscribers
```http
GET /api/v1/subscribers?page=1&pageSize=20&isActive=true
```

#### Get Subscriber by ID
```http
GET /api/v1/subscribers/{id}
```

#### Update Subscriber
```http
PUT /api/v1/subscribers/{id}
Content-Type: application/json

{
  "retentionDays": 60,
  "description": "Updated description"
}
```

#### Delete Subscriber
```http
DELETE /api/v1/subscribers/{id}
```

### Event Types

#### List Event Types
```http
GET /api/v1/event-types?page=1&pageSize=20
```

#### Get Event Type by Name
```http
GET /api/v1/event-types/user.created
```

#### Get Schema for Event Type
```http
GET /api/v1/event-types/user.created/schema
```

#### Update Schema
```http
PUT /api/v1/event-types/user.created/schema
Content-Type: application/json

{
  "format": "JsonSchema",
  "content": "{...updated schema...}",
  "version": 2,
  "isActive": true
}
```

### Subscriptions

#### List Subscriptions
```http
GET /api/v1/subscriptions?page=1&pageSize=20&eventTypeId={guid}&isActive=true
```

#### Get Subscription
```http
GET /api/v1/subscriptions/{id}
```

#### Update Subscription
```http
PUT /api/v1/subscriptions/{id}
Content-Type: application/json

{
  "endpointUrl": "https://api.acme.com/webhooks/v2",
  "timeoutSeconds": 45,
  "maxRetries": 10
}
```

#### Pause Subscription
```http
POST /api/v1/subscriptions/{id}/pause
```

#### Resume Subscription
```http
POST /api/v1/subscriptions/{id}/resume
```

#### Delete Subscription
```http
DELETE /api/v1/subscriptions/{id}
```

### Webhooks

#### Send Webhook
```http
POST /api/v1/webhooks
Content-Type: application/json

{
  "eventTypeId": "guid",
  "subscriberId": "guid",
  "payload": "{...json...}",
  "scheduledFor": "2025-10-19T15:00:00Z",
  "metadata": "{...}"
}
```

#### Get Webhook Status
```http
GET /api/v1/webhooks/{id}
```

#### Get Delivery Attempts
```http
GET /api/v1/webhooks/{id}/attempts
```

#### Validate Payload (without sending)
```http
POST /api/v1/webhooks/validate
Content-Type: application/json

{
  "eventTypeId": "guid",
  "payload": "{...json...}"
}
```

### Message Bus (Testing Endpoint)

#### Trigger Test Message
```http
POST /api/v1/messagebus/trigger
Content-Type: application/json

{
  "eventTypeId": "guid",
  "payload": "{...json...}",
  "targetUrl": "https://example.com/webhook"
}
```

---

## Authentication

### Current Implementation (Development)

Authentication is currently in development placeholder mode. The API uses a hardcoded subscriber ID:

```csharp
// Fallback for development
return Guid.Parse("00000000-0000-0000-0000-000000000001");
```

### Planned Implementation

**API Key Authentication:**

```http
POST /api/v1/webhooks
X-API-Key: acme_live_abc123def456ghi789jkl012mno345
Content-Type: application/json

{...}
```

**Key Format:**
```
{prefix}_live_{random}
{prefix}_test_{random}
```

Example:
- Production: `acme_live_k8dh2jd9s0a1b2c3d4e5f6g7h8i9j0`
- Test: `acme_test_x1y2z3a4b5c6d7e8f9g0h1i2j3k4l5`

---

## Error Handling

### HTTP Status Codes

| Code | Meaning | Description |
|------|---------|-------------|
| 200 | OK | Successful GET request |
| 201 | Created | Resource successfully created |
| 202 | Accepted | Request accepted for processing |
| 400 | Bad Request | Invalid request body or parameters |
| 401 | Unauthorized | Missing or invalid authentication |
| 404 | Not Found | Resource doesn't exist |
| 409 | Conflict | Resource already exists |
| 422 | Unprocessable Entity | Validation failed |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server error |
| 503 | Service Unavailable | Service temporarily unavailable |

### Error Response Format

```json
{
  "error": "Validation failed",
  "details": [
    "Email field is required",
    "Password must be at least 8 characters"
  ],
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "timestamp": "2025-10-19T10:05:00Z"
}
```

### Common Errors

**Validation Error (400)**
```json
{
  "error": "Payload does not match event type schema",
  "details": [
    "Required property 'userId' is missing",
    "Property 'email' must be a valid email format"
  ]
}
```

**Not Found (404)**
```json
{
  "error": "Event type not found"
}
```

**Conflict (409)**
```json
{
  "error": "Event type with this name already exists"
}
```

---

## Rate Limiting

### Current Limits (Development)

Rate limiting is not yet implemented. Planned limits:

- **API Calls**: 1000 requests per minute per API key
- **Webhook Sends**: 100 webhooks per second per subscriber
- **Payload Size**: 1 MB maximum

### Rate Limit Headers (Planned)

```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 950
X-RateLimit-Reset: 1634650800
```

---

## Service Discovery & Cross-Service Communication

HookVerse uses .NET Aspire's built-in service discovery to enable seamless communication between services without hardcoding URLs. This section explains how services discover and communicate with each other.

### Overview

**Key Concepts**:
- **Service Names**: Services are registered with logical names (e.g., "api", "worker", "dashboard")
- **Automatic Resolution**: HttpClient automatically resolves service names to actual endpoints
- **Environment Independence**: Same code works across local, staging, and production environments
- **Zero Configuration**: No manual URL configuration needed in service code

### Service Architecture

```
┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│   Dashboard  │────────▶│     API      │────────▶│    Worker    │
│   (Blazor)   │  HTTP   │ (REST API)   │  Queue  │  (Background)│
└──────────────┘         └──────────────┘         └──────────────┘
       │                        │                        │
       │                        │                        │
       └────────────────────────┴────────────────────────┘
                    Service Discovery Layer
```

### Service Names

| Service | Aspire Name | Default Port | Purpose |
|---------|-------------|--------------|---------|
| API | `http://api` | 7001 | REST API for webhook management |
| Worker | `http://worker` | 7002 | Background webhook delivery |
| Dashboard | `http://dashboard` | 7003 | Admin UI for monitoring |
| PostgreSQL | `postgres` | 5432 | Database |
| RabbitMQ | `rabbitmq` | 5672 | Message queue |
| Redis | `redis` | 6379 | Caching |

### Configuring Service Discovery

**1. In ServiceDefaults (Automatic for All Services)**

All services that reference `HookVerse.ServiceDefaults` automatically get service discovery:

```csharp
// src/HookVerse.ServiceDefaults/Extensions.cs
public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) 
    where TBuilder : IHostApplicationBuilder
{
    // Enable service discovery for the service
    builder.Services.AddServiceDiscovery();

    // Configure all HttpClients to use service discovery
    builder.Services.ConfigureHttpClientDefaults(http =>
    {
        // Turn on resilience by default
        http.AddStandardResilienceHandler();

        // Turn on service discovery by default
        http.AddServiceDiscovery();
    });

    return builder;
}
```

**2. In Your Service (Automatic Registration)**

Simply call `AddServiceDefaults()` in your service's `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// This enables service discovery, OpenTelemetry, and health checks
builder.AddServiceDefaults();

// HttpClient will automatically use service discovery
builder.Services.AddHttpClient<IMyService, MyService>();

var app = builder.Build();
```

### Using Service Discovery in Code

#### Example 1: Dashboard Calling API

**Dashboard Service Registration:**

```csharp
// src/HookVerse.Dashboard/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Configure HttpClient to call the API service
// Use service name "http://api" (resolved via service discovery)
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://api";

builder.Services.AddHttpClient<IWebhookApiClient, WebhookApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

**Making API Calls:**

```csharp
// src/HookVerse.Dashboard/Services/WebhookApiClient.cs
public class WebhookApiClient : IWebhookApiClient
{
    private readonly HttpClient _httpClient;

    public WebhookApiClient(HttpClient httpClient)
    {
        // HttpClient is pre-configured with BaseAddress = "http://api"
        // Service discovery resolves "http://api" to actual endpoint
        _httpClient = httpClient;
    }

    public async Task<WebhookDetailsDto?> GetWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default)
    {
        // Request goes to: http://api/api/v1/webhooks/{webhookId}
        // Service discovery resolves "http://api" to https://localhost:7001 (local)
        //                                      or https://api.hookverse.com (production)
        var response = await _httpClient.GetAsync($"/api/v1/webhooks/{webhookId}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
            return null;
            
        return await response.Content.ReadFromJsonAsync<WebhookDetailsDto>(cancellationToken);
    }

    public async Task<List<EventTypeDto>> GetEventTypesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/v1/event-types", cancellationToken);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<List<EventTypeDto>>(cancellationToken) ?? [];
    }
}
```

#### Example 2: Worker Calling External Webhooks

**Worker Service Registration:**

```csharp
// src/HookVerse.Worker/Program.cs
var builder = Host.CreateApplicationBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Register HttpClient for webhook delivery
// This client is for external URLs (customer webhook endpoints)
builder.Services.AddHttpClient<IDeliveryService, DeliveryService>();
```

**Delivering Webhooks:**

```csharp
// src/HookVerse.Infrastructure/Services/DeliveryService.cs
public class DeliveryService : IDeliveryService
{
    private readonly HttpClient _httpClient;
    
    public DeliveryService(HttpClient httpClient)
    {
        // HttpClient with automatic resilience and telemetry
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<DeliveryResult> DeliverWebhookAsync(
        string targetUrl,
        string payload,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, targetUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        foreach (var header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Service discovery NOT used here - targetUrl is external customer endpoint
            var response = await _httpClient.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            return new DeliveryResult
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                ResponseBody = await response.Content.ReadAsStringAsync(cancellationToken),
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new DeliveryResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }
}
```

### Configuration Strategies

#### Strategy 1: Aspire Service Name (Recommended)

Use service names directly in configuration:

```json
{
  "ApiSettings": {
    "BaseUrl": "http://api"
  }
}
```

**Advantages**:
- Works automatically in Aspire-orchestrated environments
- Zero configuration for local development
- Consistent across all developers

#### Strategy 2: Fallback Configuration

Provide fallback URLs for non-Aspire scenarios:

```csharp
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://api";

builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});
```

**Configuration files**:

```json
// appsettings.Development.json (when NOT using Aspire)
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001"
  }
}

// appsettings.Production.json
{
  "ApiSettings": {
    "BaseUrl": "https://api.hookverse.com"
  }
}
```

#### Strategy 3: Named HttpClients

Register multiple HttpClients for different services:

```csharp
// Register named clients
builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri("http://api");
});

builder.Services.AddHttpClient("worker", client =>
{
    client.BaseAddress = new Uri("http://worker");
});

// Usage in service
public class MyService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> CallApiAsync()
    {
        var client = _httpClientFactory.CreateClient("api");
        var response = await client.GetAsync("/health");
        return await response.Content.ReadAsStringAsync();
    }
}
```

### Environment-Specific Behavior

#### Local Development (with Aspire AppHost)

```csharp
// AppHost automatically configures service discovery
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.HookVerse_Api>("api");
var dashboard = builder.AddProject<Projects.HookVerse_Dashboard>("dashboard");

// Dashboard can now reference "http://api"
// Aspire resolves to: https://localhost:7001
```

#### Kubernetes Deployment

Service discovery uses Kubernetes DNS:

```yaml
# Service name: api
# Namespace: hookverse
# DNS resolution: api.hookverse.svc.cluster.local
# HttpClient resolves "http://api" to: http://api.hookverse.svc.cluster.local
```

#### Azure Container Apps Deployment

Service discovery uses Azure Container Apps internal DNS:

```bicep
// Service name: api
// HttpClient resolves "http://api" to internal FQDN
// Example: api.internal.redgrass-12345.eastus.azurecontainerapps.io
```

### Testing Service Discovery

**Integration Test Example:**

```csharp
[Fact]
public async Task Dashboard_Should_Discover_And_Call_Api()
{
    // Arrange
    var appHost = await DistributedApplicationTestingBuilder
        .CreateAsync<Projects.HookVerse_AppHost>();
    
    await using var app = await appHost.BuildAsync();
    await app.StartAsync();

    var dashboardClient = app.CreateHttpClient("dashboard");
    var apiClient = app.CreateHttpClient("api");

    // Act - Dashboard calls API via service discovery
    var apiHealthResponse = await apiClient.GetAsync("/health");
    var dashboardHealthResponse = await dashboardClient.GetAsync("/health");

    // Assert
    apiHealthResponse.EnsureSuccessStatusCode();
    dashboardHealthResponse.EnsureSuccessStatusCode();
}
```

### Troubleshooting Service Discovery

#### Problem: Service not found

**Error Message:**
```
System.InvalidOperationException: No such host is known (api)
```

**Solutions:**
1. Verify service is registered in AppHost:
   ```csharp
   var api = builder.AddProject<Projects.HookVerse_Api>("api");
   ```

2. Check service is running:
   ```powershell
   # In Aspire Dashboard: https://localhost:17191
   # Resources tab -> Verify service status
   ```

3. Verify ServiceDefaults is referenced:
   ```xml
   <ProjectReference Include="..\HookVerse.ServiceDefaults\HookVerse.ServiceDefaults.csproj" />
   ```

#### Problem: Circular dependency

**Symptom**: Service A calls Service B, Service B calls Service A

**Solution**: Use asynchronous messaging instead:
```csharp
// Instead of HTTP call
await _apiClient.PostAsync("/notify", ...);

// Use RabbitMQ
await _messageBus.PublishAsync(new NotificationEvent { ... });
```

#### Problem: Timeout in local development

**Solution**: Increase timeout in development:
```csharp
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.BaseAddress = new Uri("http://api");
    client.Timeout = TimeSpan.FromSeconds(app.Environment.IsDevelopment() ? 300 : 30);
});
```

### Best Practices

1. **Use Service Names**: Always use logical service names (`http://api`) instead of hardcoded URLs
2. **Configure Timeouts**: Set appropriate timeouts based on expected response times
3. **Enable Resilience**: Use `AddStandardResilienceHandler()` for automatic retries
4. **Add Telemetry**: Service discovery integrates with OpenTelemetry automatically
5. **Test Locally**: Use Aspire AppHost to test service discovery before deploying
6. **Document Dependencies**: Clearly document which services depend on which

### Related Documentation

- [DEVELOPMENT.md](./DEVELOPMENT.md) - Local development setup with Aspire
- [RUNNING.md](./RUNNING.md) - Running and debugging services
- [DEPLOYMENT.md](./DEPLOYMENT.md) - Production deployment configuration

---

## Best Practices

### 1. Webhook Signatures

Always verify webhook signatures when receiving webhooks:

```csharp
using System.Security.Cryptography;
using System.Text;

public bool VerifySignature(string payload, string signature, string secret)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    var computedSignature = Convert.ToHexString(hash).ToLowerInvariant();
    
    return signature == computedSignature;
}
```

### 2. Idempotency

Use the `traceId` to implement idempotent webhook handling:

```csharp
public async Task<IActionResult> HandleWebhook([FromBody] WebhookPayload payload)
{
    var traceId = Request.Headers["X-Trace-ID"].ToString();
    
    // Check if already processed
    if (await _db.ProcessedWebhooks.AnyAsync(w => w.TraceId == traceId))
    {
        return Ok(new { status = "already_processed" });
    }
    
    // Process webhook
    await ProcessWebhookAsync(payload);
    
    // Save trace ID
    await _db.ProcessedWebhooks.AddAsync(new ProcessedWebhook { TraceId = traceId });
    await _db.SaveChangesAsync();
    
    return Ok(new { status = "success" });
}
```

### 3. Schema Validation

Always define schemas for your event types to ensure data consistency:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "eventId": {"type": "string", "format": "uuid"},
    "timestamp": {"type": "string", "format": "date-time"},
    "data": {"type": "object"}
  },
  "required": ["eventId", "timestamp", "data"],
  "additionalProperties": false
}
```

### 4. Error Handling

Implement proper error handling in your webhook receivers:

```csharp
[HttpPost("webhooks/user-events")]
public async Task<IActionResult> ReceiveWebhook([FromBody] JsonDocument payload)
{
    try
    {
        // Verify signature
        var signature = Request.Headers["X-Webhook-Signature"].ToString();
        if (!VerifySignature(payload.RootElement.GetRawText(), signature, _webhookSecret))
        {
            return Unauthorized(new { error = "Invalid signature" });
        }
        
        // Process webhook
        await ProcessUserEventAsync(payload);
        
        return Ok(new { status = "success" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing webhook");
        
        // Return 500 to trigger retry
        return StatusCode(500, new { error = "Processing failed" });
    }
}
```

### 5. Scheduled Webhooks

Use `scheduledFor` for delayed delivery:

```json
{
  "eventTypeId": "guid",
  "subscriberId": "guid",
  "payload": "{...}",
  "scheduledFor": "2025-10-19T15:00:00Z",
  "metadata": "{\"delayReason\":\"business_hours_only\"}"
}
```

### 6. Pagination

Always use pagination for list endpoints:

```http
GET /api/v1/subscriptions?page=1&pageSize=50
```

Response includes pagination metadata:
```json
{
  "items": [...],
  "page": 1,
  "pageSize": 50,
  "totalCount": 250,
  "totalPages": 5
}
```

### 7. Monitoring

Use the `traceId` for distributed tracing and log correlation:

```
INFO: Webhook sent with TraceId: 4bf92f3577b34da6a3ce929d0e0e4736
INFO: Delivery attempt 1 for TraceId: 4bf92f3577b34da6a3ce929d0e0e4736
INFO: Delivery successful for TraceId: 4bf92f3577b34da6a3ce929d0e0e4736
```

---

## Code Examples

### Complete Integration Example (C#)

```csharp
using System.Net.Http.Json;

public class HookVerseClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public HookVerseClient(string baseUrl, string apiKey)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _apiKey = apiKey;
    }

    public async Task<WebhookResponse> SendWebhookAsync(
        Guid eventTypeId,
        Guid subscriberId,
        object payload)
    {
        var request = new
        {
            eventTypeId,
            subscriberId,
            payload = JsonSerializer.Serialize(payload)
        };

        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
        
        var response = await _httpClient.PostAsJsonAsync("/api/v1/webhooks", request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<WebhookResponse>();
    }
}

// Usage
var client = new HookVerseClient("https://localhost:7001", "your-api-key");
var result = await client.SendWebhookAsync(
    eventTypeId: Guid.Parse("660e8400-e29b-41d4-a716-446655440001"),
    subscriberId: Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
    payload: new { userId = "123", email = "user@example.com" }
);

Console.WriteLine($"Webhook sent! TraceId: {result.TraceId}");
```

---

## Additional Resources

- **Swagger UI**: https://localhost:7001/swagger
- **OpenAPI Spec**: https://localhost:7001/swagger/v1/swagger.json
- **Health Check**: https://localhost:7001/health
- **Metrics**: https://localhost:7001/metrics (if enabled)

For infrastructure setup and troubleshooting, see [RUNNING.md](./RUNNING.md).
