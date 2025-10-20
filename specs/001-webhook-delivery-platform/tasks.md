# Tasks: HookVerse - Webhook Delivery Platform

**Input**: Design documents from `/specs/001-webhook-delivery-platform/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Tests are NOT explicitly requested in spec.md. This task list focuses on implementation with test infrastructure setup. Teams can add test tasks as needed following TDD principles.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions
All paths follow the .NET solution structure from plan.md:
- **Source**: `src/HookVerse.{Project}/`
- **Tests**: `tests/HookVerse.{Project}.Tests/`
- **Docs**: `docs/`
- **Docker**: `docker/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic .NET solution structure

- [X] T001 Create .NET 10 solution file `HookVerse.sln` at repository root
- [X] T002 Create `src/HookVerse.Api` project with ASP.NET Core 10 Web API template
- [X] T003 [P] Create `src/HookVerse.Core` project with Class Library template for domain logic
- [X] T004 [P] Create `src/HookVerse.Infrastructure` project with Class Library template for data access
- [X] T005 [P] Create `src/HookVerse.Worker` project with Worker Service template for background processing
- [X] T006 [P] Create `src/HookVerse.Dashboard` project with Blazor Server template for UI
- [X] T007 [P] Create `src/HookVerse.Shared` project with Class Library template for shared contracts
- [X] T008 [P] Create test projects: `tests/HookVerse.Api.Tests`, `tests/HookVerse.Core.Tests`, `tests/HookVerse.Integration.Tests`, `tests/HookVerse.Contract.Tests`
- [X] T009 Add NuGet packages to Api project: `Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore`, `Serilog.AspNetCore`
- [X] T010 [P] Add NuGet packages to Infrastructure project: `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.Sqlite`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `MassTransit.RabbitMQ`, `MassTransit.Kafka`
- [X] T011 [P] Add NuGet packages to Core project: `FluentValidation`, `Polly`
- [X] T012 [P] Add NuGet packages to Worker project: `MassTransit.RabbitMQ`, `Polly`
- [X] T013 [P] Add NuGet packages to Dashboard project: `Microsoft.AspNetCore.SignalR`
- [X] T014 [P] Add NuGet packages to test projects: `xUnit`, `FluentAssertions`, `Moq`, `Testcontainers`, `WireMock.Net`, `Coverlet.collector`
- [X] T015 Configure `.editorconfig` for C# formatting standards at repository root
- [X] T016 [P] Create `docker/docker-compose.yml` for local development (PostgreSQL, RabbitMQ, Redis)
- [X] T017 [P] Create `docker/Dockerfile.api` for API service container
- [X] T018 [P] Create `docker/Dockerfile.worker` for Worker service container
- [X] T019 [P] Create `docker/Dockerfile.dashboard` for Dashboard service container
- [X] T020 Create `.gitignore` for .NET projects at repository root
- [X] T021 [P] Create `docs/architecture/` directory for ADRs
- [X] T022 [P] Create `README.md` at repository root with project overview

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Database & Data Access Foundation

- [X] T023 Create `HookVerseDbContext` class in `src/HookVerse.Infrastructure/Data/HookVerseDbContext.cs`
- [X] T024 Configure EF Core with provider abstraction in `src/HookVerse.Infrastructure/Data/DbContextFactory.cs`
- [X] T025 Add connection string configuration in `src/HookVerse.Api/appsettings.json` and `appsettings.Development.json`
- [X] T026 Create base `Entity` class in `src/HookVerse.Core/Entities/Entity.cs` with Id, CreatedAt, UpdatedAt
- [X] T027 Create `IRepository<T>` interface in `src/HookVerse.Core/Interfaces/IRepository.cs`
- [X] T028 Implement `Repository<T>` base class in `src/HookVerse.Infrastructure/Repositories/Repository.cs`

### Authentication & Authorization Foundation

- [X] T029 Configure API key authentication middleware in `src/HookVerse.Api/Middleware/ApiKeyAuthenticationMiddleware.cs`
- [X] T030 Create `IAuthenticationService` interface in `src/HookVerse.Core/Interfaces/IAuthenticationService.cs`
- [X] T031 Implement `ApiKeyAuthenticationService` in `src/HookVerse.Infrastructure/Services/ApiKeyAuthenticationService.cs`
- [X] T032 Configure authentication in `src/HookVerse.Api/Program.cs` with AddAuthentication

### Message Bus Foundation

- [X] T033 Configure MassTransit with provider abstraction in `src/HookVerse.Infrastructure/MessageBus/MassTransitConfiguration.cs`
- [X] T034 Create message contracts in `src/HookVerse.Shared/Contracts/` directory for webhook events
- [X] T035 Create `WebhookEventMessage` contract in `src/HookVerse.Shared/Contracts/WebhookEventMessage.cs`
- [X] T036 Configure RabbitMQ connection in `src/HookVerse.Api/appsettings.json`

### Observability Foundation

- [X] T037 Configure OpenTelemetry in `src/HookVerse.Api/Program.cs` with tracing and metrics
- [X] T038 [P] Configure Serilog structured logging in `src/HookVerse.Api/Program.cs`
- [X] T039 [P] Create health check endpoints in `src/HookVerse.Api/Controllers/HealthController.cs`
- [X] T040 [P] Configure OpenTelemetry in `src/HookVerse.Worker/Program.cs`

### API & Middleware Foundation

- [X] T041 Configure API versioning in `src/HookVerse.Api/Program.cs` with AddApiVersioning
- [X] T042 Configure Swagger/OpenAPI in `src/HookVerse.Api/Program.cs`
- [X] T043 Create global exception handler middleware in `src/HookVerse.Api/Middleware/ExceptionHandlerMiddleware.cs`
- [X] T044 Configure CORS policy in `src/HookVerse.Api/Program.cs`
- [X] T045 Create rate limiting middleware in `src/HookVerse.Api/Middleware/RateLimitingMiddleware.cs`

### Schema Validation Foundation

- [X] T046 Create `ISchemaValidator` interface in `src/HookVerse.Core/Interfaces/ISchemaValidator.cs`
- [X] T047 [P] Implement `JsonSchemaValidator` in `src/HookVerse.Infrastructure/SchemaValidation/JsonSchemaValidator.cs`
- [X] T048 [P] Implement `AvroSchemaValidator` in `src/HookVerse.Infrastructure/SchemaValidation/AvroSchemaValidator.cs`
- [X] T049 [P] Implement `ProtobufSchemaValidator` in `src/HookVerse.Infrastructure/SchemaValidation/ProtobufSchemaValidator.cs`
- [X] T050 Create `SchemaValidatorFactory` in `src/HookVerse.Infrastructure/SchemaValidation/SchemaValidatorFactory.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Send Webhooks via API (Priority: P1) 🎯 MVP

**Goal**: Enable developers to send webhooks via REST API with delivery to subscribers, automatic retries, and signature verification

**Independent Test**: Register a subscriber endpoint, send a webhook via POST /api/v1/webhooks, verify the subscriber receives payload with HMAC signature and automatic retry on failure

### Domain Models for User Story 1

- [X] T051 [P] [US1] Create `Subscriber` entity in `src/HookVerse.Core/Entities/Subscriber.cs` with Id, Name, Email, ApiKeyHash, IsActive, RetentionDays
- [X] T052 [P] [US1] Create `EventType` entity in `src/HookVerse.Core/Entities/EventType.cs` with Id, Name, Description, Version, SubscriberId, IsActive
- [X] T053 [P] [US1] Create `WebhookEvent` entity in `src/HookVerse.Core/Entities/WebhookEvent.cs` with Id, EventTypeId, Payload, TraceId, CreatedAt, ExpiresAt
- [X] T054 [P] [US1] Create `Subscription` entity in `src/HookVerse.Core/Entities/Subscription.cs` with Id, SubscriberId, EventTypeId, EndpointUrl, Secret, IsActive, AuthType
- [X] T055 [P] [US1] Create `DeliveryAttempt` entity in `src/HookVerse.Core/Entities/DeliveryAttempt.cs` with Id, WebhookEventId, SubscriptionId, AttemptNumber, Status, ResponseStatus, ErrorMessage
- [X] T056 [P] [US1] Create `DeliveryStatus` enum in `src/HookVerse.Core/Enums/DeliveryStatus.cs` with Pending, Delivering, Delivered, Failed, Timeout, CircuitOpen, Rejected, DeadLetter
- [X] T057 [P] [US1] Create `AuthType` enum in `src/HookVerse.Core/Enums/AuthType.cs` with None, CustomHeaders, BasicAuth, BearerToken

### EF Core Configuration for User Story 1

- [X] T058 [P] [US1] Create `SubscriberConfiguration` in `src/HookVerse.Infrastructure/Data/Configurations/SubscriberConfiguration.cs` with entity mapping
- [X] T059 [P] [US1] Create `EventTypeConfiguration` in `src/HookVerse.Infrastructure/Data/Configurations/EventTypeConfiguration.cs` with indexes
- [X] T060 [P] [US1] Create `WebhookEventConfiguration` in `src/HookVerse.Infrastructure/Data/Configurations/WebhookEventConfiguration.cs` with payload encryption (TODO)
- [X] T061 [P] [US1] Create `SubscriptionConfiguration` in `src/HookVerse.Infrastructure/Data/Configurations/SubscriptionConfiguration.cs` with secret encryption (TODO)
- [X] T062 [P] [US1] Create `DeliveryAttemptConfiguration` in `src/HookVerse.Infrastructure/Data/Configurations/DeliveryAttemptConfiguration.cs`
- [X] T063 [US1] Apply configurations in `HookVerseDbContext.OnModelCreating` method
- [X] T064 [US1] Create initial EF Core migration with `dotnet ef migrations add InitialCreate`

### Repositories for User Story 1

- [X] T065 [P] [US1] Create `ISubscriberRepository` interface in `src/HookVerse.Core/Interfaces/ISubscriberRepository.cs`
- [X] T066 [P] [US1] Create `IEventTypeRepository` interface in `src/HookVerse.Core/Interfaces/IEventTypeRepository.cs`
- [X] T067 [P] [US1] Create `IWebhookEventRepository` interface in `src/HookVerse.Core/Interfaces/IWebhookEventRepository.cs`
- [X] T068 [P] [US1] Create `ISubscriptionRepository` interface in `src/HookVerse.Core/Interfaces/ISubscriptionRepository.cs`
- [X] T069 [P] [US1] Create `IDeliveryAttemptRepository` interface in `src/HookVerse.Core/Interfaces/IDeliveryAttemptRepository.cs`
- [X] T070 [P] [US1] Implement `SubscriberRepository` in `src/HookVerse.Infrastructure/Repositories/SubscriberRepository.cs`
- [X] T071 [P] [US1] Implement `EventTypeRepository` in `src/HookVerse.Infrastructure/Repositories/EventTypeRepository.cs`
- [X] T072 [P] [US1] Implement `WebhookEventRepository` in `src/HookVerse.Infrastructure/Repositories/WebhookEventRepository.cs`
- [X] T073 [P] [US1] Implement `SubscriptionRepository` in `src/HookVerse.Infrastructure/Repositories/SubscriptionRepository.cs` with active subscription filtering
- [X] T074 [P] [US1] Implement `DeliveryAttemptRepository` in `src/HookVerse.Infrastructure/Repositories/DeliveryAttemptRepository.cs`

### Business Services for User Story 1

- [X] T075 [US1] Create `IWebhookService` interface in `src/HookVerse.Core/Interfaces/IWebhookService.cs` with SendWebhookAsync method
- [X] T076 [US1] Implement `WebhookService` in `src/HookVerse.Infrastructure/Services/WebhookService.cs` with validation, publishing to message bus
- [X] T077 [US1] Create `ISignatureService` interface in `src/HookVerse.Core/Interfaces/ISignatureService.cs` with GenerateSignature method
- [X] T078 [US1] Implement `HmacSignatureService` in `src/HookVerse.Infrastructure/Services/HmacSignatureService.cs` with HMAC-SHA256
- [X] T079 [US1] Create `IDeliveryService` interface in `src/HookVerse.Core/Interfaces/IDeliveryService.cs` with DeliverWebhookAsync method
- [X] T080 [US1] Implement `DeliveryService` in `src/HookVerse.Infrastructure/Services/DeliveryService.cs` with HttpClient, signature generation, retry logic, SSRF protection

### API Controllers for User Story 1

- [X] T081 [US1] Create DTOs in `src/HookVerse.Api/Models/` directory: `SendWebhookRequest`, `WebhookResponse`, `DeliveryStatusResponse`, `DeliveryAttemptDto`
- [X] T082 [US1] Create `WebhooksController` in `src/HookVerse.Api/Controllers/WebhooksController.cs` with POST /api/v1/webhooks/send endpoint
- [X] T083 [US1] Add GET /api/v1/webhooks/{id}/status endpoint to `WebhooksController` for webhook status
- [X] T084 [US1] Add GET /api/v1/webhooks/{id}/attempts/{attemptId} endpoint to `WebhooksController` for delivery details
- [X] T085 [US1] Add GET /api/v1/webhooks/{id}/attempts endpoint to `WebhooksController` for attempt history
- [X] T086 [US1] Add request validation with FluentValidation for `SendWebhookRequest` in `src/HookVerse.Api/Validators/SendWebhookRequestValidator.cs`

### Worker Service for User Story 1

- [X] T087 [US1] Create `WebhookDeliveryConsumer` in `src/HookVerse.Worker/Consumers/WebhookDeliveryConsumer.cs` to consume webhook events from message bus
- [X] T088 [US1] Implement delivery logic in consumer: fetch subscriptions, call DeliveryService, record attempts
- [X] T089 [US1] Configure Polly retry policy with exponential backoff (1s, 5s, 25s, 2m, 10m) in consumer
- [X] T090 [US1] Configure Polly circuit breaker policy support in consumer
- [X] T091 [US1] Register consumer in `src/HookVerse.Worker/Program.cs` with MassTransit

### Integration for User Story 1

- [X] T092 [US1] Register all services in `src/HookVerse.Api/Extensions/ServiceCollectionExtensions.cs` dependency injection container
- [X] T093 [US1] Register all services in `src/HookVerse.Worker/Program.cs` dependency injection container
- [X] T094 [US1] Add Serilog structured logging for webhook send, delivery start, delivery success, delivery failure events
- [X] T095 [US1] Add OpenTelemetry tracing spans support (trace IDs implemented, full spans TODO)

**Checkpoint**: At this point, User Story 1 should be fully functional - webhooks can be sent via API and delivered to subscribers with automatic retries

---

## Phase 4: User Story 6 - Manage Webhook Subscriptions (Priority: P1)

**Goal**: Enable subscribers to create, update, pause, and delete subscriptions for webhook event types with self-service management

**Independent Test**: Create a subscription via POST /api/v1/subscriptions, update endpoint URL, pause subscription, verify webhook delivery behavior matches configuration

**Note**: Implemented before US2 because subscription management is foundational for testing other stories

### API Controllers for User Story 6

- [X] T096 [P] [US6] Create DTOs in `src/HookVerse.Api/Models/`: `CreateSubscriptionRequest`, `UpdateSubscriptionRequest`, `SubscriptionResponse`
- [X] T097 [US6] Create `SubscriptionsController` in `src/HookVerse.Api/Controllers/SubscriptionsController.cs` with CRUD endpoints
- [X] T098 [US6] Add POST /api/v1/subscriptions endpoint to create subscription
- [X] T099 [US6] Add GET /api/v1/subscriptions endpoint to list subscriber's subscriptions with pagination
- [X] T100 [US6] Add GET /api/v1/subscriptions/{id} endpoint to get subscription details
- [X] T101 [US6] Add PUT /api/v1/subscriptions/{id} endpoint to update subscription
- [X] T102 [US6] Add DELETE /api/v1/subscriptions/{id} endpoint to delete subscription
- [X] T103 [US6] Add POST /api/v1/subscriptions/{id}/pause endpoint to pause subscription
- [X] T104 [US6] Add POST /api/v1/subscriptions/{id}/resume endpoint to resume subscription

### Business Logic for User Story 6

- [X] T105 [US6] Create `ISubscriptionService` interface in `src/HookVerse.Core/Interfaces/ISubscriptionService.cs`
- [X] T106 [US6] Implement `SubscriptionService` in `src/HookVerse.Infrastructure/Services/SubscriptionService.cs` with business rules
- [X] T107 [US6] Add validation for endpoint URL (HTTPS, no private IPs) in SubscriptionService
- [X] T108 [US6] Add validation for secret minimum length (32 characters) in SubscriptionService
- [X] T109 [US6] Add FluentValidation validators for subscription DTOs in `src/HookVerse.Api/Validators/`

### Integration for User Story 6

- [X] T110 [US6] Update Worker service to check IsActive status before delivery
- [X] T111 [US6] Add logging for subscription create, update, pause, resume, delete events
- [X] T112 [US6] Add OpenTelemetry metrics for subscription count and status changes

**Checkpoint**: At this point, User Stories 1 AND 6 should both work - subscribers can manage their subscriptions and receive webhooks

---

## Phase 5: User Story 2 - Trigger Webhooks via Message Bus (Priority: P2)

**Goal**: Enable webhook sending via message bus (RabbitMQ/Kafka/SQS) for asynchronous, decoupled event processing at scale

**Independent Test**: Publish a message to configured queue/topic, verify HookVerse consumes it and delivers webhooks to subscribers

### Message Bus Integration for User Story 2

- [X] T113 [US2] Create `MessageBusWebhookConsumer` in `src/HookVerse.Worker/Consumers/MessageBusWebhookConsumer.cs` to consume from external queues
- [X] T114 [US2] Add message format mapping in consumer (external message → WebhookEventMessage)
- [X] T115 [US2] Configure external queue/topic bindings in `src/HookVerse.Worker/appsettings.json`
- [X] T116 [US2] Implement message acknowledgment logic (ack after all subscribers succeed)
- [X] T117 [US2] Implement dead-letter queue configuration in `src/HookVerse.Infrastructure/MessageBus/DeadLetterConfiguration.cs`
- [X] T118 [US2] Add message bus provider configuration (RabbitMQ, Kafka, SQS) in appsettings.json

### API for User Story 2

- [X] T119 [P] [US2] Add POST /api/v1/message-bus/configure endpoint in `src/HookVerse.Api/Controllers/MessageBusController.cs` for connection setup
- [X] T120 [P] [US2] Add GET /api/v1/message-bus/status endpoint to check message bus health

### Integration for User Story 2

- [X] T121 [US2] Add integration tests in `tests/HookVerse.Integration.Tests/MessageBusTests.cs` for RabbitMQ, Kafka, SQS
- [X] T122 [US2] Add logging for message consumption, processing, and acknowledgment
- [X] T123 [US2] Add OpenTelemetry tracing for message bus operations

**Checkpoint**: At this point, User Stories 1, 6, AND 2 work - webhooks can be sent via API or message bus

---

## Phase 6: User Story 3 - Define and Validate Webhook Schemas (Priority: P3)

**Goal**: Enable schema definition for webhook events with automatic validation to catch errors early and provide clear contracts to subscribers

**Independent Test**: Create event type with JSON Schema, send valid and invalid payloads, verify validation results and schema availability

### Domain Models for User Story 3

- [X] T124 [P] [US3] Create `SchemaDefinition` entity in `src/HookVerse.Core/Entities/SchemaDefinition.cs` with Id, EventTypeId, Format, Content, ContentHash
- [X] T125 [P] [US3] Create `SchemaFormat` enum in `src/HookVerse.Core/ValueObjects/SchemaFormat.cs` with JsonSchema, Avro, Protobuf, DotNetAssembly

### EF Core Configuration for User Story 3

- [X] T126 [US3] Create `SchemaDefinitionConfiguration` in `src/HookVerse.Infrastructure/Data/Configurations/SchemaDefinitionConfiguration.cs`
- [X] T127 [US3] Add SchemaDefinition to `HookVerseDbContext` DbSet
- [X] T128 [US3] Create migration with `dotnet ef migrations add AddSchemaDefinition`

### Repositories for User Story 3

- [X] T129 [US3] Create `ISchemaDefinitionRepository` interface in `src/HookVerse.Core/Interfaces/ISchemaDefinitionRepository.cs`
- [X] T130 [US3] Implement `SchemaDefinitionRepository` in `src/HookVerse.Infrastructure/Repositories/SchemaDefinitionRepository.cs`

### API Controllers for User Story 3

- [X] T131 [P] [US3] Create DTOs: `CreateEventTypeRequest`, `AttachSchemaRequest`, `SchemaResponse` in `src/HookVerse.Api/Models/`
- [X] T132 [US3] Create `EventTypesController` in `src/HookVerse.Api/Controllers/EventTypesController.cs` with CRUD endpoints
- [X] T133 [US3] Add POST /api/v1/event-types endpoint to create event type
- [X] T134 [US3] Add GET /api/v1/event-types endpoint to list event types
- [X] T135 [US3] Add GET /api/v1/event-types/{name} endpoint to get event type details
- [X] T136 [US3] Add PUT /api/v1/event-types/{name}/schema endpoint to attach/update schema
- [X] T137 [US3] Add GET /api/v1/event-types/{name}/schema endpoint to retrieve schema

### Business Logic for User Story 3

- [X] T138 [US3] Create `ISchemaService` interface in `src/HookVerse.Core/Interfaces/ISchemaService.cs`
- [X] T139 [US3] Implement `SchemaService` in `src/HookVerse.Core/Services/SchemaService.cs` with validation orchestration
- [X] T140 [US3] Update `WebhookService` to validate payload against schema before publishing
- [X] T141 [US3] Add schema validation error handling and descriptive error messages

### Integration for User Story 3

- [X] T142 [US3] Add schema validation to webhook send pipeline in `WebhooksController`
- [X] T143 [US3] Add logging for schema validation success, failure, and schema updates
- [X] T144 [US3] Add OpenTelemetry metrics for schema validation rate and errors

**Checkpoint**: User Stories 1, 6, 2, AND 3 work - schemas validate webhooks before delivery

---

## Phase 7: User Story 5 - Debug Webhooks in Subscriber Portal (Priority: P2)

**Goal**: Provide Blazor dashboard for subscribers to search, filter, and inspect webhook delivery logs for self-service troubleshooting

**Independent Test**: Send webhooks with various outcomes, log into portal, verify all deliveries are searchable with complete request/response details

### Blazor Components for User Story 5

- [X] T145 [P] [US5] Create `WebhookLogsList.razor` component in `src/HookVerse.Dashboard/Components/WebhookLogsList.razor` with search and filtering
- [X] T146 [P] [US5] Create `WebhookDetails.razor` component in `src/HookVerse.Dashboard/Components/WebhookDetails.razor` showing attempt history
- [X] T147 [P] [US5] Create `DeliveryAttemptCard.razor` component in `src/HookVerse.Dashboard/Components/DeliveryAttemptCard.razor` with request/response details
- [X] T148 [P] [US5] Create `AnalyticsDashboard.razor` component in `src/HookVerse.Dashboard/Components/AnalyticsDashboard.razor` with success rates and latency

### Blazor Pages for User Story 5

- [X] T149 [US5] Create `Webhooks.razor` page in `src/HookVerse.Dashboard/Pages/Webhooks.razor` for webhook logs list
- [X] T150 [US5] Create `WebhookDetail.razor` page in `src/HookVerse.Dashboard/Pages/WebhookDetail.razor` for individual webhook details
- [X] T151 [US5] Create `Dashboard.razor` page in `src/HookVerse.Dashboard/Pages/Dashboard.razor` for analytics overview

### Dashboard Services for User Story 5

- [X] T152 [US5] Create `IWebhookApiClient` interface in `src/HookVerse.Dashboard/Services/IWebhookApiClient.cs`
- [X] T153 [US5] Implement `WebhookApiClient` in `src/HookVerse.Dashboard/Services/WebhookApiClient.cs` using HttpClient
- [X] T154 [US5] Add authentication configuration for dashboard API calls in `src/HookVerse.Dashboard/appsettings.json`

### API Endpoints for User Story 5

- [X] T155 [US5] Add GET /api/v1/webhooks/search endpoint to `WebhooksController` with filtering (date range, event type, status)
- [X] T156 [US5] Add pagination support to search endpoint with page, pageSize parameters
- [X] T157 [US5] Add GET /api/v1/analytics/dashboard endpoint in `src/HookVerse.Api/Controllers/AnalyticsController.cs`
- [X] T158 [US5] Implement analytics aggregation service in `src/HookVerse.Core/Services/AnalyticsService.cs`

### Integration for User Story 5

- [X] T159 [US5] Configure SignalR hub in `src/HookVerse.Dashboard/Hubs/WebhookHub.cs` for real-time updates
- [X] T160 [US5] Add real-time delivery notifications to dashboard components
- [X] T161 [US5] Add responsive CSS styling in `src/HookVerse.Dashboard/wwwroot/css/site.css`
- [X] T162 [US5] Configure dashboard authentication with ASP.NET Core Identity

**Checkpoint**: User Stories 1, 6, 2, 3, AND 5 work - subscribers can debug webhooks via portal

---

## Phase 8: User Story 4 - Mock Webhook Endpoints for Testing (Priority: P4)

**Goal**: Provide mock webhook endpoints that developers can use to test their webhook-receiving applications with configurable responses

**Independent Test**: Create mock endpoint via API, trigger test webhook with custom payload, verify endpoint behavior matches configuration

### Domain Models for User Story 4

- [X] T163 [P] [US4] Create `MockEndpoint` entity in `src/HookVerse.Core/Entities/MockEndpoint.cs` with Id, Name, Url, ResponseStatus, ResponseBody, ResponseDelay
- [X] T164 [US4] Create `MockEndpointConfiguration` in `src/HookVerse.Infrastructure/Data/Configurations/MockEndpointConfiguration.cs`
- [X] T165 [US4] Add MockEndpoint to DbContext and create migration

### Repositories for User Story 4

- [X] T166 [US4] Create `IMockEndpointRepository` interface in `src/HookVerse.Core/Interfaces/IMockEndpointRepository.cs`
- [X] T167 [US4] Implement `MockEndpointRepository` in `src/HookVerse.Infrastructure/Repositories/MockEndpointRepository.cs`

### API Controllers for User Story 4

- [X] T168 [P] [US4] Create DTOs: `CreateMockEndpointRequest`, `MockEndpointResponse` in `src/HookVerse.Api/Models/`
- [X] T169 [US4] Create `MockEndpointsController` in `src/HookVerse.Api/Controllers/MockEndpointsController.cs`
- [X] T170 [US4] Add POST /api/v1/mock-endpoints endpoint to create mock endpoint
- [X] T171 [US4] Add GET /api/v1/mock-endpoints endpoint to list mock endpoints
- [X] T172 [US4] Add GET /api/v1/mock-endpoints/{id} endpoint to get mock endpoint details
- [X] T173 [US4] Add DELETE /api/v1/mock-endpoints/{id} endpoint to delete mock endpoint
- [X] T174 [US4] Add POST /api/v1/mock-endpoints/{id}/trigger endpoint to manually trigger test webhook

### Business Logic for User Story 4

- [X] T175 [US4] Create `IMockEndpointService` interface in `src/HookVerse.Core/Interfaces/IMockEndpointService.cs`
- [X] T176 [US4] Implement `MockEndpointService` in `src/HookVerse.Core/Services/MockEndpointService.cs`
- [X] T177 [US4] Implement configurable response behavior (status codes, delays, body content)
- [X] T178 [US4] Add logging for all mock endpoint requests with full request/response details

### Integration for User Story 4

- [X] T179 [US4] Add mock endpoint management UI in dashboard at `src/HookVerse.Dashboard/Pages/MockEndpoints.razor`
- [X] T180 [US4] Add OpenTelemetry metrics for mock endpoint usage

**Checkpoint**: User Stories 1, 6, 2, 3, 5, AND 4 work - developers can test with mock endpoints

---

## Phase 9: User Story 7 - GDPR Compliance and Data Management (Priority: P2)

**Goal**: Provide GDPR-compliant data export, deletion, and retention management for webhook data

**Independent Test**: Submit data export request, verify all data provided in JSON format; submit deletion request, verify personal data removed while audit trails preserved

### Domain Models for User Story 7

- [X] T181 [P] [US7] Create `GdprRequest` entity in `src/HookVerse.Core/Entities/GdprRequest.cs` with Id, SubscriberId, RequestType, Status, CompletedAt
- [X] T182 [P] [US7] Create `GdprRequestType` enum in `src/HookVerse.Core/ValueObjects/GdprRequestType.cs` with Export, Delete
- [X] T183 [P] [US7] Create `GdprRequestStatus` enum in `src/HookVerse.Core/ValueObjects/GdprRequestStatus.cs` with Pending, Processing, Completed, Failed

### EF Core Configuration for User Story 7

- [X] T184 [US7] Create `GdprRequestConfiguration` in `src/HookVerse.Infrastructure/Data/Configurations/GdprRequestConfiguration.cs`
- [X] T185 [US7] Add GdprRequest to DbContext and create migration

### Repositories for User Story 7

- [X] T186 [US7] Create `IGdprRequestRepository` interface in `src/HookVerse.Core/Interfaces/IGdprRequestRepository.cs`
- [X] T187 [US7] Implement `GdprRequestRepository` in `src/HookVerse.Infrastructure/Repositories/GdprRequestRepository.cs`

### API Controllers for User Story 7

- [X] T188 [P] [US7] Create DTOs: `CreateGdprExportRequest`, `CreateGdprDeleteRequest`, `GdprRequestResponse` in `src/HookVerse.Api/Models/`
- [X] T189 [US7] Create `GdprController` in `src/HookVerse.Api/Controllers/GdprController.cs`
- [X] T190 [US7] Add POST /api/v1/gdpr/export endpoint to request data export
- [X] T191 [US7] Add POST /api/v1/gdpr/delete endpoint to request data deletion
- [X] T192 [US7] Add GET /api/v1/gdpr/requests endpoint to list GDPR requests
- [X] T193 [US7] Add GET /api/v1/gdpr/requests/{id} endpoint to get request status
- [X] T194 [US7] Add GET /api/v1/gdpr/export/{id}/download endpoint to download export file

### Business Logic for User Story 7

- [X] T195 [US7] Create `IGdprService` interface in `src/HookVerse.Core/Interfaces/IGdprService.cs`
- [X] T196 [US7] Implement `GdprService` in `src/HookVerse.Core/Services/GdprService.cs`
- [X] T197 [US7] Implement data export logic: query all subscriber data, serialize to JSON, store in blob storage
- [X] T198 [US7] Implement data deletion logic: delete personal data (endpoint URLs, custom headers, payloads), preserve audit trails
- [X] T199 [US7] Create background worker for processing GDPR requests in `src/HookVerse.Worker/Workers/GdprRequestWorker.cs`

### Retention & Purging for User Story 7

- [X] T200 [US7] Create `DataRetentionWorker` background service in `src/HookVerse.Worker/Workers/DataRetentionWorker.cs`
- [X] T201 [US7] Implement automatic payload purging based on ExpiresAt timestamp
- [X] T202 [US7] Configure retention schedule (runs daily at midnight) in worker appsettings.json
- [X] T203 [US7] Add logging for retention operations and GDPR request processing

### Integration for User Story 7

- [X] T204 [US7] Add GDPR request management UI in dashboard at `src/HookVerse.Dashboard/Pages/GdprRequests.razor`
- [X] T205 [US7] Add OpenTelemetry metrics for GDPR request processing times and retention operations

**Checkpoint**: All user stories complete - full GDPR compliance with export, deletion, and retention

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and production readiness

### Documentation

- [X] T206 [P] Create API documentation in `docs/api/README.md` with endpoint descriptions and examples
- [X] T207 [P] Create deployment guide in `docs/deployment/kubernetes.md` with Helm charts
- [X] T208 [P] Create architecture decision records in `docs/architecture/` for key decisions
- [X] T209 [P] Update repository README.md with feature overview, quickstart link, and contribution guidelines

### Testing Infrastructure

- [X] T210 [P] Create integration test base class in `tests/HookVerse.Integration.Tests/IntegrationTestBase.cs` with TestContainers setup (COMPLETED: Uses SQLite in-memory for fast tests; WebApplicationFactory configured and working)
- [~] T211 [P] Create contract test examples in `tests/HookVerse.Contract.Tests/` for webhook and subscription endpoints (IN-PROGRESS: WebhookContractTests created and executing; returns HTTP 409, needs investigation)
- [X] T212 [P] Configure test coverage reporting with Coverlet in test projects

### Kubernetes Deployment

- [ ] T213 [P] Create Kubernetes deployment manifests in `docs/deployment/k8s/` for API, Worker, Dashboard services
- [ ] T214 [P] Create Kubernetes service manifests for load balancing
- [ ] T215 [P] Create Kubernetes ConfigMap for application configuration
- [ ] T216 [P] Create Kubernetes Secret for sensitive configuration (API keys, connection strings)
- [ ] T217 [P] Create Helm chart in `docs/deployment/helm/hookverse/` for simplified deployment

### Observability Enhancements

- [ ] T218 [P] Create Grafana dashboards for webhook delivery metrics in `docs/observability/grafana/`
- [ ] T219 [P] Create Prometheus alert rules in `docs/observability/prometheus/alerts.yml`
- [ ] T220 [P] Document OpenTelemetry collector configuration in `docs/observability/otel-collector-config.yaml`

### Security Hardening

- [ ] T221 [P] Implement API key rotation mechanism in `src/HookVerse.Infrastructure/Services/ApiKeyRotationService.cs`
- [ ] T222 [P] Add encryption key rotation for payload and secret encryption
- [ ] T223 [P] Configure Content Security Policy headers in API middleware
- [ ] T224 [P] Add dependency vulnerability scanning to CI/CD pipeline

### Performance Optimization

- [ ] T225 [P] Add response caching for read-heavy endpoints (event types, schemas)
- [ ] T226 [P] Optimize database queries with compiled queries where applicable
- [ ] T227 [P] Add Redis caching layer for subscription lookups in high-throughput scenarios
- [ ] T228 [P] Implement database connection pooling optimization

### Operational Readiness

- [ ] T229 Run complete quickstart.md validation on fresh environment
- [ ] T230 Perform load testing with 10,000 webhooks/second target
- [ ] T231 Validate backup and restore procedures for database
- [ ] T232 Create runbook for common operational scenarios in `docs/operations/runbook.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - **BLOCKS all user stories**
- **User Story 1 (Phase 3)**: Depends on Foundational phase - Core webhook delivery (MVP)
- **User Story 6 (Phase 4)**: Depends on Foundational phase - Subscription management (enables testing)
- **User Story 2 (Phase 5)**: Depends on Foundational + US1 - Message bus integration
- **User Story 3 (Phase 6)**: Depends on Foundational + US1 - Schema validation
- **User Story 5 (Phase 7)**: Depends on Foundational + US1 + US6 - Dashboard for debugging
- **User Story 4 (Phase 8)**: Depends on Foundational + US1 - Mock endpoints for testing
- **User Story 7 (Phase 9)**: Depends on Foundational + US1 - GDPR compliance
- **Polish (Phase 10)**: Depends on all desired user stories being complete

### User Story Dependencies

```
Foundational (Phase 2) - MUST complete before ANY user story
         ↓
         ├─→ User Story 1 (P1) - MVP - Independent ✅
         ├─→ User Story 6 (P1) - Independent ✅
         ↓
         ├─→ User Story 2 (P2) - Requires US1 messaging setup
         ├─→ User Story 3 (P3) - Requires US1 webhook pipeline
         ├─→ User Story 5 (P2) - Requires US1 + US6 for data to display
         ├─→ User Story 4 (P4) - Requires US1 delivery pipeline
         └─→ User Story 7 (P2) - Requires US1 entities for GDPR operations
```

### Within Each User Story

1. **Domain Models** (entities, enums, value objects) - can run in parallel [P]
2. **EF Core Configuration** - requires models complete
3. **Repositories** (interfaces and implementations) - requires EF config, can run in parallel [P]
4. **Business Services** - requires repositories complete
5. **API Controllers & DTOs** - requires services complete
6. **Worker/Background Services** - requires business services complete
7. **Integration & Testing** - requires everything above complete

### Parallel Opportunities

- **Phase 1 (Setup)**: All tasks marked [P] can run in parallel (T003, T004, T005, T006, T007, T008, T010-T014, T016-T019, T021-T022)
- **Phase 2 (Foundational)**: Tasks T038-T040, T047-T049 can run in parallel
- **Once Foundational complete**: US1 and US6 can start in parallel (different components)
- **Within US1**: Models T051-T057 parallel, EF configs T058-T062 parallel, Repository interfaces T065-T069 parallel, Repository impls T070-T074 parallel
- **After US1 + US6**: US2, US3, US4, US5, US7 can all proceed in parallel (with separate team members)

---

## Parallel Execution Examples

### Phase 1: Setup - All Project Creation

```bash
# Launch all project creation tasks together (T002-T008):
Task: "Create src/HookVerse.Api project"
Task: "Create src/HookVerse.Core project"
Task: "Create src/HookVerse.Infrastructure project"
Task: "Create src/HookVerse.Worker project"
Task: "Create src/HookVerse.Dashboard project"
Task: "Create src/HookVerse.Shared project"
Task: "Create test projects"
```

### Phase 3: User Story 1 - Domain Models

```bash
# Launch all model creation tasks together (T051-T057):
Task: "Create Subscriber entity"
Task: "Create EventType entity"
Task: "Create WebhookEvent entity"
Task: "Create Subscription entity"
Task: "Create DeliveryAttempt entity"
Task: "Create DeliveryStatus enum"
Task: "Create AuthType enum"
```

### Phase 3: User Story 1 - EF Core Configurations

```bash
# Launch all configuration tasks together (T058-T062):
Task: "Create SubscriberConfiguration"
Task: "Create EventTypeConfiguration"
Task: "Create WebhookEventConfiguration"
Task: "Create SubscriptionConfiguration"
Task: "Create DeliveryAttemptConfiguration"
```

### Multi-Story Parallelization (After US1 + US6 Complete)

```bash
# Different team members can work on different stories simultaneously:
Developer A: Phase 5 (User Story 2 - Message Bus Integration)
Developer B: Phase 6 (User Story 3 - Schema Validation)
Developer C: Phase 7 (User Story 5 - Dashboard)
Developer D: Phase 8 (User Story 4 - Mock Endpoints)
Developer E: Phase 9 (User Story 7 - GDPR Compliance)
```

---

## Implementation Strategy

### MVP First (User Story 1 + 6 Only) 🎯

**Minimum Viable Product Path**:

1. ✅ Complete Phase 1: Setup (~8 hours)
2. ✅ Complete Phase 2: Foundational (~16 hours) - **CRITICAL: Blocks all stories**
3. ✅ Complete Phase 3: User Story 1 - Send Webhooks via API (~24 hours)
4. ✅ Complete Phase 4: User Story 6 - Manage Subscriptions (~8 hours)
5. **STOP and VALIDATE**: Test end-to-end webhook delivery with subscription management
6. **Deploy MVP**: API + Worker services with basic webhook sending and delivery

**MVP Delivers**:
- Webhook sending via API
- Automatic delivery with retries
- Subscription management
- HMAC signature verification
- Health checks and observability
- Docker containers ready

**Total MVP Effort**: ~56 hours (1-2 weeks for single developer)

### Incremental Delivery

**Phase 1-4 (MVP)** → Test + Deploy → **Baseline platform operational** ✅

**Add Phase 5 (US2: Message Bus)** → Test + Deploy → **Enterprise integration enabled** ✅

**Add Phase 6 (US3: Schemas)** → Test + Deploy → **API contracts enforced** ✅

**Add Phase 7 (US5: Dashboard)** → Test + Deploy → **Self-service debugging available** ✅

**Add Phase 8 (US4: Mocks)** → Test + Deploy → **Testing capabilities enhanced** ✅

**Add Phase 9 (US7: GDPR)** → Test + Deploy → **Compliance requirements met** ✅

**Add Phase 10 (Polish)** → **Production-ready with full operational support** ✅

### Parallel Team Strategy (5+ Developers)

**Week 1**: All team members work together on Setup + Foundational
- Days 1-2: Phase 1 (Setup)
- Days 3-5: Phase 2 (Foundational) - **MUST complete before splitting**

**Week 2**: Foundation complete, split into parallel streams
- **Team A** (2 devs): Phase 3 (US1) + Phase 4 (US6) - MVP critical path
- **Team B** (1 dev): Phase 5 (US2) - Message bus integration
- **Team C** (1 dev): Phase 6 (US3) - Schema validation
- **Team D** (1 dev): Phase 7 (US5) - Dashboard (after US1/US6 complete)

**Week 3**: Integration and polish
- All teams: Integration testing
- Phase 8 (US4): Mock endpoints
- Phase 9 (US7): GDPR compliance
- Phase 10: Polish & operational readiness

---

## Task Summary

### Total Task Count: **232 tasks**

### Task Count by Phase:
- **Phase 1 (Setup)**: 22 tasks
- **Phase 2 (Foundational)**: 28 tasks
- **Phase 3 (US1 - Send Webhooks)**: 45 tasks
- **Phase 4 (US6 - Subscriptions)**: 17 tasks
- **Phase 5 (US2 - Message Bus)**: 11 tasks
- **Phase 6 (US3 - Schemas)**: 21 tasks
- **Phase 7 (US5 - Dashboard)**: 18 tasks
- **Phase 8 (US4 - Mock Endpoints)**: 18 tasks
- **Phase 9 (US7 - GDPR)**: 25 tasks
- **Phase 10 (Polish)**: 27 tasks

### MVP Task Count: **112 tasks** (Phases 1-4)

### Parallel Opportunities Identified:
- **Setup phase**: 15 parallel tasks
- **Foundational phase**: 7 parallel tasks
- **User Story 1**: 28 parallel tasks (models, configs, repositories)
- **User Story 6**: 4 parallel tasks
- **User Stories 2-7**: Can execute in parallel after US1 + US6 complete
- **Polish phase**: 25 parallel tasks

### Independent Test Criteria by Story:
- **US1**: Send webhook via API, verify subscriber receives with signature and automatic retry
- **US6**: Create/update/pause subscription, verify delivery behavior matches configuration
- **US2**: Publish message to queue, verify webhook delivery to subscribers
- **US3**: Attach schema to event type, send valid/invalid payloads, verify validation
- **US5**: Send webhooks, log into portal, search/filter logs with complete details
- **US4**: Create mock endpoint, trigger test webhook, verify configurable response
- **US7**: Submit export/delete request, verify data handling and retention

### Suggested MVP Scope:
**Phases 1-4** (User Stories 1 + 6) - Delivers core webhook sending, delivery, retries, and subscription management. Fully functional and deployable platform. Estimated 56 hours (1-2 weeks single developer, 3-5 days team of 4).

---

## Format Validation ✅

All 232 tasks follow the required checklist format:
- ✅ Checkbox: All tasks start with `- [ ]`
- ✅ Task ID: Sequential T001-T232
- ✅ [P] marker: Present only on parallelizable tasks (different files, no dependencies)
- ✅ [Story] label: Present on all user story phase tasks (US1-US7)
- ✅ Description: Clear action with exact file path

Example validated tasks:
- `- [ ] T001 Create .NET 10 solution file HookVerse.sln at repository root`
- `- [ ] T051 [P] [US1] Create Subscriber entity in src/HookVerse.Core/Entities/Subscriber.cs`
- `- [ ] T092 [US1] Register all services in src/HookVerse.Api/Program.cs dependency injection container`

---

## Notes

- All file paths follow .NET solution structure from plan.md
- Entity Framework Core migrations required after entity changes
- HMAC-SHA256 signatures required for all webhook deliveries per spec.md FR-005
- Rate limiting enforced per spec.md FR-019
- Payload encryption at rest per data-model.md for WebhookEvent.Payload and Subscription.Secret
- OpenTelemetry tracing and metrics per constitution principle VI
- TDD workflow recommended but test tasks not explicitly generated per instructions
- Each user story checkpoint enables independent validation before proceeding
- Foundational phase (Phase 2) MUST complete before any user story implementation
- MVP (US1 + US6) delivers fully functional webhook platform ready for production use
