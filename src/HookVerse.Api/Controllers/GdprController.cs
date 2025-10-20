using HookVerse.Api.Models;
using HookVerse.Core.Interfaces;
using HookVerse.Core.ValueObjects;
using HookVerse.Infrastructure.Metrics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HookVerse.Api.Controllers;

/// <summary>
/// API controller for GDPR compliance operations.
/// </summary>
[ApiController]
[Route("api/v1/gdpr")]
[Produces("application/json")]
public class GdprController : ControllerBase
{
    private readonly IGdprService _gdprService;
    private readonly GdprMetrics _gdprMetrics;
    private readonly ILogger<GdprController> _logger;

    public GdprController(
        IGdprService gdprService,
        GdprMetrics gdprMetrics,
        ILogger<GdprController> logger)
    {
        _gdprService = gdprService;
        _gdprMetrics = gdprMetrics;
        _logger = logger;
    }

    /// <summary>
    /// Request a data export (GDPR Right to Access).
    /// </summary>
    [HttpPost("export")]
    [ProducesResponseType(typeof(GdprRequestResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GdprRequestResponse>> RequestExport(
        [FromBody] CreateGdprExportRequest request,
        CancellationToken cancellationToken)
    {
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        try
        {
            var metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null;
            var gdprRequest = await _gdprService.CreateExportRequestAsync(subscriberId.Value, metadata, cancellationToken);

            _gdprMetrics.RecordRequestCreated("Export");

            var response = MapToResponse(gdprRequest);
            return Accepted(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating export request for subscriber {SubscriberId}", subscriberId.Value);
            return StatusCode(500, new { error = "An error occurred while creating the export request" });
        }
    }

    /// <summary>
    /// Request data deletion (GDPR Right to Erasure).
    /// </summary>
    [HttpPost("delete")]
    [ProducesResponseType(typeof(GdprRequestResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GdprRequestResponse>> RequestDelete(
        [FromBody] CreateGdprDeleteRequest request,
        CancellationToken cancellationToken)
    {
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        if (!request.Confirmed)
        {
            return BadRequest(new { error = "Deletion must be confirmed" });
        }

        try
        {
            var metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null;
            var gdprRequest = await _gdprService.CreateDeleteRequestAsync(subscriberId.Value, metadata, cancellationToken);

            _gdprMetrics.RecordRequestCreated("Delete");

            var response = MapToResponse(gdprRequest);
            return Accepted(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating delete request for subscriber {SubscriberId}", subscriberId.Value);
            return StatusCode(500, new { error = "An error occurred while creating the delete request" });
        }
    }

    /// <summary>
    /// Get all GDPR requests for the authenticated subscriber.
    /// </summary>
    [HttpGet("requests")]
    [ProducesResponseType(typeof(List<GdprRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<GdprRequestResponse>>> GetRequests(CancellationToken cancellationToken)
    {
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        try
        {
            var requests = await _gdprService.GetRequestsAsync(subscriberId.Value, cancellationToken);
            var response = requests.Select(MapToResponse).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDPR requests for subscriber {SubscriberId}", subscriberId.Value);
            return StatusCode(500, new { error = "An error occurred while retrieving GDPR requests" });
        }
    }

    /// <summary>
    /// Get a specific GDPR request by ID.
    /// </summary>
    [HttpGet("requests/{id}")]
    [ProducesResponseType(typeof(GdprRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GdprRequestResponse>> GetRequest(Guid id, CancellationToken cancellationToken)
    {
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        try
        {
            var request = await _gdprService.GetRequestAsync(id, subscriberId.Value, cancellationToken);
            
            if (request == null)
            {
                return NotFound(new { error = "GDPR request not found" });
            }

            return Ok(MapToResponse(request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GDPR request {RequestId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the GDPR request" });
        }
    }

    /// <summary>
    /// Download the export file for a completed export request.
    /// </summary>
    [HttpGet("export/{id}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DownloadExport(Guid id, CancellationToken cancellationToken)
    {
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        try
        {
            var (fileStream, fileName) = await _gdprService.GetExportFileAsync(id, subscriberId.Value, cancellationToken);
            
            if (fileStream == null || fileName == null)
            {
                return NotFound(new { error = "Export file not found or not ready" });
            }

            return File(fileStream, "application/json", fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid export download request {RequestId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading export file for request {RequestId}", id);
            return StatusCode(500, new { error = "An error occurred while downloading the export file" });
        }
    }

    private GdprRequestResponse MapToResponse(HookVerse.Core.Entities.GdprRequest request)
    {
        Dictionary<string, string>? metadata = null;
        if (!string.IsNullOrEmpty(request.Metadata))
        {
            try
            {
                metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(request.Metadata);
            }
            catch
            {
                // Ignore invalid JSON
            }
        }

        return new GdprRequestResponse
        {
            Id = request.Id,
            SubscriberId = request.SubscriberId,
            RequestType = request.RequestType.ToString(),
            Status = request.Status.ToString(),
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            CompletedAt = request.CompletedAt,
            ErrorMessage = request.ErrorMessage,
            ExportFilePath = request.ExportFilePath,
            ExportFileSizeBytes = request.ExportFileSizeBytes,
            Metadata = metadata
        };
    }
}
