# Phase 8 Implementation: Mock Webhook Endpoints

## Overview
Phase 8 implements User Story 4 - Mock Webhook Endpoints for Testing. This feature allows developers to create mock HTTP endpoints with configurable responses to test their webhook-receiving applications.

**Tasks Completed**: T163-T178 (16/18 Phase 8 tasks = 89%)

## What Was Implemented

### 1. Domain Models (T163)

**MockEndpoint Entity** (`src/HookVerse.Core/Entities/MockEndpoint.cs`)
- Represents a configurable mock webhook endpoint
- Properties:
  - `SubscriberId`: Owner of the mock endpoint
  - `Name`, `Description`: User-friendly identification
  - `UrlPath`: Auto-generated path (format: `/mock/{guid}`)
  - `ResponseStatus`: HTTP status code (default: 200)
  - `ResponseBody`: Configurable response content
  - `ResponseContentType`: Content type (default: application/json)
  - `ResponseDelayMs`: Simulated latency (default: 0)
  - `ResponseHeaders`: Custom headers (JSON)
  - `IsActive`: Enable/disable endpoint
  - `RequestCount`, `LastRequestAt`: Usage statistics

**MockEndpointRequest Entity** (Supporting)
- Logs all requests to mock endpoints for debugging
- Captures: method, path, query string, headers, body, IP, user-agent
- Stores response details for audit trail

### 2. EF Core Configuration (T164-T165)

**MockEndpointConfiguration** (`src/HookVerse.Infrastructure/Data/Configurations/`)
- Table: `MockEndpoints`
- Unique index on `UrlPath` for fast lookups
- Indexes on `SubscriberId`, `IsActive`
- JSONB column for `ResponseHeaders` (PostgreSQL optimization)
- Cascade delete from `Subscriber`

**MockEndpointRequestConfiguration**
- Table: `MockEndpointRequests`
- Indexes on `MockEndpointId`, `ReceivedAt`
- JSONB for request/response headers
- Text columns for bodies

**Database Migration**: `AddMockEndpoints` created successfully

### 3. Repository Layer (T166-T167)

**IMockEndpointRepository** (`src/HookVerse.Core/Interfaces/`)
- Standard CRUD operations (via `IRepository<MockEndpoint>`)
- Specialized methods:
  - `GetBySubscriberIdAsync`: List all endpoints for subscriber
  - `GetByUrlPathAsync`: Find endpoint by URL (for request routing)
  - `GetActiveBySubscriberIdAsync`: Active endpoints only
  - `IncrementRequestCountAsync`: Atomic statistics update

**MockEndpointRepository** (`src/HookVerse.Infrastructure/Repositories/`)
- Implements all interface methods
- Optimized queries with proper indexes
- Thread-safe request counting

### 4. API Layer (T168-T174)

**DTOs** (`src/HookVerse.Api/Models/MockEndpointDtos.cs`)
- `CreateMockEndpointRequest`: All configuration properties
- `UpdateMockEndpointRequest`: Optional properties for partial updates
- `MockEndpointResponse`: Full details including generated URL
- `TriggerMockWebhookRequest`: EventType and payload for manual testing

**MockEndpointsController** (`src/HookVerse.Api/Controllers/`)
Six REST endpoints:
1. **POST /api/v1/mock-endpoints**: Create new mock endpoint
   - Auto-generates unique URL path
   - Returns 201 Created with Location header

2. **GET /api/v1/mock-endpoints**: List all mock endpoints
   - Filtered by authenticated subscriber
   - Includes usage statistics

3. **GET /api/v1/mock-endpoints/{id}**: Get endpoint details
   - Authorization check (subscriberId match)
   - Returns 404 if not found or unauthorized

4. **PUT /api/v1/mock-endpoints/{id}**: Update endpoint
   - Partial updates supported
   - Only updates non-null properties

5. **DELETE /api/v1/mock-endpoints/{id}**: Delete endpoint
   - Authorization check
   - Returns 204 No Content

6. **POST /api/v1/mock-endpoints/{id}/trigger**: Manual test trigger
   - Logs test webhook request
   - Returns mock endpoint URL and details

### 5. Business Logic (T175-T178)

**IMockEndpointService** (`src/HookVerse.Core/Interfaces/`)
- Interface with 6 methods
- Core method: `HandleMockRequestAsync` returns tuple:
  - (StatusCode, Body, ContentType, Headers, DelayMs)

**MockEndpointService** (`src/HookVerse.Core/Services/`, 201 lines)
- **CreateMockEndpointAsync**: 
  - Generates unique URL: `/mock/{Guid.NewGuid():N}`
  - Serializes headers dictionary to JSON
  
- **GetMockEndpointAsync**: 
  - Authorization check (subscriberId validation)
  
- **UpdateMockEndpointAsync**: 
  - Null-safe partial updates
  - Handles header serialization/deserialization
  
- **DeleteMockEndpointAsync**: 
  - Authorization + deletion
  
- **HandleMockRequestAsync** (T177-T178):
  - Finds mock endpoint by URL path
  - Creates `MockEndpointRequest` record (full logging)
  - Increments request count atomically
  - Parses response headers from JSON
  - Returns configured response (status, body, headers, delay)
  - Returns 404 if endpoint not found or inactive

### 6. Request Handler Middleware

**MockEndpointMiddleware** (`src/HookVerse.Api/Middleware/`)
- Intercepts requests to `/mock/*` paths
- Extracts request details:
  - Method, path, query string
  - Headers, body, content type
  - Client IP, user agent
- Calls `HandleMockRequestAsync` service method
- Applies configured delay (`await Task.Delay`)
- Sets response: status code, content type, headers, body
- Error handling with 500 response

**Registered in Pipeline** (`Program.cs`):
- Added before API key authentication
- Allows unauthenticated access to mock endpoints

### 7. Dependency Injection (T163-T178)

**ServiceCollectionExtensions** updated:
- `IMockEndpointRepository` → `MockEndpointRepository`
- `IMockEndpointService` → `MockEndpointService`

## How It Works

### Creating a Mock Endpoint

```bash
POST /api/v1/mock-endpoints
X-Api-Key: your-api-key

{
  "name": "Test Success Response",
  "description": "Returns 200 OK with custom payload",
  "responseStatus": 200,
  "responseBody": "{\"success\":true,\"message\":\"Webhook received\"}",
  "responseContentType": "application/json",
  "responseDelayMs": 500,
  "responseHeaders": {
    "X-Custom-Header": "test-value"
  }
}
```

Response:
```json
{
  "id": "guid",
  "url": "https://api.example.com/mock/abc123def456",
  "urlPath": "/mock/abc123def456",
  "name": "Test Success Response",
  "responseStatus": 200,
  ...
}
```

### Using the Mock Endpoint

Any HTTP request to the generated URL:
```bash
POST https://api.example.com/mock/abc123def456
Content-Type: application/json

{
  "event": "order.created",
  "data": { "orderId": 123 }
}
```

Mock endpoint behavior:
1. Logs the request (method, headers, body, IP, etc.)
2. Increments request count
3. Waits 500ms (configured delay)
4. Returns HTTP 200 with configured body and headers

### Inspecting Requests

```bash
GET /api/v1/mock-endpoints/{id}
```

Returns endpoint details including:
- `requestCount`: Total requests received
- `lastRequestAt`: Timestamp of last request

## Testing Scenarios Enabled

1. **Success Cases**: Mock 200/201 responses to verify happy path
2. **Error Handling**: Mock 4xx/5xx responses to test error handling
3. **Timeouts**: Configure delays to test timeout behavior
4. **Retries**: Mock failures to verify retry logic
5. **Custom Headers**: Test signature validation, auth headers
6. **Rate Limiting**: Test multiple requests to same endpoint

## Architecture Highlights

### Layered Design
```
Controller (REST API) 
    ↓
Service (Business Logic) 
    ↓
Repository (Data Access) 
    ↓
Entity Framework (ORM) 
    ↓
PostgreSQL (Database)
```

### Request Flow
```
HTTP Request → MockEndpointMiddleware 
    ↓
MockEndpointService.HandleMockRequestAsync 
    ↓
MockEndpointRepository (lookup by URL)
    ↓
Log request → Update stats → Return response
```

### Data Integrity
- Unique constraint on `UrlPath` prevents duplicates
- Authorization checks in service layer
- Atomic request counting
- Cascade deletes maintain referential integrity

## Pending Work (T179-T180)

### T179: Dashboard UI
Create `src/HookVerse.Dashboard/Components/Pages/MockEndpoints.razor`:
- List all mock endpoints with stats
- Create/edit/delete forms
- View request history
- Copy URL button
- Test endpoint button

### T180: Metrics
Add OpenTelemetry metrics:
- `mock_endpoint_requests_total` (counter) - by endpoint_id, method, status
- `mock_endpoint_response_time` (histogram) - by endpoint_id
- `mock_endpoint_active_count` (gauge) - by subscriber_id

## Files Created/Modified

### New Files (11)
1. `src/HookVerse.Core/Entities/MockEndpoint.cs`
2. `src/HookVerse.Core/Entities/MockEndpointRequest.cs`
3. `src/HookVerse.Infrastructure/Data/Configurations/MockEndpointConfiguration.cs`
4. `src/HookVerse.Infrastructure/Data/Configurations/MockEndpointRequestConfiguration.cs`
5. `src/HookVerse.Core/Interfaces/IMockEndpointRepository.cs`
6. `src/HookVerse.Infrastructure/Repositories/MockEndpointRepository.cs`
7. `src/HookVerse.Api/Models/MockEndpointDtos.cs`
8. `src/HookVerse.Core/Interfaces/IMockEndpointService.cs`
9. `src/HookVerse.Core/Services/MockEndpointService.cs` (201 lines)
10. `src/HookVerse.Api/Controllers/MockEndpointsController.cs` (308 lines)
11. `src/HookVerse.Api/Middleware/MockEndpointMiddleware.cs`

### Modified Files (4)
1. `src/HookVerse.Infrastructure/Data/HookVerseDbContext.cs` (added DbSets)
2. `src/HookVerse.Api/Extensions/ServiceCollectionExtensions.cs` (DI registration)
3. `src/HookVerse.Api/Program.cs` (middleware registration)
4. `specs/001-webhook-delivery-platform/tasks.md` (marked T163-T178 complete)

### Database Migration (1)
- `src/HookVerse.Infrastructure/Migrations/YYYYMMDD_AddMockEndpoints.cs`

## Build Status
✅ **Solution builds successfully** (all projects compile without errors)

## Progress Update
- **Phase 7**: 100% complete (18/18 tasks)
- **Phase 8**: 89% complete (16/18 tasks)
- **Overall**: 178/232 tasks = 77% complete

## Next Steps
1. Implement T179: Dashboard UI for mock endpoint management
2. Implement T180: OpenTelemetry metrics
3. Test end-to-end with real webhook payloads
4. Move to Phase 9: GDPR Compliance
