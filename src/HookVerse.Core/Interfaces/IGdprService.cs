using HookVerse.Core.Entities;

namespace HookVerse.Core.Interfaces;

/// <summary>
/// Service interface for GDPR compliance operations.
/// </summary>
public interface IGdprService
{
    /// <summary>
    /// Create a data export request.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="metadata">Optional metadata (JSON).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created GDPR request.</returns>
    Task<GdprRequest> CreateExportRequestAsync(Guid subscriberId, string? metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a data deletion request.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="metadata">Optional metadata (JSON).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created GDPR request.</returns>
    Task<GdprRequest> CreateDeleteRequestAsync(Guid subscriberId, string? metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all GDPR requests for a subscriber.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of GDPR requests.</returns>
    Task<IEnumerable<GdprRequest>> GetRequestsAsync(Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific GDPR request.
    /// </summary>
    /// <param name="id">Request ID.</param>
    /// <param name="subscriberId">The subscriber ID (for authorization).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>GDPR request or null.</returns>
    Task<GdprRequest?> GetRequestAsync(Guid id, Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the export file stream for a completed export request.
    /// </summary>
    /// <param name="id">Request ID.</param>
    /// <param name="subscriberId">The subscriber ID (for authorization).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of (FileStream, FileName) or (null, null) if not found.</returns>
    Task<(Stream? FileStream, string? FileName)> GetExportFileAsync(Guid id, Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a data export request.
    /// </summary>
    /// <param name="requestId">Request ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessExportRequestAsync(Guid requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a data deletion request.
    /// </summary>
    /// <param name="requestId">Request ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessDeleteRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
}
