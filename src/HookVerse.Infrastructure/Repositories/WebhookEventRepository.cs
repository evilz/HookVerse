using HookVerse.Core.Entities;
using HookVerse.Core.Interfaces;
using HookVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HookVerse.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for WebhookEvent entity.
/// </summary>
public class WebhookEventRepository : Repository<WebhookEvent>, IWebhookEventRepository
{
    public WebhookEventRepository(HookVerseDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<WebhookEvent>> GetBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookEvents
            .Where(we => we.SubscriberId == subscriberId)
            .Include(we => we.EventType)
            .OrderByDescending(we => we.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WebhookEvent>> GetScheduledForDeliveryAsync(DateTime beforeTime, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookEvents
            .Where(we => 
                (we.ScheduledFor == null || we.ScheduledFor <= beforeTime) &&
                we.ExpiresAt > DateTime.UtcNow)
            .Include(we => we.EventType)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WebhookEvent>> GetExpiredEventsAsync(DateTime beforeTime, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookEvents
            .Where(we => we.ExpiresAt < beforeTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<WebhookEvent?> GetWithDeliveryAttemptsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookEvents
            .Include(we => we.DeliveryAttempts.OrderBy(da => da.AttemptNumber))
            .Include(we => we.EventType)
            .FirstOrDefaultAsync(we => we.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<WebhookEvent> Events, int TotalCount)> SearchAsync(
        Guid subscriberId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? eventTypeId = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WebhookEvents
            .Include(we => we.EventType)
            .Include(we => we.DeliveryAttempts)
            .Where(we => we.SubscriberId == subscriberId);

        if (startDate.HasValue)
        {
            query = query.Where(we => we.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(we => we.CreatedAt <= endDate.Value);
        }

        if (eventTypeId.HasValue)
        {
            query = query.Where(we => we.EventTypeId == eventTypeId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var events = await query
            .OrderByDescending(we => we.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (events, totalCount);
    }
}
