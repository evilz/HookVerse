using HookVerse.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace HookVerse.Infrastructure.Services;

/// <summary>
/// HMAC-SHA256 signature service for webhook payload signing.
/// </summary>
public class HmacSignatureService : ISignatureService
{
    public string GenerateSignature(string payload, string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload, nameof(payload));
        ArgumentException.ThrowIfNullOrWhiteSpace(secret, nameof(secret));

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public bool ValidateSignature(string payload, string secret, string signature)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload, nameof(payload));
        ArgumentException.ThrowIfNullOrWhiteSpace(secret, nameof(secret));
        ArgumentException.ThrowIfNullOrWhiteSpace(signature, nameof(signature));

        var expectedSignature = GenerateSignature(payload, secret);
        
        // Use constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant())
        );
    }
}
