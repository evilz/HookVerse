using HookVerse.Core.Entities;
using HookVerse.Core.Enums;
using HookVerse.Core.Interfaces;
using HookVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HookVerse.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for DeliveryAttempt entity.
/// </summary>
public class DeliveryAttemptRepository : Repository<DeliveryAttempt>, IDeliveryAttemptRepository
{
    public DeliveryAttemptRepository(HookVerseDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<DeliveryAttempt>> GetByWebhookEventIdAsync(Guid webhookEventId, CancellationToken cancellationToken = default)
    {
        return await _context.DeliveryAttempts
            .Where(da => da.WebhookEventId == webhookEventId)
            .OrderBy(da => da.AttemptNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DeliveryAttempt>> GetBySubscriptionIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _context.DeliveryAttempts
            .Where(da => da.SubscriptionId == subscriptionId)
            .OrderByDescending(da => da.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DeliveryAttempt>> GetForRetryAsync(DeliveryStatus status, DateTime beforeTime, CancellationToken cancellationToken = default)
    {
        return await _context.DeliveryAttempts
            .Where(da => 
                da.Status == status && 
                da.NextRetryAt != null && 
                da.NextRetryAt <= beforeTime)
            .Include(da => da.WebhookEvent)
            .Include(da => da.Subscription)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeliveryAttempt?> GetLatestAttemptAsync(Guid webhookEventId, CancellationToken cancellationToken = default)
    {
        return await _context.DeliveryAttempts
            .Where(da => da.WebhookEventId == webhookEventId)
            .OrderByDescending(da => da.AttemptNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
