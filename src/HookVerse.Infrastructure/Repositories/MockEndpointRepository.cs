using HookVerse.Core.Entities;
using HookVerse.Core.Interfaces;
using HookVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HookVerse.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for MockEndpoint entity.
/// </summary>
public class MockEndpointRepository : Repository<MockEndpoint>, IMockEndpointRepository
{
    public MockEndpointRepository(HookVerseDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MockEndpoint>> GetBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default)
    {
        return await _context.MockEndpoints
            .Where(m => m.SubscriberId == subscriberId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MockEndpoint?> GetByUrlPathAsync(string urlPath, CancellationToken cancellationToken = default)
    {
        return await _context.MockEndpoints
            .FirstOrDefaultAsync(m => m.UrlPath == urlPath, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MockEndpoint>> GetActiveBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default)
    {
        return await _context.MockEndpoints
            .Where(m => m.SubscriberId == subscriberId && m.IsActive)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task IncrementRequestCountAsync(Guid mockEndpointId, CancellationToken cancellationToken = default)
    {
        var mockEndpoint = await _context.MockEndpoints
            .FirstOrDefaultAsync(m => m.Id == mockEndpointId, cancellationToken);

        if (mockEndpoint != null)
        {
            mockEndpoint.RequestCount++;
            mockEndpoint.LastRequestAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
