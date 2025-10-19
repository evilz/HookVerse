using HookVerse.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HookVerse.Api.Controllers;

/// <summary>
/// API controller for analytics and reporting endpoints.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard analytics including success rates, trends, and top event types.
    /// </summary>
    /// <param name="startDate">Optional start date filter (defaults to 30 days ago).</param>
    /// <param name="endDate">Optional end date filter (defaults to now).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard analytics data.</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardAnalyticsDto>> GetDashboardAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        // Get subscriber ID from authenticated context
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        try
        {
            // Default to last 30 days if not specified
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            _logger.LogInformation(
                "Getting dashboard analytics for subscriber {SubscriberId} from {StartDate} to {EndDate}",
                subscriberId.Value, startDate, endDate);

            var analytics = await _analyticsService.GetDashboardAnalyticsAsync(
                subscriberId.Value,
                startDate,
                endDate,
                cancellationToken);

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard analytics for subscriber {SubscriberId}", subscriberId.Value);
            return StatusCode(500, new { error = "An error occurred while retrieving analytics" });
        }
    }
}
