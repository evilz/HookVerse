using HookVerse.Core.Entities;
using HookVerse.Core.Interfaces;
using HookVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HookVerse.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Subscription entity.
/// </summary>
public class SubscriptionRepository : Repository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(HookVerseDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Subscription>> GetByEventTypeIdAsync(Guid eventTypeId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Where(s => s.EventTypeId == eventTypeId)
            .Include(s => s.Subscriber)
            .Include(s => s.EventType)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetActiveByEventTypeIdAsync(Guid eventTypeId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Where(s => s.EventTypeId == eventTypeId && s.IsActive)
            .Include(s => s.Subscriber)
            .Include(s => s.EventType)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Where(s => s.SubscriberId == subscriberId)
            .Include(s => s.EventType)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateLastDeliveryAsync(Guid subscriptionId, DateTime timestamp, CancellationToken cancellationToken = default)
    {
        var subscription = await _context.Subscriptions.FindAsync(new object[] { subscriptionId }, cancellationToken);
        if (subscription != null)
        {
            subscription.LastDeliveryAt = timestamp;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
