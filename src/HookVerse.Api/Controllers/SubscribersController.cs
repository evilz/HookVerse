using Asp.Versioning;
using HookVerse.Api.Models;
using HookVerse.Core.Entities;
using HookVerse.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace HookVerse.Api.Controllers;

/// <summary>
/// API controller for managing subscribers (tenants)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class SubscribersController : ControllerBase
{
    private readonly IRepository<Subscriber> _subscriberRepository;
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<SubscribersController> _logger;

    public SubscribersController(
        IRepository<Subscriber> subscriberRepository,
        IApiKeyService apiKeyService,
        ILogger<SubscribersController> logger)
    {
        _subscriberRepository = subscriberRepository;
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new subscriber and generates an API key
    /// </summary>
    /// <param name="request">The subscriber creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created subscriber with API key</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SubscriberCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubscriberCreatedResponse>> CreateSubscriber(
        [FromBody] CreateSubscriberRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create subscriber entity
            var subscriber = new Subscriber
            {
                Name = request.Name,
                Email = request.Email,
                IsActive = true,
                RetentionDays = request.RetentionDays
            };

            await _subscriberRepository.AddAsync(subscriber, cancellationToken);
            await _subscriberRepository.SaveChangesAsync(cancellationToken);

            // Generate API key for the subscriber
            var apiKey = await _apiKeyService.GenerateApiKeyAsync(
                subscriber.Id,
                "Primary Key",
                cancellationToken);

            _logger.LogInformation(
                "Created new subscriber {SubscriberId} with name {SubscriberName}",
                subscriber.Id,
                subscriber.Name);

            var response = new SubscriberCreatedResponse
            {
                Id = subscriber.Id,
                Name = subscriber.Name,
                Email = subscriber.Email,
                IsActive = subscriber.IsActive,
                RetentionDays = subscriber.RetentionDays,
                CreatedAt = subscriber.CreatedAt,
                UpdatedAt = subscriber.UpdatedAt,
                ApiKey = apiKey
            };

            return CreatedAtAction(
                nameof(GetSubscriber),
                new { id = subscriber.Id },
                response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscriber");
            return BadRequest(new { error = "Failed to create subscriber", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets a subscriber by ID
    /// </summary>
    /// <param name="id">The subscriber ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The subscriber information</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SubscriberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriberResponse>> GetSubscriber(
        Guid id,
        CancellationToken cancellationToken)
    {
        var subscriber = await _subscriberRepository.GetByIdAsync(id, cancellationToken);
        
        if (subscriber == null)
        {
            _logger.LogWarning("Subscriber {SubscriberId} not found", id);
            return NotFound(new { error = "Subscriber not found" });
        }

        var response = new SubscriberResponse
        {
            Id = subscriber.Id,
            Name = subscriber.Name,
            Email = subscriber.Email,
            IsActive = subscriber.IsActive,
            RetentionDays = subscriber.RetentionDays,
            CreatedAt = subscriber.CreatedAt,
            UpdatedAt = subscriber.UpdatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Lists all subscribers
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of subscribers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<SubscriberResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<SubscriberResponse>>> ListSubscribers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Expression<Func<Subscriber, bool>>? predicate = null;
            if (isActive.HasValue)
            {
                predicate = s => s.IsActive == isActive.Value;
            }

            var (subscribers, totalCount) = await _subscriberRepository.GetPagedAsync(
                page,
                pageSize,
                predicate,
                cancellationToken);

            var items = subscribers.Select(s => new SubscriberResponse
            {
                Id = s.Id,
                Name = s.Name,
                Email = s.Email,
                IsActive = s.IsActive,
                RetentionDays = s.RetentionDays,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            var response = new PaginatedResponse<SubscriberResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing subscribers");
            return BadRequest(new { error = "Failed to list subscribers", details = ex.Message });
        }
    }

    /// <summary>
    /// Updates a subscriber
    /// </summary>
    /// <param name="id">The subscriber ID</param>
    /// <param name="request">The update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated subscriber</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SubscriberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubscriberResponse>> UpdateSubscriber(
        Guid id,
        [FromBody] UpdateSubscriberRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var subscriber = await _subscriberRepository.GetByIdAsync(id, cancellationToken);
            
            if (subscriber == null)
            {
                _logger.LogWarning("Subscriber {SubscriberId} not found for update", id);
                return NotFound(new { error = "Subscriber not found" });
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
                subscriber.Name = request.Name;

            if (!string.IsNullOrWhiteSpace(request.Email))
                subscriber.Email = request.Email;

            if (request.RetentionDays.HasValue)
                subscriber.RetentionDays = request.RetentionDays.Value;

            if (request.IsActive.HasValue)
                subscriber.IsActive = request.IsActive.Value;

            subscriber.UpdatedAt = DateTime.UtcNow;

            await _subscriberRepository.UpdateAsync(subscriber, cancellationToken);
            await _subscriberRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated subscriber {SubscriberId}", id);

            var response = new SubscriberResponse
            {
                Id = subscriber.Id,
                Name = subscriber.Name,
                Email = subscriber.Email,
                IsActive = subscriber.IsActive,
                RetentionDays = subscriber.RetentionDays,
                CreatedAt = subscriber.CreatedAt,
                UpdatedAt = subscriber.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscriber {SubscriberId}", id);
            return BadRequest(new { error = "Failed to update subscriber", details = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a subscriber
    /// </summary>
    /// <param name="id">The subscriber ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubscriber(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var subscriber = await _subscriberRepository.GetByIdAsync(id, cancellationToken);
            
            if (subscriber == null)
            {
                _logger.LogWarning("Subscriber {SubscriberId} not found for deletion", id);
                return NotFound(new { error = "Subscriber not found" });
            }

            await _subscriberRepository.DeleteAsync(subscriber, cancellationToken);
            await _subscriberRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted subscriber {SubscriberId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscriber {SubscriberId}", id);
            return BadRequest(new { error = "Failed to delete subscriber", details = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new API key for a subscriber
    /// </summary>
    /// <param name="id">The subscriber ID</param>
    /// <param name="request">The API key creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created API key</returns>
    [HttpPost("{id}/api-keys")]
    [ProducesResponseType(typeof(ApiKeyCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiKeyCreatedResponse>> CreateApiKey(
        Guid id,
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var subscriber = await _subscriberRepository.GetByIdAsync(id, cancellationToken);
            
            if (subscriber == null)
            {
                _logger.LogWarning("Subscriber {SubscriberId} not found for API key creation", id);
                return NotFound(new { error = "Subscriber not found" });
            }

            var apiKey = await _apiKeyService.GenerateApiKeyAsync(
                subscriber.Id,
                request.Name,
                cancellationToken);

            // Get the created API key entity to return full details
            var apiKeyEntity = await _apiKeyService.GetApiKeyByHashAsync(
                _apiKeyService.HashApiKey(apiKey),
                cancellationToken);

            _logger.LogInformation(
                "Created new API key for subscriber {SubscriberId}",
                subscriber.Id);

            var response = new ApiKeyCreatedResponse
            {
                Id = apiKeyEntity!.Id,
                Name = apiKeyEntity.Name,
                ApiKey = apiKey,
                KeyPrefix = apiKeyEntity.KeyPrefix,
                CreatedAt = apiKeyEntity.CreatedAt
            };

            return CreatedAtAction(
                nameof(GetApiKeys),
                new { id = subscriber.Id },
                response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key for subscriber {SubscriberId}", id);
            return BadRequest(new { error = "Failed to create API key", details = ex.Message });
        }
    }

    /// <summary>
    /// Lists all API keys for a subscriber
    /// </summary>
    /// <param name="id">The subscriber ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of API keys (without the actual key)</returns>
    [HttpGet("{id}/api-keys")]
    [ProducesResponseType(typeof(List<ApiKeyListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ApiKeyListItem>>> GetApiKeys(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var subscriber = await _subscriberRepository.GetByIdAsync(id, cancellationToken);
            
            if (subscriber == null)
            {
                _logger.LogWarning("Subscriber {SubscriberId} not found", id);
                return NotFound(new { error = "Subscriber not found" });
            }

            var apiKeys = await _apiKeyService.GetApiKeysForTenantAsync(id, cancellationToken);

            var response = apiKeys.Select(k => new ApiKeyListItem
            {
                Id = k.Id,
                Name = k.Name,
                KeyPrefix = k.KeyPrefix,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt,
                LastUsedAt = k.LastUsedAt
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API keys for subscriber {SubscriberId}", id);
            return BadRequest(new { error = "Failed to get API keys", details = ex.Message });
        }
    }

    /// <summary>
    /// Revokes (deactivates) an API key
    /// </summary>
    /// <param name="id">The subscriber ID</param>
    /// <param name="keyId">The API key ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}/api-keys/{keyId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeApiKey(
        Guid id,
        Guid keyId,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _apiKeyService.RevokeApiKeyAsync(keyId, cancellationToken);
            
            if (!success)
            {
                _logger.LogWarning("API key {KeyId} not found for subscriber {SubscriberId}", keyId, id);
                return NotFound(new { error = "API key not found" });
            }

            _logger.LogInformation("Revoked API key {KeyId} for subscriber {SubscriberId}", keyId, id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key {KeyId}", keyId);
            return BadRequest(new { error = "Failed to revoke API key", details = ex.Message });
        }
    }
}
