namespace HookVerse.Core.Interfaces;

/// <summary>
/// Service interface for generating webhook signatures.
/// </summary>
public interface ISignatureService
{
    /// <summary>
    /// Generates an HMAC-SHA256 signature for the payload.
    /// </summary>
    /// <param name="payload">The payload to sign.</param>
    /// <param name="secret">The secret key.</param>
    /// <returns>The hex-encoded signature.</returns>
    string GenerateSignature(string payload, string secret);

    /// <summary>
    /// Validates an HMAC-SHA256 signature.
    /// </summary>
    /// <param name="payload">The original payload.</param>
    /// <param name="secret">The secret key.</param>
    /// <param name="signature">The signature to validate.</param>
    /// <returns>True if signature is valid, otherwise false.</returns>
    bool ValidateSignature(string payload, string secret, string signature);
}
