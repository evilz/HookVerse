using Microsoft.AspNetCore.Mvc;
using HookVerse.Infrastructure.Services;

namespace HookVerse.Api.Controllers;

/// <summary>
/// API endpoints for encryption key rotation management
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class EncryptionKeyRotationController : ControllerBase
{
    private readonly EncryptionKeyRotationService _rotationService;
    private readonly ILogger<EncryptionKeyRotationController> _logger;

    public EncryptionKeyRotationController(
        EncryptionKeyRotationService rotationService,
        ILogger<EncryptionKeyRotationController> logger)
    {
        _rotationService = rotationService ?? throw new ArgumentNullException(nameof(rotationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get encryption statistics including key versions in use
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Encryption statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(EncryptionStatistics), StatusCodes.Status200OK)]
    public async Task<ActionResult<EncryptionStatistics>> GetStatistics(
        CancellationToken cancellationToken)
    {
        var stats = await _rotationService.GetEncryptionStatisticsAsync(cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// Re-encrypt webhook events with the current encryption key
    /// </summary>
    /// <param name="maxRecords">Maximum number of records to re-encrypt (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records re-encrypted</returns>
    [HttpPost("reencrypt/webhook-events")]
    [ProducesResponseType(typeof(ReEncryptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReEncryptionResponse>> ReEncryptWebhookEvents(
        [FromQuery] int? maxRecords = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Re-encryption requested for webhook events. Max records: {MaxRecords}", maxRecords);

        try
        {
            var reEncryptedCount = await _rotationService.ReEncryptWebhookEventsAsync(
                maxRecords,
                cancellationToken);

            var response = new ReEncryptionResponse
            {
                Success = true,
                RecordsReEncrypted = reEncryptedCount,
                Message = $"Successfully re-encrypted {reEncryptedCount} webhook event(s)"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error re-encrypting webhook events");
            return BadRequest(new ProblemDetails
            {
                Title = "Re-encryption Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Re-encrypt subscriptions with the current encryption key
    /// </summary>
    /// <param name="maxRecords">Maximum number of records to re-encrypt (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records re-encrypted</returns>
    [HttpPost("reencrypt/subscriptions")]
    [ProducesResponseType(typeof(ReEncryptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReEncryptionResponse>> ReEncryptSubscriptions(
        [FromQuery] int? maxRecords = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Re-encryption requested for subscriptions. Max records: {MaxRecords}", maxRecords);

        try
        {
            var reEncryptedCount = await _rotationService.ReEncryptSubscriptionsAsync(
                maxRecords,
                cancellationToken);

            var response = new ReEncryptionResponse
            {
                Success = true,
                RecordsReEncrypted = reEncryptedCount,
                Message = $"Successfully re-encrypted {reEncryptedCount} subscription(s)"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error re-encrypting subscriptions");
            return BadRequest(new ProblemDetails
            {
                Title = "Re-encryption Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Perform complete encryption key rotation
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rotation result</returns>
    [HttpPost("rotate")]
    [ProducesResponseType(typeof(EncryptionKeyRotationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EncryptionKeyRotationResult>> RotateEncryptionKey(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Encryption key rotation requested");

        var result = await _rotationService.PerformKeyRotationAsync(cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Encryption Key Rotation Failed",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        _logger.LogInformation(
            "Encryption key rotation completed. New version: {Version}, Re-encrypted: {Count}",
            result.NewKeyVersion,
            result.RecordsReEncrypted);

        return Ok(result);
    }
}

/// <summary>
/// Response for re-encryption operations
/// </summary>
public class ReEncryptionResponse
{
    public bool Success { get; set; }
    public int RecordsReEncrypted { get; set; }
    public string Message { get; set; } = string.Empty;
}
