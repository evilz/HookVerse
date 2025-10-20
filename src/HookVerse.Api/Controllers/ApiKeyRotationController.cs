using Microsoft.AspNetCore.Mvc;
using HookVerse.Infrastructure.Services;

namespace HookVerse.Api.Controllers;

/// <summary>
/// API endpoints for API key rotation management
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class ApiKeyRotationController : ControllerBase
{
    private readonly ApiKeyRotationService _rotationService;
    private readonly ILogger<ApiKeyRotationController> _logger;

    public ApiKeyRotationController(
        ApiKeyRotationService rotationService,
        ILogger<ApiKeyRotationController> logger)
    {
        _rotationService = rotationService ?? throw new ArgumentNullException(nameof(rotationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Rotate an API key, creating a new key while keeping the old one valid during grace period
    /// </summary>
    /// <param name="apiKeyId">ID of the API key to rotate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rotation result with new API key</returns>
    [HttpPost("{apiKeyId}/rotate")]
    [ProducesResponseType(typeof(ApiKeyRotationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiKeyRotationResult>> RotateApiKey(
        Guid apiKeyId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rotation requested for API key {KeyId}", apiKeyId);

        var result = await _rotationService.RotateApiKeyAsync(apiKeyId, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "API Key Rotation Failed",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Log but don't include the actual API key in logs
        _logger.LogInformation(
            "Successfully rotated API key {OldKeyId} to {NewKeyId}",
            result.OldKeyId, result.NewKeyId);

        return Ok(result);
    }

    /// <summary>
    /// Get rotation status for all API keys belonging to a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of API key rotation statuses</returns>
    [HttpGet("status/{tenantId}")]
    [ProducesResponseType(typeof(IEnumerable<ApiKeyRotationStatus>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ApiKeyRotationStatus>>> GetRotationStatus(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var statuses = await _rotationService.GetRotationStatusAsync(tenantId, cancellationToken);
        
        var statusList = statuses.ToList();
        
        if (!statusList.Any())
        {
            return NotFound(new ProblemDetails
            {
                Title = "No API Keys Found",
                Detail = $"No active API keys found for tenant {tenantId}",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(statusList);
    }

    /// <summary>
    /// Validate tenant's API keys and get recommendations
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with recommendations</returns>
    [HttpGet("validate/{tenantId}")]
    [ProducesResponseType(typeof(ApiKeyValidationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiKeyValidationResponse>> ValidateTenantKeys(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var isValid = await _rotationService.ValidateTenantKeysAsync(tenantId, cancellationToken);
        var statuses = await _rotationService.GetRotationStatusAsync(tenantId, cancellationToken);
        var statusList = statuses.ToList();

        var response = new ApiKeyValidationResponse
        {
            IsValid = isValid,
            TenantId = tenantId,
            TotalKeys = statusList.Count,
            KeysRequiringRotation = statusList.Count(s => s.RequiresRotation),
            CriticalKeys = statusList.Count(s => s.Urgency == RotationUrgency.Critical),
            HighUrgencyKeys = statusList.Count(s => s.Urgency == RotationUrgency.High),
            Recommendations = BuildRecommendations(statusList)
        };

        return Ok(response);
    }

    /// <summary>
    /// Trigger manual rotation check for all expired keys (admin only)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of keys rotated</returns>
    [HttpPost("trigger-auto-rotation")]
    [ProducesResponseType(typeof(AutoRotationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AutoRotationResponse>> TriggerAutoRotation(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual auto-rotation triggered");

        var rotatedCount = await _rotationService.AutoRotateExpiredKeysAsync(cancellationToken);
        var revokedCount = await _rotationService.RevokeExpiredGracePeriodKeysAsync(cancellationToken);

        var response = new AutoRotationResponse
        {
            RotatedCount = rotatedCount,
            RevokedCount = revokedCount,
            Message = $"Auto-rotation completed. Rotated {rotatedCount} keys, revoked {revokedCount} keys."
        };

        _logger.LogInformation(
            "Manual auto-rotation completed. Rotated: {Rotated}, Revoked: {Revoked}",
            rotatedCount, revokedCount);

        return Ok(response);
    }

    private static List<string> BuildRecommendations(List<ApiKeyRotationStatus> statuses)
    {
        var recommendations = new List<string>();

        var criticalKeys = statuses.Where(s => s.Urgency == RotationUrgency.Critical).ToList();
        var highKeys = statuses.Where(s => s.Urgency == RotationUrgency.High).ToList();
        var mediumKeys = statuses.Where(s => s.Urgency == RotationUrgency.Medium).ToList();
        var gracePeriodKeys = statuses.Where(s => s.InGracePeriod).ToList();

        if (criticalKeys.Any())
        {
            recommendations.Add(
                $"URGENT: {criticalKeys.Count} API key(s) have exceeded maximum lifetime and should be rotated immediately.");
        }

        if (highKeys.Any())
        {
            recommendations.Add(
                $"WARNING: {highKeys.Count} API key(s) will expire soon and should be rotated within the next few days.");
        }

        if (mediumKeys.Any())
        {
            recommendations.Add(
                $"NOTICE: {mediumKeys.Count} API key(s) are approaching expiration. Consider rotating them soon.");
        }

        if (gracePeriodKeys.Any())
        {
            recommendations.Add(
                $"INFO: {gracePeriodKeys.Count} API key(s) are currently in grace period. " +
                "Update your applications to use the new keys before the grace period ends.");
        }

        if (!recommendations.Any())
        {
            recommendations.Add("All API keys are healthy. No action required at this time.");
        }

        return recommendations;
    }
}

/// <summary>
/// Response for API key validation
/// </summary>
public class ApiKeyValidationResponse
{
    public bool IsValid { get; set; }
    public Guid TenantId { get; set; }
    public int TotalKeys { get; set; }
    public int KeysRequiringRotation { get; set; }
    public int CriticalKeys { get; set; }
    public int HighUrgencyKeys { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Response for auto-rotation trigger
/// </summary>
public class AutoRotationResponse
{
    public int RotatedCount { get; set; }
    public int RevokedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
