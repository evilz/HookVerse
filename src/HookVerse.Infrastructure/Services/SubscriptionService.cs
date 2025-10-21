using HookVerse.Core.Entities;
using HookVerse.Core.Enums;
using HookVerse.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.RegularExpressions;

namespace HookVerse.Infrastructure.Services;

/// <summary>
/// Service for managing webhook subscriptions with business rule validation
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<SubscriptionService> _logger;

    // Private IP ranges (RFC 1918, RFC 4193, loopback)
    private static readonly string[] PrivateIpPatterns = new[]
    {
        @"^10\.",                          // 10.0.0.0/8
        @"^172\.(1[6-9]|2[0-9]|3[0-1])\.", // 172.16.0.0/12
        @"^192\.168\.",                    // 192.168.0.0/16
        @"^127\.",                         // 127.0.0.0/8 (loopback)
        @"^169\.254\.",                    // 169.254.0.0/16 (link-local)
        @"^fc00:",                         // fc00::/7 (IPv6 unique local)
        @"^fe80:",                         // fe80::/10 (IPv6 link-local)
        @"^::1$",                          // ::1 (IPv6 loopback)
    };

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        ILogger<SubscriptionService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }

    public async Task<Subscription> CreateSubscriptionAsync(
        Guid subscriberId,
        Guid eventTypeId,
        string endpointUrl,
        string secret,
        AuthType authType,
        string? authConfig,
        string? description,
        int timeoutSeconds,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        // Validate endpoint URL
        ValidateEndpointUrl(endpointUrl);

        // Validate secret
        ValidateSecret(secret);

        // Validate timeout and retries
        ValidateTimeout(timeoutSeconds);
        ValidateMaxRetries(maxRetries);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriberId,
            EventTypeId = eventTypeId,
            EndpointUrl = endpointUrl,
            Secret = secret, // TODO: Should be hashed/encrypted in production
            AuthType = authType,
            AuthConfig = authConfig,
            Description = description,
            TimeoutSeconds = timeoutSeconds,
            MaxRetries = maxRetries,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _subscriptionRepository.AddAsync(subscription, cancellationToken);

        _logger.LogInformation(
            "Created subscription {SubscriptionId} for subscriber {SubscriberId} to event type {EventTypeId}",
            subscription.Id, subscriberId, eventTypeId);

        return subscription;
    }

    public async Task<Subscription> UpdateSubscriptionAsync(
        Subscription subscription,
        string? endpointUrl,
        string? secret,
        AuthType? authType,
        string? authConfig,
        string? description,
        int? timeoutSeconds,
        int? maxRetries,
        CancellationToken cancellationToken = default)
    {
        var hasChanges = false;

        if (endpointUrl != null && endpointUrl != subscription.EndpointUrl)
        {
            ValidateEndpointUrl(endpointUrl);
            subscription.EndpointUrl = endpointUrl;
            hasChanges = true;
        }

        if (secret != null && secret != subscription.Secret)
        {
            ValidateSecret(secret);
            subscription.Secret = secret; // TODO: Should be hashed/encrypted in production
            hasChanges = true;
        }

        if (authType.HasValue && authType.Value != subscription.AuthType)
        {
            subscription.AuthType = authType.Value;
            hasChanges = true;
        }

        if (authConfig != null && authConfig != subscription.AuthConfig)
        {
            subscription.AuthConfig = authConfig;
            hasChanges = true;
        }

        if (description != null && description != subscription.Description)
        {
            subscription.Description = description;
            hasChanges = true;
        }

        if (timeoutSeconds.HasValue && timeoutSeconds.Value != subscription.TimeoutSeconds)
        {
            ValidateTimeout(timeoutSeconds.Value);
            subscription.TimeoutSeconds = timeoutSeconds.Value;
            hasChanges = true;
        }

        if (maxRetries.HasValue && maxRetries.Value != subscription.MaxRetries)
        {
            ValidateMaxRetries(maxRetries.Value);
            subscription.MaxRetries = maxRetries.Value;
            hasChanges = true;
        }

        if (hasChanges)
        {
            subscription.UpdatedAt = DateTime.UtcNow;
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

            _logger.LogInformation(
                "Updated subscription {SubscriptionId}",
                subscription.Id);
        }

        return subscription;
    }

    private static void ValidateEndpointUrl(string endpointUrl)
    {
        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            throw new ArgumentException("Endpoint URL is required", nameof(endpointUrl));
        }

        // Must be valid URL
        if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Endpoint URL must be a valid absolute URL", nameof(endpointUrl));
        }

        // Must be HTTPS
        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Endpoint URL must use HTTPS protocol", nameof(endpointUrl));
        }

        // Must not be a private IP (SSRF protection)
        var host = uri.Host;
        
        // Try to resolve hostname to IP
        try
        {
            var addresses = Dns.GetHostAddresses(host);
            foreach (var address in addresses)
            {
                var ipString = address.ToString();
                foreach (var pattern in PrivateIpPatterns)
                {
                    if (Regex.IsMatch(ipString, pattern, RegexOptions.IgnoreCase))
                    {
                        throw new ArgumentException(
                            "Endpoint URL must not resolve to a private IP address (SSRF protection)",
                            nameof(endpointUrl));
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            // DNS resolution failed - allow for now but log warning
            // In production, you might want to fail here
        }
    }

    private static void ValidateSecret(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ArgumentException("Secret is required", nameof(secret));
        }

        if (secret.Length < 32)
        {
            throw new ArgumentException(
                "Secret must be at least 32 characters long for security",
                nameof(secret));
        }
    }

    private static void ValidateTimeout(int timeoutSeconds)
    {
        if (timeoutSeconds < 1 || timeoutSeconds > 300)
        {
            throw new ArgumentException(
                "Timeout must be between 1 and 300 seconds",
                nameof(timeoutSeconds));
        }
    }

    private static void ValidateMaxRetries(int maxRetries)
    {
        if (maxRetries < 0 || maxRetries > 10)
        {
            throw new ArgumentException(
                "Max retries must be between 0 and 10",
                nameof(maxRetries));
        }
    }
}
