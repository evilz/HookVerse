namespace HookVerse.Core.Enums;

/// <summary>
/// Delivery status for webhook delivery attempts.
/// </summary>
public enum DeliveryStatus
{
    Pending = 0,
    Delivering = 1,
    Delivered = 2,      // 2xx response
    Failed = 3,         // 4xx/5xx response
    Timeout = 4,        // Request timeout
    CircuitOpen = 5,    // Circuit breaker open
    Rejected = 6,       // SSRF or validation failure
    DeadLetter = 7      // Exhausted all retries
}
