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
}
