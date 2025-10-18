using Asp.Versioning;
using HookVerse.Api.Models;
using HookVerse.Core.Enums;
using HookVerse.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HookVerse.Api.Controllers;

/// <summary>
/// API controller for webhook operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IWebhookService webhookService,
        ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Sends a webhook event.
    /// </summary>
    /// <param name="request">The webhook request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created webhook event information.</returns>
    [HttpPost("send")]
    [ProducesResponseType(typeof(WebhookResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WebhookResponse>> SendWebhook(
        [FromBody] SendWebhookRequest request,
        CancellationToken cancellationToken)
    {
        // Get subscriber ID from authenticated context (set by API key middleware)
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        try
        {
            var webhookEvent = await _webhookService.SendWebhookAsync(
                request.EventTypeId,
                subscriberId.Value,
                request.Payload,
                request.ScheduledFor,
                request.Metadata,
                cancellationToken);

            var response = new WebhookResponse
            {
                Id = webhookEvent.Id,
                EventTypeId = webhookEvent.EventTypeId,
                TraceId = webhookEvent.TraceId,
                CreatedAt = webhookEvent.CreatedAt,
                ScheduledFor = webhookEvent.ScheduledFor,
                ExpiresAt = webhookEvent.ExpiresAt,
                PayloadSizeBytes = webhookEvent.PayloadSizeBytes,
                Status = "accepted"
            };

            _logger.LogInformation("Webhook {WebhookId} accepted for processing", webhookEvent.Id);

            return Accepted(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid webhook request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending webhook");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    /// <summary>
    /// Gets the status of a webhook event.
    /// </summary>
    /// <param name="webhookId">The webhook event ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The webhook delivery status.</returns>
    [HttpGet("{webhookId}/status")]
    [ProducesResponseType(typeof(DeliveryStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DeliveryStatusResponse>> GetWebhookStatus(
        Guid webhookId,
        CancellationToken cancellationToken)
    {
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        var webhookEvent = await _webhookService.GetWebhookStatusAsync(webhookId, cancellationToken);
        if (webhookEvent == null)
        {
            return NotFound(new { error = $"Webhook {webhookId} not found" });
        }

        // Verify webhook belongs to authenticated subscriber
        if (webhookEvent.SubscriberId != subscriberId.Value)
        {
            return NotFound(new { error = $"Webhook {webhookId} not found" });
        }

        var response = new DeliveryStatusResponse
        {
            WebhookEventId = webhookEvent.Id,
            EventType = webhookEvent.EventType?.Name ?? "unknown",
            TraceId = webhookEvent.TraceId,
            CreatedAt = webhookEvent.CreatedAt,
            TotalAttempts = webhookEvent.DeliveryAttempts.Count,
            SuccessfulDeliveries = webhookEvent.DeliveryAttempts.Count(a => a.Status == DeliveryStatus.Delivered),
            FailedDeliveries = webhookEvent.DeliveryAttempts.Count(a => 
                a.Status == DeliveryStatus.Failed || 
                a.Status == DeliveryStatus.Timeout || 
                a.Status == DeliveryStatus.DeadLetter),
            Attempts = webhookEvent.DeliveryAttempts
                .OrderBy(a => a.AttemptNumber)
                .Select(a => new DeliveryAttemptDto
                {
                    Id = a.Id,
                    SubscriptionId = a.SubscriptionId,
                    EndpointUrl = a.Subscription?.EndpointUrl ?? "unknown",
                    AttemptNumber = a.AttemptNumber,
                    Status = a.Status,
                    ResponseStatus = a.ResponseStatus,
                    DurationMs = a.DurationMs,
                    ErrorMessage = a.ErrorMessage,
                    StartedAt = a.StartedAt,
                    CompletedAt = a.CompletedAt,
                    NextRetryAt = a.NextRetryAt
                })
                .ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets a specific delivery attempt.
    /// </summary>
    /// <param name="webhookId">The webhook event ID.</param>
    /// <param name="attemptId">The delivery attempt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The delivery attempt details.</returns>
    [HttpGet("{webhookId}/attempts/{attemptId}")]
    [ProducesResponseType(typeof(DeliveryAttemptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DeliveryAttemptDto>> GetDeliveryAttempt(
        Guid webhookId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        var webhookEvent = await _webhookService.GetWebhookStatusAsync(webhookId, cancellationToken);
        if (webhookEvent == null || webhookEvent.SubscriberId != subscriberId.Value)
        {
            return NotFound(new { error = $"Webhook {webhookId} not found" });
        }

        var attempt = webhookEvent.DeliveryAttempts.FirstOrDefault(a => a.Id == attemptId);
        if (attempt == null)
        {
            return NotFound(new { error = $"Delivery attempt {attemptId} not found" });
        }

        var response = new DeliveryAttemptDto
        {
            Id = attempt.Id,
            SubscriptionId = attempt.SubscriptionId,
            EndpointUrl = attempt.Subscription?.EndpointUrl ?? "unknown",
            AttemptNumber = attempt.AttemptNumber,
            Status = attempt.Status,
            ResponseStatus = attempt.ResponseStatus,
            DurationMs = attempt.DurationMs,
            ErrorMessage = attempt.ErrorMessage,
            StartedAt = attempt.StartedAt,
            CompletedAt = attempt.CompletedAt,
            NextRetryAt = attempt.NextRetryAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets all delivery attempts for a webhook.
    /// </summary>
    /// <param name="webhookId">The webhook event ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of delivery attempts.</returns>
    [HttpGet("{webhookId}/attempts")]
    [ProducesResponseType(typeof(List<DeliveryAttemptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<DeliveryAttemptDto>>> GetDeliveryAttempts(
        Guid webhookId,
        CancellationToken cancellationToken)
    {
        var subscriberId = HttpContext.Items["SubscriberId"] as Guid?;
        if (!subscriberId.HasValue)
        {
            return Unauthorized(new { error = "Invalid or missing API key" });
        }

        var webhookEvent = await _webhookService.GetWebhookStatusAsync(webhookId, cancellationToken);
        if (webhookEvent == null || webhookEvent.SubscriberId != subscriberId.Value)
        {
            return NotFound(new { error = $"Webhook {webhookId} not found" });
        }

        var attempts = webhookEvent.DeliveryAttempts
            .OrderBy(a => a.AttemptNumber)
            .Select(a => new DeliveryAttemptDto
            {
                Id = a.Id,
                SubscriptionId = a.SubscriptionId,
                EndpointUrl = a.Subscription?.EndpointUrl ?? "unknown",
                AttemptNumber = a.AttemptNumber,
                Status = a.Status,
                ResponseStatus = a.ResponseStatus,
                DurationMs = a.DurationMs,
                ErrorMessage = a.ErrorMessage,
                StartedAt = a.StartedAt,
                CompletedAt = a.CompletedAt,
                NextRetryAt = a.NextRetryAt
            })
            .ToList();

        return Ok(attempts);
    }
}
