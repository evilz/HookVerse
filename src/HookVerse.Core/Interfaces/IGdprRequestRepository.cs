using HookVerse.Core.Entities;
using HookVerse.Core.ValueObjects;

namespace HookVerse.Core.Interfaces;

/// <summary>
/// Repository interface for GDPR requests.
/// </summary>
public interface IGdprRequestRepository : IRepository<GdprRequest>
{
    /// <summary>
    /// Get all GDPR requests for a subscriber.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of GDPR requests.</returns>
    Task<IEnumerable<GdprRequest>> GetBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending requests to process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pending GDPR requests.</returns>
    Task<IEnumerable<GdprRequest>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the status of a GDPR request.
    /// </summary>
    /// <param name="id">Request ID.</param>
    /// <param name="status">New status.</param>
    /// <param name="errorMessage">Error message if failed.</param>
    /// <param name="exportFilePath">Export file path if completed export.</param>
    /// <param name="exportFileSizeBytes">Export file size if completed export.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateStatusAsync(
        Guid id,
        GdprRequestStatus status,
        string? errorMessage = null,
        string? exportFilePath = null,
        long? exportFileSizeBytes = null,
        CancellationToken cancellationToken = default);
}
