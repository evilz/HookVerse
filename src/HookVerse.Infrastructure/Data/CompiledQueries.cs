using HookVerse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HookVerse.Infrastructure.Data;

/// <summary>
/// Compiled queries for frequently-used database operations.
/// Compiled queries are pre-compiled and cached for better performance.
/// </summary>
public static class CompiledQueries
{
    /// <summary>
    /// Get active subscriptions by tenant ID and event type.
    /// This is the most frequently executed query in the system.
    /// </summary>
    public static readonly Func<ApplicationDbContext, Guid, string, IAsyncEnumerable<Subscription>> GetActiveSubscriptionsByEventType =
        EF.CompileAsyncQuery((ApplicationDbContext context, Guid tenantId, string eventType) =>
            context.Subscriptions
                .Include(s => s.EventType)
                .Include(s => s.Filters)
                .Where(s => s.TenantId == tenantId
                    && s.EventType.Name == eventType
                    && s.IsActive
                    && !s.IsDeleted)
                .OrderBy(s => s.Priority)
                .AsNoTracking());

    /// <summary>
    /// Get all active subscriptions for a tenant.
    /// Used for subscription management and validation.
    /// </summary>
    public static readonly Func<ApplicationDbContext, Guid, IAsyncEnumerable<Subscription>> GetActiveSubscriptionsByTenant =
        EF.CompileAsyncQuery((ApplicationDbContext context, Guid tenantId) =>
            context.Subscriptions
                .Include(s => s.EventType)
                .Include(s => s.Filters)
                .Where(s => s.TenantId == tenantId
                    && s.IsActive
                    && !s.IsDeleted)
                .OrderBy(s => s.Priority)
                .AsNoTracking());

    /// <summary>
    /// Get subscription by ID with all related entities.
    /// Used for single subscription retrieval.
    /// </summary>
    public static readonly Func<ApplicationDbContext, Guid, Task<Subscription?>> GetSubscriptionById =
        EF.CompileAsyncQuery((ApplicationDbContext context, Guid id) =>
            context.Subscriptions
                .Include(s => s.EventType)
                .Include(s => s.Filters)
                .Include(s => s.Headers)
                .AsNoTracking()
                .FirstOrDefault(s => s.Id == id && !s.IsDeleted));

    /// <summary>
    /// Get subscription count by tenant.
    /// Used for quota validation and tenant statistics.
    /// </summary>
    public static readonly Func<ApplicationDbContext, Guid, Task<int>> GetSubscriptionCountByTenant =
        EF.CompileAsyncQuery((ApplicationDbContext context, Guid tenantId) =>
            context.Subscriptions
                .Count(s => s.TenantId == tenantId && !s.IsDeleted));

    /// <summary>
    /// Get event type by name.
    /// Used for event type lookups during webhook processing.
    /// </summary>
    public static readonly Func<ApplicationDbContext, string, Task<EventType?>> GetEventTypeByName =
        EF.CompileAsyncQuery((ApplicationDbContext context, string name) =>
            context.EventTypes
                .Include(e => e.Schema)
                .AsNoTracking()
                .FirstOrDefault(e => e.Name == name && !e.IsDeleted));

    /// <summary>
    /// Get all event types for a tenant.
    /// Used for event type management and validation.
    /// </summary>
    public static readonly Func<ApplicationDbContext, Guid, IAsyncEnumerable<EventType>> GetEventTypesByTenant =
        EF.CompileAsyncQuery((ApplicationDbContext context, Guid tenantId) =>
            context.EventTypes
                .Include(e => e.Schema)
                .Where(e => e.TenantId == tenantId && !e.IsDeleted)
                .OrderBy(e => e.Name)
                .AsNoTracking());

    /// <summary>
    /// Get webhook event by ID.
    /// Used for webhook event retrieval and retry operations.
    /// </summary>
    public static readonly Func<ApplicationDbContext, Guid, Task<WebhookEvent?>> GetWebhookEventById =
        EF.CompileAsyncQuery((ApplicationDbContext context, Guid id) =>
            context.WebhookEvents
                .Include(e => e.EventType)
                .AsNoTracking()
                .FirstOrDefault(e => e.Id == id));

    /// <summary>
    /// Get pending webhook events for processing.
    /// Used by the webhook delivery background service.
    /// </summary>
    public static readonly Func<ApplicationDbContext, int, IAsyncEnumerable<WebhookEvent>> GetPendingWebhookEvents =
        EF.CompileAsyncQuery((ApplicationDbContext context, int limit) =>
            context.WebhookEvents
                .Include(e => e.EventType)
                .Where(e => e.Status == WebhookEventStatus.Pending
                    && e.ScheduledAt <= DateTime.UtcNow)
                .OrderBy(e => e.ScheduledAt)
                .Take(limit)
                .AsTracking());

    /// <summary>
    /// Get failed webhook events for retry.
    /// Used by the retry background service.
    /// </summary>
    public static readonly Func<ApplicationDbContext, int, IAsyncEnumerable<WebhookEvent>> GetFailedWebhookEventsForRetry =
        EF.CompileAsyncQuery((ApplicationDbContext context, int limit) =>
            context.WebhookEvents
                .Include(e => e.EventType)
                .Where(e => e.Status == WebhookEventStatus.Failed
                    && e.RetryCount < e.MaxRetries
                    && e.NextRetryAt <= DateTime.UtcNow)
                .OrderBy(e => e.NextRetryAt)
                .Take(limit)
                .AsTracking());

    /// <summary>
    /// Get webhook delivery attempts by event ID.
    /// Used for delivery history and debugging.
    /// </summary>
    public static readonly Func<ApplicationDbContext, Guid, IAsyncEnumerable<WebhookDeliveryAttempt>> GetDeliveryAttemptsByEventId =
        EF.CompileAsyncQuery((ApplicationDbContext context, Guid eventId) =>
            context.WebhookDeliveryAttempts
                .Where(a => a.WebhookEventId == eventId)
                .OrderByDescending(a => a.AttemptedAt)
                .AsNoTracking());

    /// <summary>
    /// Get API key by key value (hashed).
    /// Used for authentication and authorization.
    /// </summary>
    public static readonly Func<ApplicationDbContext, string, Task<ApiKey?>> GetApiKeyByKeyHash =
        EF.CompileAsyncQuery((ApplicationDbContext context, string keyHash) =>
            context.ApiKeys
                .Include(k => k.Tenant)
                .AsNoTracking()
                .FirstOrDefault(k => k.KeyHash == keyHash
                    && k.IsActive
                    && !k.IsDeleted
                    && (!k.ExpiresAt.HasValue || k.ExpiresAt.Value > DateTime.UtcNow)));

    /// <summary>
    /// Get active API keys by tenant.
    /// Used for API key management.
    /// </summary>
    public static readonly Func<ApplicationDbContext, Guid, IAsyncEnumerable<ApiKey>> GetActiveApiKeysByTenant =
        EF.CompileAsyncQuery((ApplicationDbContext context, Guid tenantId) =>
            context.ApiKeys
                .Where(k => k.TenantId == tenantId
                    && k.IsActive
                    && !k.IsDeleted
                    && (!k.ExpiresAt.HasValue || k.ExpiresAt.Value > DateTime.UtcNow))
                .OrderByDescending(k => k.CreatedAt)
                .AsNoTracking());

    /// <summary>
    /// Get tenant by ID.
    /// Used for tenant validation and quota checks.
    /// </summary>
    public static readonly Func<ApplicationDbContext, Guid, Task<Tenant?>> GetTenantById =
        EF.CompileAsyncQuery((ApplicationDbContext context, Guid id) =>
            context.Tenants
                .AsNoTracking()
                .FirstOrDefault(t => t.Id == id && !t.IsDeleted));

    /// <summary>
    /// Get webhook statistics for a tenant.
    /// Used for analytics and monitoring.
    /// </summary>
    public static readonly Func<ApplicationDbContext, Guid, DateTime, DateTime, Task<WebhookStatistics>> GetWebhookStatistics =
        EF.CompileAsyncQuery((ApplicationDbContext context, Guid tenantId, DateTime startDate, DateTime endDate) =>
            context.WebhookEvents
                .Where(e => e.TenantId == tenantId
                    && e.CreatedAt >= startDate
                    && e.CreatedAt <= endDate)
                .GroupBy(e => 1)
                .Select(g => new WebhookStatistics
                {
                    TotalEvents = g.Count(),
                    SuccessfulEvents = g.Count(e => e.Status == WebhookEventStatus.Delivered),
                    FailedEvents = g.Count(e => e.Status == WebhookEventStatus.Failed),
                    PendingEvents = g.Count(e => e.Status == WebhookEventStatus.Pending),
                    AverageDeliveryTime = g.Where(e => e.Status == WebhookEventStatus.Delivered)
                        .Average(e => (double?)EF.Property<TimeSpan>(e, "DeliveryDuration").TotalMilliseconds) ?? 0
                })
                .FirstOrDefault() ?? new WebhookStatistics());

    /// <summary>
    /// Check if subscription exists for tenant and URL.
    /// Used for duplicate subscription detection.
    /// </summary>
    public static readonly Func<ApplicationDbContext, Guid, string, string, Task<bool>> SubscriptionExists =
        EF.CompileAsyncQuery((ApplicationDbContext context, Guid tenantId, string url, string eventTypeName) =>
            context.Subscriptions
                .Any(s => s.TenantId == tenantId
                    && s.Url == url
                    && s.EventType.Name == eventTypeName
                    && !s.IsDeleted));

    /// <summary>
    /// Get schema by event type ID.
    /// Used for webhook payload validation.
    /// </summary>
    public static readonly Func<ApplicationDbContext, Guid, Task<Schema?>> GetSchemaByEventTypeId =
        EF.CompileAsyncQuery((ApplicationDbContext context, Guid eventTypeId) =>
            context.Schemas
                .AsNoTracking()
                .FirstOrDefault(s => s.EventTypeId == eventTypeId && !s.IsDeleted));
}

/// <summary>
/// Statistics data for webhook events.
/// </summary>
public class WebhookStatistics
{
    public int TotalEvents { get; set; }
    public int SuccessfulEvents { get; set; }
    public int FailedEvents { get; set; }
    public int PendingEvents { get; set; }
    public double AverageDeliveryTime { get; set; }

    public double SuccessRate => TotalEvents > 0 ? (double)SuccessfulEvents / TotalEvents * 100 : 0;
    public double FailureRate => TotalEvents > 0 ? (double)FailedEvents / TotalEvents * 100 : 0;
}
