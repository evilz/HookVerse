using HookVerse.Core.Entities;
using HookVerse.Core.Interfaces;
using HookVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HookVerse.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for SchemaDefinition entity
/// </summary>
public class SchemaDefinitionRepository : Repository<SchemaDefinition>, ISchemaDefinitionRepository
{
    public SchemaDefinitionRepository(HookVerseDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<SchemaDefinition?> GetByEventTypeIdAsync(Guid eventTypeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.EventType)
            .FirstOrDefaultAsync(s => s.EventTypeId == eventTypeId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<SchemaDefinition?> GetActiveByEventTypeIdAsync(Guid eventTypeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.EventType)
            .FirstOrDefaultAsync(s => s.EventTypeId == eventTypeId && s.IsActive, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsForEventTypeAsync(Guid eventTypeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(s => s.EventTypeId == eventTypeId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SchemaDefinition>> GetBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.EventType)
            .Where(s => s.EventType.SubscriberId == subscriberId)
            .ToListAsync(cancellationToken);
    }
}
