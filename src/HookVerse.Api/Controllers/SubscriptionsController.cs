using Asp.Versioning;
using HookVerse.Api.Models;
using HookVerse.Core.Interfaces;
using HookVerse.Infrastructure.Metrics;
using Microsoft.AspNetCore.Mvc;

namespace HookVerse.Api.Controllers;

/// <summary>
/// Controller for managing webhook subscriptions
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/subscriptions")]
[Produces("application/json")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly ILogger<SubscriptionsController> _logger;
    private readonly SubscriptionMetrics? _metrics;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        ISubscriptionRepository subscriptionRepository,
        IEventTypeRepository eventTypeRepository,
        ILogger<SubscriptionsController> logger,
        SubscriptionMetrics? metrics = null)
    {
        _subscriptionService = subscriptionService;
        _subscriptionRepository = subscriptionRepository;
        _eventTypeRepository = eventTypeRepository;
        _logger = logger;
        _metrics = metrics;
    }

    /// <summary>
    /// Create a new webhook subscription
    /// </summary>
    /// <param name="request">Subscription creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created subscription details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionResponse>> CreateSubscription(
        [FromBody] CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Get subscriber ID from authenticated user context
        var subscriberId = GetAuthenticatedSubscriberId();

        _logger.LogInformation(
            "Creating subscription for subscriber {SubscriberId} to event type {EventTypeId}",
            subscriberId, request.EventTypeId);

        // Verify event type exists and belongs to this subscriber
        var eventType = await _eventTypeRepository.GetByIdAsync(request.EventTypeId, cancellationToken);
        if (eventType == null)
        {
            return NotFound(new { error = "Event type not found" });
        }

        if (eventType.SubscriberId != subscriberId)
        {
            return NotFound(new { error = "Event type not found" });
        }

        // Create subscription through service (validates business rules)
        var subscription = await _subscriptionService.CreateSubscriptionAsync(
            subscriberId,
            request.EventTypeId,
            request.EndpointUrl,
            request.Secret,
            request.AuthType,
            request.AuthConfig,
            request.Description,
            request.TimeoutSeconds,
            request.MaxRetries,
            cancellationToken);

        var response = MapToResponse(subscription, eventType.Name);

        _logger.LogInformation(
            "Subscription {SubscriptionId} created successfully for subscriber {SubscriberId}",
            subscription.Id, subscriberId);

        // Record metrics
        _metrics?.RecordSubscriptionCreated(subscriberId, request.EventTypeId);

        return CreatedAtAction(
            nameof(GetSubscription),
            new { id = subscription.Id },
            response);
    }

    /// <summary>
    /// Get all subscriptions for the authenticated subscriber
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page (max 100)</param>
    /// <param name="eventTypeId">Optional filter by event type ID</param>
    /// <param name="isActive">Optional filter by active status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of subscriptions</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<SubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResponse<SubscriptionResponse>>> GetSubscriptions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? eventTypeId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var subscriberId = GetAuthenticatedSubscriberId();

        pageSize = Math.Min(pageSize, 100); // Cap at 100
        page = Math.Max(page, 1); // Ensure at least page 1

        _logger.LogInformation(
            "Fetching subscriptions for subscriber {SubscriberId}, page {Page}, pageSize {PageSize}",
            subscriberId, page, pageSize);

        var subscriptions = (await _subscriptionRepository.GetBySubscriberIdAsync(
            subscriberId,
            cancellationToken)).ToList();

        // Apply filters
        if (eventTypeId.HasValue)
        {
            subscriptions = subscriptions.Where(s => s.EventTypeId == eventTypeId.Value).ToList();
        }

        if (isActive.HasValue)
        {
            subscriptions = subscriptions.Where(s => s.IsActive == isActive.Value).ToList();
        }

        // Paginate
        var totalCount = subscriptions.Count;
        var items = subscriptions
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Load event type names
        var eventTypeIds = items.Select(s => s.EventTypeId).Distinct().ToList();
        var eventTypes = new Dictionary<Guid, string>();
        foreach (var etId in eventTypeIds)
        {
            var et = await _eventTypeRepository.GetByIdAsync(etId, cancellationToken);
            if (et != null)
            {
                eventTypes[etId] = et.Name;
            }
        }

        var responses = items.Select(s => MapToResponse(s, eventTypes.GetValueOrDefault(s.EventTypeId, "Unknown"))).ToList();

        return Ok(new PaginatedResponse<SubscriptionResponse>
        {
            Items = responses,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    /// <summary>
    /// Get a specific subscription by ID
    /// </summary>
    /// <param name="id">Subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionResponse>> GetSubscription(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var subscriberId = GetAuthenticatedSubscriberId();

        _logger.LogInformation(
            "Fetching subscription {SubscriptionId} for subscriber {SubscriberId}",
            id, subscriberId);

        var subscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);

        if (subscription == null || subscription.SubscriberId != subscriberId)
        {
            return NotFound(new { error = "Subscription not found" });
        }

        var eventType = await _eventTypeRepository.GetByIdAsync(subscription.EventTypeId, cancellationToken);
        var response = MapToResponse(subscription, eventType?.Name ?? "Unknown");

        return Ok(response);
    }

    /// <summary>
    /// Update an existing subscription
    /// </summary>
    /// <param name="id">Subscription ID</param>
    /// <param name="request">Updated subscription details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated subscription details</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionResponse>> UpdateSubscription(
        Guid id,
        [FromBody] UpdateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var subscriberId = GetAuthenticatedSubscriberId();

        _logger.LogInformation(
            "Updating subscription {SubscriptionId} for subscriber {SubscriberId}",
            id, subscriberId);

        var subscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);

        if (subscription == null || subscription.SubscriberId != subscriberId)
        {
            return NotFound(new { error = "Subscription not found" });
        }

        // Update subscription through service (validates business rules)
        var updated = await _subscriptionService.UpdateSubscriptionAsync(
            subscription,
            request.EndpointUrl,
            request.Secret,
            request.AuthType,
            request.AuthConfig,
            request.Description,
            request.TimeoutSeconds,
            request.MaxRetries,
            cancellationToken);

        var eventType = await _eventTypeRepository.GetByIdAsync(updated.EventTypeId, cancellationToken);
        var response = MapToResponse(updated, eventType?.Name ?? "Unknown");

        _logger.LogInformation(
            "Subscription {SubscriptionId} updated successfully",
            id);

        // Record metrics
        _metrics?.RecordSubscriptionUpdated(id);

        return Ok(response);
    }

    /// <summary>
    /// Delete a subscription
    /// </summary>
    /// <param name="id">Subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubscription(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var subscriberId = GetAuthenticatedSubscriberId();

        _logger.LogInformation(
            "Deleting subscription {SubscriptionId} for subscriber {SubscriberId}",
            id, subscriberId);

        var subscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);

        if (subscription == null || subscription.SubscriberId != subscriberId)
        {
            return NotFound(new { error = "Subscription not found" });
        }

        await _subscriptionRepository.DeleteAsync(subscription, cancellationToken);

        _logger.LogInformation(
            "Subscription {SubscriptionId} deleted successfully",
            id);

        // Record metrics
        _metrics?.RecordSubscriptionDeleted(id);

        return NoContent();
    }

    /// <summary>
    /// Pause a subscription (stops webhook delivery)
    /// </summary>
    /// <param name="id">Subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated subscription details</returns>
    [HttpPost("{id}/pause")]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionResponse>> PauseSubscription(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var subscriberId = GetAuthenticatedSubscriberId();

        _logger.LogInformation(
            "Pausing subscription {SubscriptionId} for subscriber {SubscriberId}",
            id, subscriberId);

        var subscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);

        if (subscription == null || subscription.SubscriberId != subscriberId)
        {
            return NotFound(new { error = "Subscription not found" });
        }

        subscription.IsActive = false;
        subscription.PausedAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        var eventType = await _eventTypeRepository.GetByIdAsync(subscription.EventTypeId, cancellationToken);
        var response = MapToResponse(subscription, eventType?.Name ?? "Unknown");

        _logger.LogInformation(
            "Subscription {SubscriptionId} paused successfully",
            id);

        // Record metrics
        _metrics?.RecordSubscriptionPaused(id);

        return Ok(response);
    }

    /// <summary>
    /// Resume a paused subscription (resumes webhook delivery)
    /// </summary>
    /// <param name="id">Subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated subscription details</returns>
    [HttpPost("{id}/resume")]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionResponse>> ResumeSubscription(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var subscriberId = GetAuthenticatedSubscriberId();

        _logger.LogInformation(
            "Resuming subscription {SubscriptionId} for subscriber {SubscriberId}",
            id, subscriberId);

        var subscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);

        if (subscription == null || subscription.SubscriberId != subscriberId)
        {
            return NotFound(new { error = "Subscription not found" });
        }

        subscription.IsActive = true;
        subscription.PausedAt = null;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        var eventType = await _eventTypeRepository.GetByIdAsync(subscription.EventTypeId, cancellationToken);
        var response = MapToResponse(subscription, eventType?.Name ?? "Unknown");

        _logger.LogInformation(
            "Subscription {SubscriptionId} resumed successfully",
            id);

        // Record metrics
        _metrics?.RecordSubscriptionResumed(id);

        return Ok(response);
    }

    private Guid GetAuthenticatedSubscriberId()
    {
        // TODO: Extract from authenticated API key context
        // For now, use a placeholder - will be replaced with actual auth implementation
        var subscriberIdClaim = User.FindFirst("SubscriberId")?.Value;
        if (subscriberIdClaim != null && Guid.TryParse(subscriberIdClaim, out var subscriberId))
        {
            return subscriberId;
        }

        // Fallback for development
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private static SubscriptionResponse MapToResponse(Core.Entities.Subscription subscription, string eventTypeName)
    {
        return new SubscriptionResponse
        {
            Id = subscription.Id,
            SubscriberId = subscription.SubscriberId,
            EventTypeId = subscription.EventTypeId,
            EventTypeName = eventTypeName,
            EndpointUrl = subscription.EndpointUrl,
            AuthType = subscription.AuthType,
            Description = subscription.Description,
            TimeoutSeconds = subscription.TimeoutSeconds,
            MaxRetries = subscription.MaxRetries,
            IsActive = subscription.IsActive,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt,
            PausedAt = subscription.PausedAt
        };
    }
}
