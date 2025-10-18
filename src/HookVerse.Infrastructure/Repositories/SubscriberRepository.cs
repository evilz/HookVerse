using HookVerse.Core.Entities;
using HookVerse.Core.Interfaces;
using HookVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HookVerse.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Subscriber entity.
/// </summary>
public class SubscriberRepository : Repository<Subscriber>, ISubscriberRepository
{
    public SubscriberRepository(HookVerseDbContext context) : base(context)
    {
    }

    public async Task<Subscriber?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Subscribers
            .FirstOrDefaultAsync(s => s.Email == email, cancellationToken);
    }

    public async Task<Subscriber?> GetByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken = default)
    {
        return await _context.Subscribers
            .FirstOrDefaultAsync(s => s.ApiKeyHash == apiKeyHash, cancellationToken);
    }

    public async Task<IEnumerable<Subscriber>> GetActiveSubscribersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Subscribers
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);
    }
}
