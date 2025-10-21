using HookVerse.Api.Models;
using HookVerse.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HookVerse.Api.Controllers;

/// <summary>
/// API controller for mock webhook endpoint management.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class MockEndpointsController : ControllerBase
{
    private readonly IMockEndpointService _mockEndpointService;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<MockEndpointsController> _logger;

    public MockEndpointsController(
        IMockEndpointService mockEndpointService,
        IWebhookService webhookService,
        ILogger<MockEndpointsController> logger)
    {
        _mockEndpointService = mockEndpointService;
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new mock endpoint.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MockEndpointResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MockEndpointResponse>> CreateMockEndpoint(
        [FromBody] CreateMockEndpointRequest request,
        CancellationToken cancellationToken)
    {
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        try
        {
            var mockEndpoint = await _mockEndpointService.CreateMockEndpointAsync(
                subscriberId.Value,
                request.Name,
                request.Description,
                request.ResponseStatus,
                request.ResponseBody,
                request.ResponseContentType,
                request.ResponseDelayMs,
                request.ResponseHeaders,
                cancellationToken);

            var response = MapToResponse(mockEndpoint);
            return CreatedAtAction(nameof(GetMockEndpoint), new { id = mockEndpoint.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating mock endpoint for subscriber {SubscriberId}", subscriberId.Value);
            return StatusCode(500, new { error = "An error occurred while creating the mock endpoint" });
        }
    }

    /// <summary>
    /// Get all mock endpoints for the authenticated subscriber.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MockEndpointResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<MockEndpointResponse>>> GetMockEndpoints(CancellationToken cancellationToken)
    {
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        try
        {
            var mockEndpoints = await _mockEndpointService.GetMockEndpointsAsync(subscriberId.Value, cancellationToken);
            var response = mockEndpoints.Select(MapToResponse).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mock endpoints for subscriber {SubscriberId}", subscriberId.Value);
            return StatusCode(500, new { error = "An error occurred while retrieving mock endpoints" });
        }
    }

    /// <summary>
    /// Get a specific mock endpoint by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MockEndpointResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MockEndpointResponse>> GetMockEndpoint(Guid id, CancellationToken cancellationToken)
    {
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        try
        {
            var mockEndpoint = await _mockEndpointService.GetMockEndpointAsync(id, subscriberId.Value, cancellationToken);
            
            if (mockEndpoint == null)
            {
                return NotFound(new { error = "Mock endpoint not found" });
            }

            return Ok(MapToResponse(mockEndpoint));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mock endpoint {MockEndpointId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the mock endpoint" });
        }
    }

    /// <summary>
    /// Update a mock endpoint.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(MockEndpointResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MockEndpointResponse>> UpdateMockEndpoint(
        Guid id,
        [FromBody] UpdateMockEndpointRequest request,
        CancellationToken cancellationToken)
    {
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        try
        {
            var mockEndpoint = await _mockEndpointService.UpdateMockEndpointAsync(
                id,
                subscriberId.Value,
                request.Name,
                request.Description,
                request.ResponseStatus,
                request.ResponseBody,
                request.ResponseContentType,
                request.ResponseDelayMs,
                request.ResponseHeaders,
                request.IsActive,
                cancellationToken);

            if (mockEndpoint == null)
            {
                return NotFound(new { error = "Mock endpoint not found" });
            }

            return Ok(MapToResponse(mockEndpoint));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating mock endpoint {MockEndpointId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the mock endpoint" });
        }
    }

    /// <summary>
    /// Delete a mock endpoint.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteMockEndpoint(Guid id, CancellationToken cancellationToken)
    {
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        try
        {
            var deleted = await _mockEndpointService.DeleteMockEndpointAsync(id, subscriberId.Value, cancellationToken);
            
            if (!deleted)
            {
                return NotFound(new { error = "Mock endpoint not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting mock endpoint {MockEndpointId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the mock endpoint" });
        }
    }

    /// <summary>
    /// Trigger a test webhook to a mock endpoint.
    /// </summary>
    [HttpPost("{id}/trigger")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TriggerTestWebhook(
        Guid id,
        [FromBody] TriggerMockWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        try
        {
            var mockEndpoint = await _mockEndpointService.GetMockEndpointAsync(id, subscriberId.Value, cancellationToken);
            
            if (mockEndpoint == null)
            {
                return NotFound(new { error = "Mock endpoint not found" });
            }

            // Create a test webhook event targeting the mock endpoint
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var mockUrl = $"{baseUrl}{mockEndpoint.UrlPath}";

            var payload = request.Payload ?? new { test = true, triggeredAt = DateTime.UtcNow };
            var payloadJson = JsonSerializer.Serialize(payload);
            
            // For now, just log the trigger request
            // In a future implementation, we could send an actual webhook to the mock endpoint
            _logger.LogInformation(
                "Test webhook triggered for mock endpoint {MockEndpointId} with payload: {Payload}",
                id, payloadJson);

            return Accepted(new
            {
                mockEndpointId = mockEndpoint.Id,
                mockEndpointUrl = mockUrl,
                eventType = request.EventType,
                payload,
                message = "Test webhook trigger recorded successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering test webhook for mock endpoint {MockEndpointId}", id);
            return StatusCode(500, new { error = "An error occurred while triggering the test webhook" });
        }
    }

    private MockEndpointResponse MapToResponse(HookVerse.Core.Entities.MockEndpoint mockEndpoint)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        Dictionary<string, string>? headers = null;
        
        if (!string.IsNullOrEmpty(mockEndpoint.ResponseHeaders))
        {
            try
            {
                headers = JsonSerializer.Deserialize<Dictionary<string, string>>(mockEndpoint.ResponseHeaders);
            }
            catch
            {
                // Ignore invalid JSON
            }
        }

        return new MockEndpointResponse
        {
            Id = mockEndpoint.Id,
            SubscriberId = mockEndpoint.SubscriberId,
            Name = mockEndpoint.Name,
            Description = mockEndpoint.Description,
            Url = $"{baseUrl}{mockEndpoint.UrlPath}",
            UrlPath = mockEndpoint.UrlPath,
            ResponseStatus = mockEndpoint.ResponseStatus,
            ResponseBody = mockEndpoint.ResponseBody,
            ResponseContentType = mockEndpoint.ResponseContentType,
            ResponseDelayMs = mockEndpoint.ResponseDelayMs,
            ResponseHeaders = headers,
            IsActive = mockEndpoint.IsActive,
            RequestCount = mockEndpoint.RequestCount,
            LastRequestAt = mockEndpoint.LastRequestAt,
            CreatedAt = mockEndpoint.CreatedAt,
            UpdatedAt = mockEndpoint.UpdatedAt
        };
    }
}
