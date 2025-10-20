using HookVerse.Core.Entities;
using HookVerse.Core.Interfaces;
using HookVerse.Core.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HookVerse.Core.Services;

/// <summary>
/// Service for GDPR compliance operations.
/// </summary>
public class GdprService : IGdprService
{
    private readonly IGdprRequestRepository _gdprRequestRepository;
    private readonly ISubscriberRepository _subscriberRepository;
    private readonly IWebhookEventRepository _webhookEventRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IDeliveryAttemptRepository _deliveryAttemptRepository;
    private readonly IMockEndpointRepository _mockEndpointRepository;
    private readonly IRepository<MockEndpointRequest> _mockEndpointRequestRepository;
    private readonly ILogger<GdprService> _logger;

    public GdprService(
        IGdprRequestRepository gdprRequestRepository,
        ISubscriberRepository subscriberRepository,
        IWebhookEventRepository webhookEventRepository,
        ISubscriptionRepository subscriptionRepository,
        IDeliveryAttemptRepository deliveryAttemptRepository,
        IMockEndpointRepository mockEndpointRepository,
        IRepository<MockEndpointRequest> mockEndpointRequestRepository,
        ILogger<GdprService> logger)
    {
        _gdprRequestRepository = gdprRequestRepository;
        _subscriberRepository = subscriberRepository;
        _webhookEventRepository = webhookEventRepository;
        _subscriptionRepository = subscriptionRepository;
        _deliveryAttemptRepository = deliveryAttemptRepository;
        _mockEndpointRepository = mockEndpointRepository;
        _mockEndpointRequestRepository = mockEndpointRequestRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GdprRequest> CreateExportRequestAsync(Guid subscriberId, string? metadata, CancellationToken cancellationToken = default)
    {
        var request = new GdprRequest
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriberId,
            RequestType = GdprRequestType.Export,
            Status = GdprRequestStatus.Pending,
            Metadata = metadata
        };

        await _gdprRequestRepository.AddAsync(request, cancellationToken);
        
        _logger.LogInformation(
            "Created GDPR export request {RequestId} for subscriber {SubscriberId}",
            request.Id, subscriberId);

        return request;
    }

    /// <inheritdoc />
    public async Task<GdprRequest> CreateDeleteRequestAsync(Guid subscriberId, string? metadata, CancellationToken cancellationToken = default)
    {
        var request = new GdprRequest
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriberId,
            RequestType = GdprRequestType.Delete,
            Status = GdprRequestStatus.Pending,
            Metadata = metadata
        };

        await _gdprRequestRepository.AddAsync(request, cancellationToken);
        
        _logger.LogInformation(
            "Created GDPR delete request {RequestId} for subscriber {SubscriberId}",
            request.Id, subscriberId);

        return request;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GdprRequest>> GetRequestsAsync(Guid subscriberId, CancellationToken cancellationToken = default)
    {
        return await _gdprRequestRepository.GetBySubscriberIdAsync(subscriberId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GdprRequest?> GetRequestAsync(Guid id, Guid subscriberId, CancellationToken cancellationToken = default)
    {
        var request = await _gdprRequestRepository.GetByIdAsync(id, cancellationToken);
        
        if (request == null || request.SubscriberId != subscriberId)
        {
            return null;
        }

        return request;
    }

    /// <inheritdoc />
    public async Task<(Stream? FileStream, string? FileName)> GetExportFileAsync(Guid id, Guid subscriberId, CancellationToken cancellationToken = default)
    {
        var request = await _gdprRequestRepository.GetByIdAsync(id, cancellationToken);
        
        if (request == null || request.SubscriberId != subscriberId)
        {
            return (null, null);
        }

        if (request.RequestType != GdprRequestType.Export)
        {
            throw new InvalidOperationException("Request is not an export request");
        }

        if (request.Status != GdprRequestStatus.Completed)
        {
            throw new InvalidOperationException("Export is not yet completed");
        }

        if (string.IsNullOrEmpty(request.ExportFilePath))
        {
            return (null, null);
        }

        if (!File.Exists(request.ExportFilePath))
        {
            _logger.LogWarning("Export file not found: {FilePath}", request.ExportFilePath);
            return (null, null);
        }

        var fileStream = new FileStream(request.ExportFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var fileName = $"gdpr-export-{subscriberId:N}-{request.CreatedAt:yyyyMMdd}.json";

        return (fileStream, fileName);
    }

    /// <inheritdoc />
    public async Task ProcessExportRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await _gdprRequestRepository.GetByIdAsync(requestId, cancellationToken);
        
        if (request == null)
        {
            _logger.LogWarning("Export request {RequestId} not found", requestId);
            return;
        }

        if (request.Status != GdprRequestStatus.Pending)
        {
            _logger.LogWarning("Export request {RequestId} is not pending (status: {Status})", requestId, request.Status);
            return;
        }

        try
        {
            // Update status to processing
            await _gdprRequestRepository.UpdateStatusAsync(requestId, GdprRequestStatus.Processing, cancellationToken: cancellationToken);

            _logger.LogInformation("Processing GDPR export request {RequestId} for subscriber {SubscriberId}", requestId, request.SubscriberId);

            // Gather all subscriber data
            var exportData = await GatherSubscriberDataAsync(request.SubscriberId, cancellationToken);

            // Serialize to JSON
            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Create export directory if it doesn't exist
            var exportDir = Path.Combine(Directory.GetCurrentDirectory(), "gdpr-exports");
            Directory.CreateDirectory(exportDir);

            // Write to file
            var fileName = $"gdpr-export-{request.SubscriberId:N}-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            var filePath = Path.Combine(exportDir, fileName);
            
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            var fileSize = new FileInfo(filePath).Length;

            // Update status to completed
            await _gdprRequestRepository.UpdateStatusAsync(
                requestId,
                GdprRequestStatus.Completed,
                exportFilePath: filePath,
                exportFileSizeBytes: fileSize,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Completed GDPR export request {RequestId} for subscriber {SubscriberId}. File size: {FileSize} bytes",
                requestId, request.SubscriberId, fileSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GDPR export request {RequestId}", requestId);
            
            await _gdprRequestRepository.UpdateStatusAsync(
                requestId,
                GdprRequestStatus.Failed,
                errorMessage: ex.Message,
                cancellationToken: cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task ProcessDeleteRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await _gdprRequestRepository.GetByIdAsync(requestId, cancellationToken);
        
        if (request == null)
        {
            _logger.LogWarning("Delete request {RequestId} not found", requestId);
            return;
        }

        if (request.Status != GdprRequestStatus.Pending)
        {
            _logger.LogWarning("Delete request {RequestId} is not pending (status: {Status})", requestId, request.Status);
            return;
        }

        try
        {
            // Update status to processing
            await _gdprRequestRepository.UpdateStatusAsync(requestId, GdprRequestStatus.Processing, cancellationToken: cancellationToken);

            _logger.LogInformation("Processing GDPR delete request {RequestId} for subscriber {SubscriberId}", requestId, request.SubscriberId);

            // Delete personal data while preserving audit trails
            await DeleteSubscriberPersonalDataAsync(request.SubscriberId, cancellationToken);

            // Update status to completed
            await _gdprRequestRepository.UpdateStatusAsync(
                requestId,
                GdprRequestStatus.Completed,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Completed GDPR delete request {RequestId} for subscriber {SubscriberId}",
                requestId, request.SubscriberId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GDPR delete request {RequestId}", requestId);
            
            await _gdprRequestRepository.UpdateStatusAsync(
                requestId,
                GdprRequestStatus.Failed,
                errorMessage: ex.Message,
                cancellationToken: cancellationToken);
        }
    }

    private async Task<object> GatherSubscriberDataAsync(Guid subscriberId, CancellationToken cancellationToken)
    {
        // Gather all data related to the subscriber
        var subscriber = await _subscriberRepository.GetByIdAsync(subscriberId, cancellationToken);
        var webhookEvents = await _webhookEventRepository.GetBySubscriberIdAsync(subscriberId, cancellationToken);
        var subscriptions = await _subscriptionRepository.GetBySubscriberIdAsync(subscriberId, cancellationToken);
        var mockEndpoints = await _mockEndpointRepository.GetBySubscriberIdAsync(subscriberId, cancellationToken);

        // Get delivery attempts for all webhook events
        var webhookEventIds = webhookEvents.Select(w => w.Id).ToList();
        var deliveryAttempts = new List<DeliveryAttempt>();
        foreach (var eventId in webhookEventIds)
        {
            var attempts = await _deliveryAttemptRepository.GetByWebhookEventIdAsync(eventId, cancellationToken);
            deliveryAttempts.AddRange(attempts);
        }

        // Get mock endpoint requests
        var mockEndpointIds = mockEndpoints.Select(m => m.Id).ToList();
        var mockRequests = new List<MockEndpointRequest>();
        foreach (var endpointId in mockEndpointIds)
        {
            var requests = await _mockEndpointRequestRepository.GetAllAsync(cancellationToken);
            mockRequests.AddRange(requests.Where(r => r.MockEndpointId == endpointId));
        }

        return new
        {
            ExportDate = DateTime.UtcNow,
            SubscriberId = subscriberId,
            Subscriber = new
            {
                subscriber?.Id,
                subscriber?.Name,
                subscriber?.CreatedAt,
                subscriber?.UpdatedAt
            },
            WebhookEvents = webhookEvents.Select(w => new
            {
                w.Id,
                w.EventTypeId,
                w.Payload,
                w.ScheduledFor,
                w.TraceId,
                w.Metadata,
                w.CreatedAt
            }),
            Subscriptions = subscriptions.Select(s => new
            {
                s.Id,
                s.EventTypeId,
                s.EndpointUrl,
                s.AuthType,
                s.IsActive,
                s.MaxRetries,
                s.TimeoutSeconds,
                s.CreatedAt,
                s.UpdatedAt
            }),
            DeliveryAttempts = deliveryAttempts.Select(d => new
            {
                d.Id,
                d.WebhookEventId,
                d.AttemptNumber,
                d.Status,
                d.ResponseStatus,
                d.DurationMs,
                d.ErrorMessage,
                d.StartedAt,
                d.CompletedAt
            }),
            MockEndpoints = mockEndpoints.Select(m => new
            {
                m.Id,
                m.Name,
                m.Description,
                m.UrlPath,
                m.ResponseStatus,
                m.ResponseContentType,
                m.ResponseDelayMs,
                m.IsActive,
                m.RequestCount,
                m.LastRequestAt,
                m.CreatedAt
            }),
            MockEndpointRequests = mockRequests.Select(r => new
            {
                r.Id,
                r.MockEndpointId,
                r.Method,
                r.Path,
                r.QueryString,
                r.ContentType,
                r.ClientIp,
                r.UserAgent,
                r.ResponseStatus,
                r.ReceivedAt
            })
        };
    }

    private async Task DeleteSubscriberPersonalDataAsync(Guid subscriberId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting personal data for subscriber {SubscriberId}", subscriberId);

        // Delete subscriptions (contains endpoint URLs which may have personal data)
        var subscriptions = await _subscriptionRepository.GetBySubscriberIdAsync(subscriberId, cancellationToken);
        foreach (var subscription in subscriptions)
        {
            await _subscriptionRepository.DeleteAsync(subscription, cancellationToken);
        }

        // Delete mock endpoints
        var mockEndpoints = await _mockEndpointRepository.GetBySubscriberIdAsync(subscriberId, cancellationToken);
        foreach (var endpoint in mockEndpoints)
        {
            await _mockEndpointRepository.DeleteAsync(endpoint, cancellationToken);
        }

        // Anonymize webhook event payloads and metadata (preserve audit trail)
        var webhookEvents = await _webhookEventRepository.GetBySubscriberIdAsync(subscriberId, cancellationToken);
        foreach (var webhookEvent in webhookEvents)
        {
            webhookEvent.Payload = "{\"redacted\": true}";
            webhookEvent.Metadata = "{\"redacted\": true}";
            await _webhookEventRepository.UpdateAsync(webhookEvent, cancellationToken);
        }

        // Note: We preserve DeliveryAttempts for audit trail (they don't contain personal data)
        // Note: We preserve the Subscriber entity itself for referential integrity (just anonymize name if needed)
        var subscriber = await _subscriberRepository.GetByIdAsync(subscriberId, cancellationToken);
        if (subscriber != null)
        {
            subscriber.Name = $"Deleted User {subscriberId:N}";
            await _subscriberRepository.UpdateAsync(subscriber, cancellationToken);
        }

        _logger.LogInformation("Completed personal data deletion for subscriber {SubscriberId}", subscriberId);
    }
}
