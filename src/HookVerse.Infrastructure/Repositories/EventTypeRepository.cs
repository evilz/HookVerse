using HookVerse.Core.Entities;
using HookVerse.Core.Interfaces;
using HookVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HookVerse.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for EventType entity.
/// </summary>
public class EventTypeRepository : Repository<EventType>, IEventTypeRepository
{
    public EventTypeRepository(HookVerseDbContext context) : base(context)
    {
    }

    public async Task<EventType?> GetByNameVersionSubscriberAsync(string name, string version, Guid subscriberId, CancellationToken cancellationToken = default)
    {
        return await _context.EventTypes
            .Include(et => et.SchemaDefinition)
            .FirstOrDefaultAsync(et => 
                et.Name == name && 
                et.Version == version && 
                et.SubscriberId == subscriberId, 
                cancellationToken);
    }

    public async Task<IEnumerable<EventType>> GetBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default)
    {
        return await _context.EventTypes
            .Where(et => et.SubscriberId == subscriberId)
            .Include(et => et.SchemaDefinition)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<EventType>> GetActiveEventTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EventTypes
            .Where(et => et.IsActive)
            .Include(et => et.SchemaDefinition)
            .ToListAsync(cancellationToken);
    }
}
