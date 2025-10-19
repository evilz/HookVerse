using Asp.Versioning;
using HookVerse.Api.Models;
using HookVerse.Core.Entities;
using HookVerse.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace HookVerse.Api.Controllers;

/// <summary>
/// Controller for managing event types and their schemas
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/event-types")]
[Produces("application/json")]
public class EventTypesController : ControllerBase
{
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly ISchemaDefinitionRepository _schemaRepository;
    private readonly ILogger<EventTypesController> _logger;

    public EventTypesController(
        IEventTypeRepository eventTypeRepository,
        ISchemaDefinitionRepository schemaRepository,
        ILogger<EventTypesController> logger)
    {
        _eventTypeRepository = eventTypeRepository;
        _schemaRepository = schemaRepository;
        _logger = logger;
    }

    /// <summary>
    /// Create a new event type
    /// </summary>
    /// <param name="request">Event type creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created event type details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(EventTypeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventTypeResponse>> CreateEventType(
        [FromBody] CreateEventTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var subscriberId = GetAuthenticatedSubscriberId();

        _logger.LogInformation(
            "Creating event type {Name} for subscriber {SubscriberId}",
            request.Name, subscriberId);

        // Verify event type name doesn't already exist for this subscriber
        var existing = await _eventTypeRepository.GetByNameAndSubscriberAsync(
            request.Name, 
            subscriberId, 
            cancellationToken);
        
        if (existing != null)
        {
            return Conflict(new { error = "Event type with this name already exists" });
        }

        // Create event type
        var eventType = new EventType
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            SubscriberId = subscriberId,
            CreatedAt = DateTime.UtcNow
        };

        await _eventTypeRepository.AddAsync(eventType, cancellationToken);

        _logger.LogInformation(
            "Event type {EventTypeId} created successfully for subscriber {SubscriberId}",
            eventType.Id, subscriberId);

        var response = MapToResponse(eventType, hasSchema: false);

        return CreatedAtAction(
            nameof(GetEventTypeByName),
            new { name = eventType.Name },
            response);
    }

    /// <summary>
    /// Get all event types for the authenticated subscriber
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page (max 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of event types</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<EventTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResponse<EventTypeResponse>>> GetEventTypes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var subscriberId = GetAuthenticatedSubscriberId();

        pageSize = Math.Min(pageSize, 100); // Cap at 100
        page = Math.Max(page, 1); // Ensure at least page 1

        _logger.LogInformation(
            "Fetching event types for subscriber {SubscriberId}, page {Page}, pageSize {PageSize}",
            subscriberId, page, pageSize);

        var allEventTypes = (await _eventTypeRepository.GetBySubscriberIdAsync(
            subscriberId,
            cancellationToken)).ToList();

        var totalCount = allEventTypes.Count;
        var skip = (page - 1) * pageSize;
        var eventTypes = allEventTypes.Skip(skip).Take(pageSize).ToList();

        // Check which event types have schemas
        var responses = new List<EventTypeResponse>();
        foreach (var eventType in eventTypes)
        {
            var hasSchema = await _schemaRepository.ExistsForEventTypeAsync(
                eventType.Id,
                cancellationToken);

            responses.Add(MapToResponse(eventType, hasSchema));
        }

        var result = new PaginatedResponse<EventTypeResponse>
        {
            Items = responses,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return Ok(result);
    }

    /// <summary>
    /// Get a specific event type by name
    /// </summary>
    /// <param name="name">Event type name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event type details</returns>
    [HttpGet("{name}")]
    [ProducesResponseType(typeof(EventTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventTypeResponse>> GetEventTypeByName(
        string name,
        CancellationToken cancellationToken = default)
    {
        var subscriberId = GetAuthenticatedSubscriberId();

        _logger.LogInformation(
            "Fetching event type {Name} for subscriber {SubscriberId}",
            name, subscriberId);

        var eventType = await _eventTypeRepository.GetByNameAndSubscriberAsync(
            name, 
            subscriberId, 
            cancellationToken);
        
        if (eventType == null)
        {
            return NotFound(new { error = "Event type not found" });
        }

        var hasSchema = await _schemaRepository.ExistsForEventTypeAsync(
            eventType.Id,
            cancellationToken);

        var response = MapToResponse(eventType, hasSchema);

        return Ok(response);
    }

    /// <summary>
    /// Attach or update a schema for an event type
    /// </summary>
    /// <param name="name">Event type name</param>
    /// <param name="request">Schema details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created or updated schema details</returns>
    [HttpPut("{name}/schema")]
    [ProducesResponseType(typeof(SchemaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SchemaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SchemaResponse>> AttachSchema(
        string name,
        [FromBody] AttachSchemaRequest request,
        CancellationToken cancellationToken = default)
    {
        var subscriberId = GetAuthenticatedSubscriberId();

        _logger.LogInformation(
            "Attaching/updating schema for event type {Name}, subscriber {SubscriberId}",
            name, subscriberId);

        // Verify event type exists and belongs to this subscriber
        var eventType = await _eventTypeRepository.GetByNameAndSubscriberAsync(
            name, 
            subscriberId, 
            cancellationToken);
        
        if (eventType == null)
        {
            return NotFound(new { error = "Event type not found" });
        }

        // Calculate content hash
        var contentHash = ComputeSha256Hash(request.Content);

        // Check if schema already exists
        var existingSchema = await _schemaRepository.GetByEventTypeIdAsync(
            eventType.Id,
            cancellationToken);

        SchemaDefinition schema;
        bool isCreating = existingSchema == null;

        if (isCreating)
        {
            // Create new schema
            schema = new SchemaDefinition
            {
                Id = Guid.NewGuid(),
                EventTypeId = eventType.Id,
                Format = request.Format,
                Content = request.Content,
                ContentHash = contentHash,
                Version = request.Version,
                IsActive = request.IsActive,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _schemaRepository.AddAsync(schema, cancellationToken);

            _logger.LogInformation(
                "Schema {SchemaId} created for event type {EventTypeId}",
                schema.Id, eventType.Id);
        }
        else
        {
            // Update existing schema (we know it's not null at this point)
            existingSchema!.Format = request.Format;
            existingSchema.Content = request.Content;
            existingSchema.ContentHash = contentHash;
            existingSchema.Version = request.Version;
            existingSchema.IsActive = request.IsActive;
            existingSchema.Description = request.Description;
            existingSchema.UpdatedAt = DateTime.UtcNow;

            await _schemaRepository.UpdateAsync(existingSchema, cancellationToken);

            schema = existingSchema;

            _logger.LogInformation(
                "Schema {SchemaId} updated for event type {EventTypeId}",
                schema.Id, eventType.Id);
        }

        var response = MapToSchemaResponse(schema);

        if (isCreating)
        {
            return CreatedAtAction(
                nameof(GetSchema),
                new { name = eventType.Name },
                response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Get the schema for an event type
    /// </summary>
    /// <param name="name">Event type name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Schema details</returns>
    [HttpGet("{name}/schema")]
    [ProducesResponseType(typeof(SchemaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SchemaResponse>> GetSchema(
        string name,
        CancellationToken cancellationToken = default)
    {
        var subscriberId = GetAuthenticatedSubscriberId();

        _logger.LogInformation(
            "Fetching schema for event type {Name}, subscriber {SubscriberId}",
            name, subscriberId);

        // Verify event type exists and belongs to this subscriber
        var eventType = await _eventTypeRepository.GetByNameAndSubscriberAsync(
            name, 
            subscriberId, 
            cancellationToken);
        
        if (eventType == null)
        {
            return NotFound(new { error = "Event type not found" });
        }

        // Get schema
        var schema = await _schemaRepository.GetActiveByEventTypeIdAsync(
            eventType.Id,
            cancellationToken);

        if (schema == null)
        {
            return NotFound(new { error = "No schema defined for this event type" });
        }

        var response = MapToSchemaResponse(schema);

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

    private static EventTypeResponse MapToResponse(EventType eventType, bool hasSchema)
    {
        return new EventTypeResponse
        {
            Id = eventType.Id,
            Name = eventType.Name,
            Description = eventType.Description,
            SubscriberId = eventType.SubscriberId,
            HasSchema = hasSchema,
            CreatedAt = eventType.CreatedAt,
            UpdatedAt = eventType.UpdatedAt
        };
    }

    private static SchemaResponse MapToSchemaResponse(SchemaDefinition schema)
    {
        return new SchemaResponse
        {
            Id = schema.Id,
            EventTypeId = schema.EventTypeId,
            Format = schema.Format,
            Content = schema.Content,
            ContentHash = schema.ContentHash,
            Version = schema.Version,
            IsActive = schema.IsActive,
            Description = schema.Description,
            LastValidatedAt = schema.LastValidatedAt,
            CreatedAt = schema.CreatedAt,
            UpdatedAt = schema.UpdatedAt
        };
    }

    private static string ComputeSha256Hash(string content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
