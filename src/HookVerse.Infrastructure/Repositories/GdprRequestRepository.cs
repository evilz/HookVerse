using HookVerse.Core.Entities;
using HookVerse.Core.Interfaces;
using HookVerse.Core.ValueObjects;
using HookVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HookVerse.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for GDPR requests.
/// </summary>
public class GdprRequestRepository : Repository<GdprRequest>, IGdprRequestRepository
{
    public GdprRequestRepository(HookVerseDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GdprRequest>> GetBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default)
    {
        return await _context.GdprRequests
            .Where(g => g.SubscriberId == subscriberId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GdprRequest>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.GdprRequests
            .Where(g => g.Status == GdprRequestStatus.Pending)
            .OrderBy(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(
        Guid id,
        GdprRequestStatus status,
        string? errorMessage = null,
        string? exportFilePath = null,
        long? exportFileSizeBytes = null,
        CancellationToken cancellationToken = default)
    {
        var request = await _context.GdprRequests.FindAsync(new object[] { id }, cancellationToken);
        
        if (request == null)
        {
            return;
        }

        request.Status = status;
        request.UpdatedAt = DateTime.UtcNow;

        if (status == GdprRequestStatus.Completed || status == GdprRequestStatus.Failed)
        {
            request.CompletedAt = DateTime.UtcNow;
        }

        if (errorMessage != null)
        {
            request.ErrorMessage = errorMessage;
        }

        if (exportFilePath != null)
        {
            request.ExportFilePath = exportFilePath;
        }

        if (exportFileSizeBytes.HasValue)
        {
            request.ExportFileSizeBytes = exportFileSizeBytes.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
