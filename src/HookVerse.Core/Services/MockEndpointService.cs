using HookVerse.Core.Entities;
using HookVerse.Core.Interfaces;
using System.Text.Json;

namespace HookVerse.Core.Services;

/// <summary>
/// Service for mock endpoint management and request handling.
/// </summary>
public class MockEndpointService : IMockEndpointService
{
    private readonly IMockEndpointRepository _mockEndpointRepository;
    private readonly IRepository<MockEndpointRequest> _mockEndpointRequestRepository;

    public MockEndpointService(
        IMockEndpointRepository mockEndpointRepository,
        IRepository<MockEndpointRequest> mockEndpointRequestRepository)
    {
        _mockEndpointRepository = mockEndpointRepository;
        _mockEndpointRequestRepository = mockEndpointRequestRepository;
    }

    /// <inheritdoc />
    public async Task<MockEndpoint> CreateMockEndpointAsync(
        Guid subscriberId,
        string name,
        string? description,
        int responseStatus,
        string? responseBody,
        string responseContentType,
        int responseDelayMs,
        Dictionary<string, string>? responseHeaders,
        CancellationToken cancellationToken = default)
    {
        var mockEndpoint = new MockEndpoint
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriberId,
            Name = name,
            Description = description,
            UrlPath = $"/mock/{Guid.NewGuid():N}",
            ResponseStatus = responseStatus,
            ResponseBody = responseBody,
            ResponseContentType = responseContentType,
            ResponseDelayMs = responseDelayMs,
            ResponseHeaders = responseHeaders != null ? JsonSerializer.Serialize(responseHeaders) : null,
            IsActive = true,
            RequestCount = 0
        };

        await _mockEndpointRepository.AddAsync(mockEndpoint, cancellationToken);
        return mockEndpoint;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MockEndpoint>> GetMockEndpointsAsync(Guid subscriberId, CancellationToken cancellationToken = default)
    {
        return await _mockEndpointRepository.GetBySubscriberIdAsync(subscriberId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MockEndpoint?> GetMockEndpointAsync(Guid id, Guid subscriberId, CancellationToken cancellationToken = default)
    {
        var mockEndpoint = await _mockEndpointRepository.GetByIdAsync(id, cancellationToken);
        
        if (mockEndpoint == null || mockEndpoint.SubscriberId != subscriberId)
        {
            return null;
        }

        return mockEndpoint;
    }

    /// <inheritdoc />
    public async Task<MockEndpoint?> UpdateMockEndpointAsync(
        Guid id,
        Guid subscriberId,
        string? name,
        string? description,
        int? responseStatus,
        string? responseBody,
        string? responseContentType,
        int? responseDelayMs,
        Dictionary<string, string>? responseHeaders,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var mockEndpoint = await _mockEndpointRepository.GetByIdAsync(id, cancellationToken);
        
        if (mockEndpoint == null || mockEndpoint.SubscriberId != subscriberId)
        {
            return null;
        }

        if (name != null) mockEndpoint.Name = name;
        if (description != null) mockEndpoint.Description = description;
        if (responseStatus.HasValue) mockEndpoint.ResponseStatus = responseStatus.Value;
        if (responseBody != null) mockEndpoint.ResponseBody = responseBody;
        if (responseContentType != null) mockEndpoint.ResponseContentType = responseContentType;
        if (responseDelayMs.HasValue) mockEndpoint.ResponseDelayMs = responseDelayMs.Value;
        if (responseHeaders != null) mockEndpoint.ResponseHeaders = JsonSerializer.Serialize(responseHeaders);
        if (isActive.HasValue) mockEndpoint.IsActive = isActive.Value;

        await _mockEndpointRepository.UpdateAsync(mockEndpoint, cancellationToken);
        return mockEndpoint;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteMockEndpointAsync(Guid id, Guid subscriberId, CancellationToken cancellationToken = default)
    {
        var mockEndpoint = await _mockEndpointRepository.GetByIdAsync(id, cancellationToken);
        
        if (mockEndpoint == null || mockEndpoint.SubscriberId != subscriberId)
        {
            return false;
        }

        await _mockEndpointRepository.DeleteAsync(mockEndpoint, cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<(int Status, string? Body, string ContentType, Dictionary<string, string>? Headers, int DelayMs)> HandleMockRequestAsync(
        string urlPath,
        string method,
        string? queryString,
        Dictionary<string, string> headers,
        string? body,
        string? contentType,
        string? clientIp,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        // Find the mock endpoint
        var mockEndpoint = await _mockEndpointRepository.GetByUrlPathAsync(urlPath, cancellationToken);
        
        if (mockEndpoint == null || !mockEndpoint.IsActive)
        {
            return (404, "{\"error\":\"Mock endpoint not found\"}", "application/json", null, 0);
        }

        // Log the request
        var mockRequest = new MockEndpointRequest
        {
            Id = Guid.NewGuid(),
            MockEndpointId = mockEndpoint.Id,
            Method = method,
            Path = urlPath,
            QueryString = queryString,
            Headers = JsonSerializer.Serialize(headers),
            Body = body,
            ContentType = contentType,
            ClientIp = clientIp,
            UserAgent = userAgent,
            ResponseStatus = mockEndpoint.ResponseStatus,
            ResponseBody = mockEndpoint.ResponseBody,
            ReceivedAt = DateTime.UtcNow
        };

        await _mockEndpointRequestRepository.AddAsync(mockRequest, cancellationToken);

        // Increment request count
        await _mockEndpointRepository.IncrementRequestCountAsync(mockEndpoint.Id, cancellationToken);

        // Parse response headers
        Dictionary<string, string>? responseHeaders = null;
        if (!string.IsNullOrEmpty(mockEndpoint.ResponseHeaders))
        {
            try
            {
                responseHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(mockEndpoint.ResponseHeaders);
            }
            catch
            {
                // Ignore invalid JSON
            }
        }

        return (
            mockEndpoint.ResponseStatus,
            mockEndpoint.ResponseBody,
            mockEndpoint.ResponseContentType,
            responseHeaders,
            mockEndpoint.ResponseDelayMs
        );
    }
}
