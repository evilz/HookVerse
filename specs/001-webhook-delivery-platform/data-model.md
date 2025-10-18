# Data Model: HookVerse - Webhook Delivery Platform

**Feature**: 001-webhook-delivery-platform  
**Date**: 2025-10-18  
**Purpose**: Define domain entities, relationships, and validation rules for the webhook delivery platform

## Entity Overview

```
┌─────────────────┐         ┌──────────────────┐
│  EventType      │◄────────┤  WebhookEvent    │
│                 │         │                  │
│ - Id            │         │ - Id             │
│ - Name          │         │ - EventTypeId    │
│ - Schema        │         │ - Payload        │
└─────────────────┘         │ - CreatedAt      │
                            └────────┬─────────┘
                                     │
                                     │ 1:N
                                     ▼
┌─────────────────┐         ┌──────────────────┐
│  Subscription   │         │  DeliveryAttempt │
│                 │         │                  │
│ - Id            │         │ - Id             │
│ - EndpointUrl   │◄────────┤ - WebhookEventId │
│ - EventTypeId   │         │ - SubscriptionId │
│ - IsActive      │         │ - Status         │
└────────┬────────┘         │ - AttemptNumber  │
         │                  │ - ResponseStatus │
         │ N:1              └──────────────────┘
         ▼
┌─────────────────┐
│  Subscriber     │
│                 │
│ - Id            │
│ - Name          │
│ - ApiKey        │
└─────────────────┘

┌─────────────────┐
│  SchemaDefinition│
│                 │
│ - Id            │
│ - EventTypeId   │
│ - Format        │
│ - Content       │
└─────────────────┘

┌─────────────────┐
│  MockEndpoint   │
│                 │
│ - Id            │
│ - Url           │
│ - Configuration │
└─────────────────┘
```

---

## Core Entities

### 1. Subscriber

Represents an organization or user that sends and/or receives webhooks.

**Fields**:

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, Required | Unique identifier |
| Name | string | Required, MaxLength(200) | Display name |
| Email | string | Required, EmailAddress, MaxLength(320) | Contact email |
| ApiKey | string | Required, Unique, MaxLength(64) | Authentication key (SHA256 hash) |
| ApiKeyHash | string | Required, MaxLength(128) | Hashed API key for secure storage |
| CreatedAt | DateTime | Required | Account creation timestamp |
| UpdatedAt | DateTime | Required | Last modification timestamp |
| IsActive | bool | Required, Default(true) | Account status |
| RetentionDays | int | Required, Range(1, 3650), Default(90) | Webhook log retention period |

**Relationships**:
- Has many `Subscription` (1:N)
- Has many `WebhookEvent` (1:N) - webhooks sent by this subscriber

**Validation Rules**:
- Email must be unique across subscribers
- ApiKey must be cryptographically secure (32 bytes, base64-encoded)
- RetentionDays cannot exceed 10 years (regulatory requirement)

**Indexes**:
- Unique index on `Email`
- Unique index on `ApiKeyHash`

---

### 2. EventType

Defines a category of webhook events with optional schema validation.

**Fields**:

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, Required | Unique identifier |
| Name | string | Required, Unique, MaxLength(200) | Event type name (e.g., "order.created") |
| Description | string | MaxLength(1000) | Human-readable description |
| Version | string | Required, MaxLength(20) | Semantic version (e.g., "1.0.0") |
| SubscriberId | Guid | FK, Required | Owner of this event type |
| IsActive | bool | Required, Default(true) | Whether event type accepts new webhooks |
| CreatedAt | DateTime | Required | Creation timestamp |
| UpdatedAt | DateTime | Required | Last modification timestamp |

**Relationships**:
- Belongs to `Subscriber` (N:1)
- Has one `SchemaDefinition` (1:1, optional)
- Has many `WebhookEvent` (1:N)
- Has many `Subscription` (1:N)

**Validation Rules**:
- Name must follow dot-notation format (e.g., `resource.action`, `order.created`)
- Name must be lowercase alphanumeric with dots and underscores only
- Version must follow semantic versioning (MAJOR.MINOR.PATCH)
- Combination of (Name, Version, SubscriberId) must be unique

**Indexes**:
- Unique index on `(Name, Version, SubscriberId)`
- Index on `SubscriberId`

---

### 3. SchemaDefinition

Stores schema definitions for validating webhook payloads.

**Fields**:

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, Required | Unique identifier |
| EventTypeId | Guid | FK, Required, Unique | Associated event type |
| Format | SchemaFormat | Required | Schema format (enum) |
| Content | string | Required | Schema content (JSON/Avro/Protobuf/Assembly) |
| ContentHash | string | Required, MaxLength(64) | SHA256 hash for change detection |
| CreatedAt | DateTime | Required | Schema creation timestamp |

**Enums**:
```csharp
public enum SchemaFormat {
    JsonSchema = 0,
    Avro = 1,
    Protobuf = 2,
    DotNetAssembly = 3
}
```

**Relationships**:
- Belongs to `EventType` (1:1)

**Validation Rules**:
- Content must be valid for the specified Format
- JSON Schema: Valid JSON Schema Draft 7 or later
- Avro: Valid Avro schema JSON
- Protobuf: Valid .proto file content
- DotNetAssembly: Base64-encoded assembly bytes

**Indexes**:
- Unique index on `EventTypeId`
- Index on `ContentHash` (for detecting duplicate schemas)

---

### 4. WebhookEvent

Represents a business event to be delivered as a webhook.

**Fields**:

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, Required | Unique identifier |
| EventTypeId | Guid | FK, Required | Type of event |
| SubscriberId | Guid | FK, Required | Sender of webhook |
| Payload | string | Required | JSON payload (stored encrypted) |
| PayloadHash | string | Required, MaxLength(64) | SHA256 hash of payload |
| PayloadSizeBytes | int | Required, Range(0, 1048576) | Payload size (max 1MB) |
| TraceId | string | Required, MaxLength(32) | Distributed trace ID |
| CreatedAt | DateTime | Required | Event receipt timestamp |
| ScheduledFor | DateTime | Nullable | Scheduled delivery time (null = immediate) |
| ExpiresAt | DateTime | Required | Retention expiry date |
| Metadata | string | Nullable | Additional key-value pairs (JSON) |

**Relationships**:
- Belongs to `EventType` (N:1)
- Belongs to `Subscriber` (N:1)
- Has many `DeliveryAttempt` (1:N)

**Validation Rules**:
- Payload must be valid JSON
- PayloadSizeBytes must match actual payload byte length
- ExpiresAt = CreatedAt + Subscriber.RetentionDays
- Payload validated against SchemaDefinition if present
- TraceId must be valid W3C trace ID format

**Indexes**:
- Index on `EventTypeId`
- Index on `SubscriberId`
- Index on `CreatedAt` (for purging expired events)
- Index on `ScheduledFor` (for delivery scheduling)
- Composite index on `(SubscriberId, CreatedAt)` (for subscriber queries)

**Encryption**:
- `Payload` encrypted at rest using AES-256-GCM
- Encryption key rotated periodically (Key Vault integration)

---

### 5. Subscription

Represents a subscriber's registration to receive webhooks for a specific event type.

**Fields**:

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, Required | Unique identifier |
| SubscriberId | Guid | FK, Required | Webhook recipient |
| EventTypeId | Guid | FK, Required | Event type to subscribe to |
| EndpointUrl | string | Required, MaxLength(2000), Url | Delivery endpoint |
| Secret | string | Required, MaxLength(256) | HMAC signature secret (encrypted) |
| IsActive | bool | Required, Default(true) | Subscription status |
| FilterExpression | string | Nullable, MaxLength(500) | JSONPath filter expression |
| CustomHeaders | string | Nullable | JSON map of custom headers (encrypted) |
| AuthType | AuthType | Required | Authentication type |
| AuthConfig | string | Nullable | Auth configuration (encrypted JSON) |
| CreatedAt | DateTime | Required | Subscription creation timestamp |
| UpdatedAt | DateTime | Required | Last modification timestamp |
| LastDeliveryAt | DateTime | Nullable | Last successful delivery timestamp |

**Enums**:
```csharp
public enum AuthType {
    None = 0,
    CustomHeaders = 1,
    BasicAuth = 2,
    BearerToken = 3
}
```

**Relationships**:
- Belongs to `Subscriber` (N:1)
- Belongs to `EventType` (N:1)
- Has many `DeliveryAttempt` (1:N)

**Validation Rules**:
- EndpointUrl must be HTTPS (HTTP allowed only in development)
- EndpointUrl must not be internal/private IP (SSRF prevention)
- Blocked IPs: 127.0.0.1, localhost, 169.254.x.x, 10.x.x.x, 172.16.x.x-172.31.x.x, 192.168.x.x
- FilterExpression must be valid JSONPath syntax
- Secret minimum length: 32 characters
- CustomHeaders max size: 4KB

**Indexes**:
- Index on `SubscriberId`
- Index on `EventTypeId`
- Index on `IsActive`
- Composite index on `(EventTypeId, IsActive)` (for active subscriptions lookup)

**Encryption**:
- `Secret`, `CustomHeaders`, `AuthConfig` encrypted at rest

---

### 6. DeliveryAttempt

Records individual webhook delivery attempts with full request/response details.

**Fields**:

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, Required | Unique identifier |
| WebhookEventId | Guid | FK, Required | Associated webhook event |
| SubscriptionId | Guid | FK, Required | Target subscription |
| AttemptNumber | int | Required, Range(1, 10) | Attempt sequence number |
| Status | DeliveryStatus | Required | Delivery outcome |
| StartedAt | DateTime | Required | Attempt start timestamp |
| CompletedAt | DateTime | Nullable | Attempt completion timestamp |
| DurationMs | int | Nullable | Duration in milliseconds |
| RequestHeaders | string | Nullable | HTTP request headers (JSON) |
| RequestBody | string | Nullable | HTTP request body (same as payload) |
| ResponseStatus | int | Nullable | HTTP response status code |
| ResponseHeaders | string | Nullable | HTTP response headers (JSON) |
| ResponseBody | string | Nullable, MaxLength(10000) | HTTP response body (truncated) |
| ErrorMessage | string | Nullable, MaxLength(1000) | Error description |
| Signature | string | Required, MaxLength(128) | HMAC signature sent |
| TraceId | string | Required, MaxLength(32) | Distributed trace ID |
| NextRetryAt | DateTime | Nullable | Scheduled retry time |

**Enums**:
```csharp
public enum DeliveryStatus {
    Pending = 0,
    Delivering = 1,
    Delivered = 2,      // 2xx response
    Failed = 3,         // 4xx/5xx response
    Timeout = 4,        // Request timeout
    CircuitOpen = 5,    // Circuit breaker open
    Rejected = 6,       // SSRF or validation failure
    DeadLetter = 7      // Exhausted all retries
}
```

**Relationships**:
- Belongs to `WebhookEvent` (N:1)
- Belongs to `Subscription` (N:1)

**Validation Rules**:
- AttemptNumber must be sequential (1, 2, 3, ...)
- DurationMs = CompletedAt - StartedAt (in milliseconds)
- Status must be Delivered if ResponseStatus in 200-299 range
- Status must be Failed if ResponseStatus >= 300
- ResponseBody truncated to 10KB to prevent excessive storage

**Indexes**:
- Index on `WebhookEventId`
- Index on `SubscriptionId`
- Index on `Status`
- Index on `StartedAt`
- Composite index on `(WebhookEventId, AttemptNumber)` (for attempt history)
- Composite index on `(Status, NextRetryAt)` (for retry scheduling)

**Retention**:
- DeliveryAttempts purged when parent WebhookEvent expires
- Anonymized metadata retained for analytics (no PII)

---

### 7. MockEndpoint

Temporary webhook endpoints for testing and development.

**Fields**:

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, Required | Unique identifier |
| SubscriberId | Guid | FK, Required | Owner of mock endpoint |
| Url | string | Required, Unique, MaxLength(2000) | Mock endpoint URL |
| Name | string | Required, MaxLength(200) | Display name |
| ResponseStatus | int | Required, Default(200) | Simulated response status |
| ResponseBody | string | Nullable, MaxLength(10000) | Simulated response body |
| ResponseDelay | int | Required, Default(0), Range(0, 30000) | Simulated delay (ms) |
| IsActive | bool | Required, Default(true) | Mock endpoint status |
| CreatedAt | DateTime | Required | Creation timestamp |
| ExpiresAt | DateTime | Required | Auto-deletion time (7 days default) |
| RequestCount | int | Required, Default(0) | Number of requests received |

**Relationships**:
- Belongs to `Subscriber` (N:1)

**Validation Rules**:
- Url must be unique globally (prevents collisions)
- Url format: `/mock/{guid}` (auto-generated)
- ResponseStatus must be valid HTTP status code (100-599)
- ResponseDelay cannot exceed 30 seconds (prevent abuse)
- ExpiresAt cannot exceed 30 days from creation

**Indexes**:
- Unique index on `Url`
- Index on `SubscriberId`
- Index on `ExpiresAt` (for cleanup job)

**Cleanup**:
- Background job deletes expired mock endpoints daily

---

## Supporting Entities

### 8. DeadLetterQueue

Stores webhooks that failed all retry attempts for manual investigation.

**Fields**:

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, Required | Unique identifier |
| WebhookEventId | Guid | FK, Required | Failed webhook event |
| SubscriptionId | Guid | FK, Required | Target subscription |
| FailureReason | string | Required, MaxLength(2000) | Reason for failure |
| LastAttemptId | Guid | FK, Required | Last delivery attempt |
| CreatedAt | DateTime | Required | Dead letter timestamp |
| ResolvedAt | DateTime | Nullable | Resolution timestamp |
| Resolution | string | Nullable, MaxLength(500) | Resolution action taken |

**Relationships**:
- Belongs to `WebhookEvent` (N:1)
- Belongs to `Subscription` (N:1)
- References `DeliveryAttempt` (N:1)

**Indexes**:
- Index on `CreatedAt`
- Index on `ResolvedAt` (for filtering unresolved)
- Index on `SubscriptionId`

---

### 9. GdprRequest

Tracks GDPR data export and deletion requests.

**Fields**:

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, Required | Unique identifier |
| SubscriberId | Guid | FK, Required | Subject of request |
| RequestType | GdprRequestType | Required | Type of request |
| Status | GdprRequestStatus | Required | Processing status |
| RequestedAt | DateTime | Required | Request creation time |
| CompletedAt | DateTime | Nullable | Completion timestamp |
| ExportFileUrl | string | Nullable, MaxLength(2000) | Export download link (signed URL) |
| Notes | string | Nullable, MaxLength(2000) | Additional notes |

**Enums**:
```csharp
public enum GdprRequestType {
    DataExport = 0,
    DataDeletion = 1
}

public enum GdprRequestStatus {
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}
```

**Relationships**:
- Belongs to `Subscriber` (N:1)

**Indexes**:
- Index on `SubscriberId`
- Index on `Status`
- Composite index on `(RequestType, Status)`

---

## Value Objects

### 1. DeliveryPolicy

Configuration for retry and circuit breaker policies (stored in Subscription).

```csharp
public class DeliveryPolicy {
    public int MaxRetries { get; set; } = 5;
    public int[] RetryDelaysSeconds { get; set; } = { 1, 5, 25, 120, 600 };
    public int CircuitBreakerThreshold { get; set; } = 5;
    public int CircuitBreakerDurationSeconds { get; set; } = 60;
    public int TimeoutSeconds { get; set; } = 30;
}
```

---

### 2. EventMetadata

Arbitrary key-value pairs attached to webhook events.

```csharp
public class EventMetadata : Dictionary<string, string> {
    public const int MaxKeyLength = 100;
    public const int MaxValueLength = 1000;
    public const int MaxPairs = 20;
}
```

---

## Database Constraints

### Cascading Deletes

- **Subscriber deleted** → Soft delete (IsActive = false), retain data for retention period
- **EventType deleted** → Prevent if active subscriptions exist
- **Subscription deleted** → Preserve DeliveryAttempts for audit trail
- **WebhookEvent expired** → Cascade delete DeliveryAttempts

### Row-Level Security

- Subscribers can only access their own data
- Admin users can access all data
- Implemented via EF Core query filters

---

## Database Migrations Strategy

**Versioning**: Use EF Core migrations with timestamp prefixes (e.g., `20251018120000_InitialCreate`)

**Backwards Compatibility**:
- Additive changes (new columns, tables) are backwards compatible
- Breaking changes (column removal, type changes) require multi-phase deployment:
  1. Add new column, keep old column
  2. Dual-write to both columns
  3. Migrate data
  4. Remove old column

**Migration Testing**:
- Test migrations against production-like database
- Verify rollback procedures
- Measure migration duration (must complete in <5 minutes)

---

## Data Validation Summary

**API-Level Validation** (ASP.NET Core Data Annotations):
- Required fields, string lengths, email formats, URL formats
- Range checks, regex patterns
- Custom validation attributes for complex rules

**Domain-Level Validation** (Domain entity methods):
- Business rule enforcement (e.g., webhook payload size, retention limits)
- Cross-entity validation (e.g., subscription must reference active event type)

**Database-Level Validation** (SQL constraints):
- Primary keys, foreign keys, unique indexes
- Check constraints (e.g., retry count < 10)
- NOT NULL constraints

---

## Performance Considerations

**Indexing Strategy**:
- Cover common query patterns (subscriber lookups, event type filters, date ranges)
- Composite indexes for multi-column filters
- Avoid over-indexing (trade-off: insert performance vs. query performance)

**Partitioning**:
- Consider table partitioning for `WebhookEvent` and `DeliveryAttempt` by date range (monthly)
- Improves query performance and simplifies data retention/purging

**Archival**:
- Move expired events to cold storage (e.g., Azure Blob, S3) before deletion
- Retain anonymized analytics data indefinitely

---

## Conclusion

This data model supports all functional requirements from the specification:
- ✅ Webhook submission and delivery tracking
- ✅ Multi-format schema validation
- ✅ Subscription management with filtering
- ✅ Comprehensive delivery attempt logging
- ✅ Mock endpoints for testing
- ✅ GDPR compliance (export, deletion, retention)
- ✅ Dead-letter handling
- ✅ Observability (trace IDs, metadata)

The model follows .NET/EF Core conventions, uses appropriate data types, includes comprehensive validation, and optimizes for query performance through strategic indexing.

**Next Steps**: Generate OpenAPI contracts based on these entities.
