# HookVerse API Documentation

**Version**: v1  
**Base URL**: `http://localhost:5000/api/v1` (default)  
**Authentication**: API Key (Header: `X-Api-Key`)

## Overview

HookVerse provides REST APIs for webhook management, subscription configuration, delivery tracking, and GDPR compliance. All endpoints require API key authentication.

## Table of Contents

- [Authentication](#authentication)
- [Webhook Events](#webhook-events)
- [Event Types](#event-types)
- [Subscriptions](#subscriptions)
- [Delivery Attempts](#delivery-attempts)
- [Analytics](#analytics)
- [Mock Endpoints](#mock-endpoints)
- [GDPR Compliance](#gdpr-compliance)
- [Error Handling](#error-handling)

---

## Authentication

All API requests require an API key in the `X-Api-Key` header:

```http
X-Api-Key: your-api-key-here
```

API keys are associated with subscribers and provide access to their webhooks and subscriptions.

---

## Webhook Events

### Submit a Webhook Event

Submit a webhook event for delivery to all matching subscriptions.

**Endpoint**: `POST /api/v1/webhooks`

**Request Body**:
```json
{
  "eventType": "order.created",
  "payload": {
    "orderId": "ord_123456",
    "customerId": "cus_789",
    "amount": 99.99,
    "currency": "USD"
  },
  "metadata": {
    "source": "checkout-service",
    "correlationId": "abc-def-123"
  },
  "scheduledFor": "2025-10-20T15:30:00Z",
  "expiresAt": "2025-10-27T15:30:00Z"
}
```

**Request Fields**:
- `eventType` (string, required): Event type identifier (must match existing event type)
- `payload` (object, required): JSON payload to deliver (max 256KB default, 1MB max)
- `metadata` (object, optional): Additional metadata for tracking
- `scheduledFor` (datetime, optional): Schedule delivery for future time
- `expiresAt` (datetime, optional): Expiration time (default: 7 days from creation)

**Response** (202 Accepted):
```json
{
  "id": "evt_8a7b6c5d",
  "eventTypeId": "et_123456",
  "eventType": "order.created",
  "status": "Pending",
  "scheduledFor": "2025-10-20T15:30:00Z",
  "createdAt": "2025-10-20T15:25:00Z",
  "traceId": "trace-abc-123"
}
```

### Get Webhook Event Details

Retrieve details of a specific webhook event.

**Endpoint**: `GET /api/v1/webhooks/{id}`

**Response** (200 OK):
```json
{
  "id": "evt_8a7b6c5d",
  "eventTypeId": "et_123456",
  "eventType": "order.created",
  "payload": { "orderId": "ord_123456" },
  "metadata": { "source": "checkout-service" },
  "status": "Delivered",
  "scheduledFor": "2025-10-20T15:30:00Z",
  "expiresAt": "2025-10-27T15:30:00Z",
  "createdAt": "2025-10-20T15:25:00Z",
  "deliveryAttempts": [
    {
      "id": "da_111",
      "attemptNumber": 1,
      "status": "Success",
      "responseStatus": 200,
      "durationMs": 125,
      "startedAt": "2025-10-20T15:30:01Z",
      "completedAt": "2025-10-20T15:30:01Z"
    }
  ]
}
```

### Search Webhook Events

Search webhook events with filtering and pagination.

**Endpoint**: `GET /api/v1/webhooks/search`

**Query Parameters**:
- `startDate` (datetime, optional): Filter by creation date >= startDate
- `endDate` (datetime, optional): Filter by creation date <= endDate
- `eventTypeId` (guid, optional): Filter by event type
- `status` (string, optional): Filter by status (Pending, Processing, Delivered, Failed, Expired)
- `page` (int, default: 1): Page number
- `pageSize` (int, default: 20, max: 100): Items per page

**Example Request**:
```http
GET /api/v1/webhooks/search?eventTypeId=et_123456&status=Delivered&page=1&pageSize=20
```

**Response** (200 OK):
```json
{
  "items": [
    {
      "id": "evt_8a7b6c5d",
      "eventType": "order.created",
      "status": "Delivered",
      "createdAt": "2025-10-20T15:25:00Z",
      "deliveryCount": 3
    }
  ],
  "totalItems": 145,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

---

## Event Types

### List Event Types

Get all event types for the subscriber.

**Endpoint**: `GET /api/v1/event-types`

**Response** (200 OK):
```json
[
  {
    "id": "et_123456",
    "name": "order.created",
    "description": "Triggered when a new order is created",
    "schemaFormat": "JsonSchema",
    "schema": "{ \"type\": \"object\", \"properties\": { ... } }",
    "isActive": true,
    "createdAt": "2025-10-01T10:00:00Z"
  }
]
```

### Create Event Type

Register a new event type.

**Endpoint**: `POST /api/v1/event-types`

**Request Body**:
```json
{
  "name": "order.created",
  "description": "Triggered when a new order is created",
  "schemaFormat": "JsonSchema",
  "schema": "{\"type\":\"object\",\"properties\":{\"orderId\":{\"type\":\"string\"}},\"required\":[\"orderId\"]}"
}
```

**Response** (201 Created):
```json
{
  "id": "et_123456",
  "name": "order.created",
  "description": "Triggered when a new order is created",
  "schemaFormat": "JsonSchema",
  "schema": "...",
  "isActive": true,
  "createdAt": "2025-10-20T15:30:00Z"
}
```

---

## Subscriptions

### List Subscriptions

Get all subscriptions for the subscriber.

**Endpoint**: `GET /api/v1/subscriptions`

**Response** (200 OK):
```json
[
  {
    "id": "sub_abc123",
    "eventTypeId": "et_123456",
    "eventTypeName": "order.created",
    "endpointUrl": "https://api.example.com/webhooks",
    "authType": "HmacSha256",
    "isActive": true,
    "maxRetries": 3,
    "timeoutSeconds": 30,
    "createdAt": "2025-10-15T10:00:00Z"
  }
]
```

### Create Subscription

Subscribe to an event type.

**Endpoint**: `POST /api/v1/subscriptions`

**Request Body**:
```json
{
  "eventTypeId": "et_123456",
  "endpointUrl": "https://api.example.com/webhooks",
  "authType": "HmacSha256",
  "secret": "your-secret-key",
  "maxRetries": 3,
  "timeoutSeconds": 30,
  "customHeaders": {
    "X-Custom-Header": "value"
  }
}
```

**Request Fields**:
- `eventTypeId` (guid, required): Event type to subscribe to
- `endpointUrl` (string, required): HTTPS endpoint URL
- `authType` (string, required): Authentication type (None, HmacSha256, Bearer)
- `secret` (string, optional): Secret key for HMAC or Bearer token
- `maxRetries` (int, default: 3): Maximum retry attempts
- `timeoutSeconds` (int, default: 30): HTTP timeout in seconds
- `customHeaders` (object, optional): Additional HTTP headers

**Response** (201 Created):
```json
{
  "id": "sub_abc123",
  "eventTypeId": "et_123456",
  "endpointUrl": "https://api.example.com/webhooks",
  "authType": "HmacSha256",
  "isActive": true,
  "maxRetries": 3,
  "timeoutSeconds": 30,
  "createdAt": "2025-10-20T15:30:00Z"
}
```

### Update Subscription

Update an existing subscription.

**Endpoint**: `PUT /api/v1/subscriptions/{id}`

**Request Body**: Same as Create Subscription

**Response** (200 OK): Updated subscription object

### Delete Subscription

Delete a subscription.

**Endpoint**: `DELETE /api/v1/subscriptions/{id}`

**Response** (204 No Content)

---

## Delivery Attempts

### Get Delivery Attempts

Get delivery attempts for a webhook event.

**Endpoint**: `GET /api/v1/webhooks/{webhookId}/deliveries`

**Response** (200 OK):
```json
[
  {
    "id": "da_111",
    "webhookEventId": "evt_8a7b6c5d",
    "subscriptionId": "sub_abc123",
    "attemptNumber": 1,
    "status": "Success",
    "responseStatus": 200,
    "responseBody": "{\"received\":true}",
    "durationMs": 125,
    "errorMessage": null,
    "startedAt": "2025-10-20T15:30:01Z",
    "completedAt": "2025-10-20T15:30:01Z"
  }
]
```

---

## Analytics

### Get Dashboard Analytics

Get aggregated webhook analytics.

**Endpoint**: `GET /api/v1/analytics/dashboard`

**Query Parameters**:
- `startDate` (datetime, optional): Start date for analytics
- `endDate` (datetime, optional): End date for analytics

**Response** (200 OK):
```json
{
  "totalWebhooks": 10523,
  "successfulDeliveries": 31245,
  "failedDeliveries": 324,
  "pendingWebhooks": 45,
  "averageDeliveryTimeMs": 187,
  "successRate": 98.97,
  "webhooksByStatus": {
    "Delivered": 9845,
    "Failed": 234,
    "Pending": 45,
    "Processing": 15,
    "Expired": 384
  },
  "deliveriesByHour": [
    { "hour": "2025-10-20T14:00:00Z", "count": 423 },
    { "hour": "2025-10-20T15:00:00Z", "count": 567 }
  ]
}
```

---

## Mock Endpoints

### List Mock Endpoints

Get all mock endpoints for testing.

**Endpoint**: `GET /api/v1/mock-endpoints`

**Response** (200 OK):
```json
[
  {
    "id": "mock_123",
    "name": "Test Endpoint",
    "description": "Mock endpoint for testing webhooks",
    "urlPath": "/test-webhook",
    "responseStatus": 200,
    "responseBody": "{\"received\":true}",
    "responseDelayMs": 0,
    "requestCount": 156,
    "lastRequestAt": "2025-10-20T15:30:00Z",
    "createdAt": "2025-10-15T10:00:00Z"
  }
]
```

### Create Mock Endpoint

Create a new mock endpoint.

**Endpoint**: `POST /api/v1/mock-endpoints`

**Request Body**:
```json
{
  "name": "Test Endpoint",
  "description": "Mock endpoint for testing webhooks",
  "urlPath": "/test-webhook",
  "responseStatus": 200,
  "responseBody": "{\"received\":true}",
  "responseDelayMs": 100
}
```

**Response** (201 Created): Mock endpoint object

### Trigger Test Webhook

Send a test webhook to a mock endpoint.

**Endpoint**: `POST /api/v1/mock-endpoints/{id}/trigger`

**Request Body**:
```json
{
  "eventType": "order.created",
  "payload": {
    "orderId": "test_123",
    "amount": 99.99
  }
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "webhookId": "evt_test_123",
  "deliveryAttemptId": "da_test_456",
  "status": "Success",
  "responseStatus": 200,
  "durationMs": 125
}
```

---

## GDPR Compliance

### Request Data Export

Request export of all personal data (GDPR Right to Access).

**Endpoint**: `POST /api/v1/gdpr/export`

**Request Body**:
```json
{
  "metadata": {
    "requestReason": "user request",
    "requestedBy": "user@example.com"
  }
}
```

**Response** (202 Accepted):
```json
{
  "id": "gdpr_req_123",
  "subscriberId": "sub_789",
  "requestType": "Export",
  "status": "Pending",
  "createdAt": "2025-10-20T15:30:00Z"
}
```

### Request Data Deletion

Request deletion of all personal data (GDPR Right to Erasure).

**Endpoint**: `POST /api/v1/gdpr/delete`

**Request Body**:
```json
{
  "confirmed": true,
  "metadata": {
    "requestReason": "user request",
    "requestedBy": "user@example.com"
  }
}
```

**Response** (202 Accepted): GDPR request object

### List GDPR Requests

Get all GDPR requests.

**Endpoint**: `GET /api/v1/gdpr/requests`

**Response** (200 OK):
```json
[
  {
    "id": "gdpr_req_123",
    "subscriberId": "sub_789",
    "requestType": "Export",
    "status": "Completed",
    "createdAt": "2025-10-20T15:30:00Z",
    "completedAt": "2025-10-20T15:31:23Z",
    "exportFilePath": "gdpr-exports/gdpr-export-sub789-20251020153123.json",
    "exportFileSizeBytes": 524288
  }
]
```

### Download Export File

Download a completed GDPR export file.

**Endpoint**: `GET /api/v1/gdpr/export/{id}/download`

**Response** (200 OK): 
- Content-Type: `application/json`
- Content-Disposition: `attachment; filename="gdpr-export-*.json"`
- Body: JSON file with all subscriber data

---

## Error Handling

### Standard Error Response

All error responses follow this format:

```json
{
  "error": "Error message describing what went wrong",
  "details": "Additional details (optional)",
  "traceId": "trace-abc-123"
}
```

### HTTP Status Codes

- `200 OK`: Successful GET request
- `201 Created`: Successful POST request (resource created)
- `202 Accepted`: Request accepted for async processing
- `204 No Content`: Successful DELETE request
- `400 Bad Request`: Invalid request data
- `401 Unauthorized`: Missing or invalid API key
- `404 Not Found`: Resource not found
- `409 Conflict`: Resource conflict (e.g., duplicate subscription)
- `422 Unprocessable Entity`: Schema validation failed
- `429 Too Many Requests`: Rate limit exceeded
- `500 Internal Server Error`: Server error

### Common Error Scenarios

**Invalid API Key**:
```json
{
  "error": "Invalid or missing API key"
}
```

**Schema Validation Failed**:
```json
{
  "error": "Payload validation failed",
  "details": "Property 'orderId' is required but missing"
}
```

**Rate Limit Exceeded**:
```json
{
  "error": "Rate limit exceeded. Try again later.",
  "retryAfter": 60
}
```

---

## Rate Limits

- **Default**: 1000 requests per minute per API key
- **Webhook submission**: 100 requests per second per API key
- **Headers**: 
  - `X-RateLimit-Limit`: Maximum requests per window
  - `X-RateLimit-Remaining`: Remaining requests in current window
  - `X-RateLimit-Reset`: Unix timestamp when limit resets

---

## Webhooks

For integration testing and development, HookVerse provides mock endpoints that can receive webhooks. See the [Mock Endpoints](#mock-endpoints) section for details.

## OpenAPI Specification

The complete OpenAPI 3.0 specification is available at:
- **Swagger UI**: `http://localhost:5000/swagger`
- **JSON**: `http://localhost:5000/swagger/v1/swagger.json`

---

## Next Steps

- Review [Quickstart Guide](../../specs/001-webhook-delivery-platform/quickstart.md) for integration examples
- Explore [Dashboard UI](http://localhost:5001) for visual webhook management
- Check [Deployment Guide](../deployment/kubernetes.md) for production setup
