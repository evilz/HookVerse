using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HookVerse.Infrastructure.Services;

/// <summary>
/// Configuration options for encryption
/// </summary>
public class EncryptionOptions
{
    /// <summary>
    /// Current active encryption key (base64 encoded)
    /// </summary>
    public string CurrentKey { get; set; } = string.Empty;

    /// <summary>
    /// Previous encryption keys for decryption of old data (base64 encoded, comma-separated)
    /// </summary>
    public string PreviousKeys { get; set; } = string.Empty;

    /// <summary>
    /// Current key version
    /// </summary>
    public int CurrentKeyVersion { get; set; } = 1;

    /// <summary>
    /// Algorithm to use (AES-256-GCM)
    /// </summary>
    public string Algorithm { get; set; } = "AES-256-GCM";
}

/// <summary>
/// Encrypted data with version information
/// </summary>
public class EncryptedData
{
    public int KeyVersion { get; set; }
    public byte[] Ciphertext { get; set; } = Array.Empty<byte>();
    public byte[] Nonce { get; set; } = Array.Empty<byte>();
    public byte[] Tag { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Serialize to storable format: version|nonce|tag|ciphertext (all base64)
    /// </summary>
    public string ToStorageFormat()
    {
        return $"{KeyVersion}|" +
               $"{Convert.ToBase64String(Nonce)}|" +
               $"{Convert.ToBase64String(Tag)}|" +
               $"{Convert.ToBase64String(Ciphertext)}";
    }

    /// <summary>
    /// Deserialize from storage format
    /// </summary>
    public static EncryptedData FromStorageFormat(string storageFormat)
    {
        var parts = storageFormat.Split('|');
        if (parts.Length != 4)
        {
            throw new ArgumentException("Invalid storage format");
        }

        return new EncryptedData
        {
            KeyVersion = int.Parse(parts[0]),
            Nonce = Convert.FromBase64String(parts[1]),
            Tag = Convert.FromBase64String(parts[2]),
            Ciphertext = Convert.FromBase64String(parts[3])
        };
    }
}

/// <summary>
/// Service for encrypting and decrypting sensitive data with key versioning
/// </summary>
public class EncryptionService
{
    private readonly ILogger<EncryptionService> _logger;
    private readonly EncryptionOptions _options;
    private readonly Dictionary<int, byte[]> _keyCache;
    private const int NonceSize = 12; // 96 bits recommended for GCM
    private const int TagSize = 16;   // 128 bits authentication tag

    public EncryptionService(
        ILogger<EncryptionService> logger,
        IOptions<EncryptionOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _keyCache = new Dictionary<int, byte[]>();

        ValidateConfiguration();
        InitializeKeyCache();
    }

    /// <summary>
    /// Encrypt data using the current encryption key
    /// </summary>
    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            throw new ArgumentException("Plaintext cannot be null or empty", nameof(plaintext));
        }

        try
        {
            var key = _keyCache[_options.CurrentKeyVersion];
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            
            // Generate random nonce
            var nonce = new byte[NonceSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }

            // Encrypt using AES-256-GCM
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[TagSize];

            using (var aes = new AesGcm(key))
            {
                aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
            }

            var encryptedData = new EncryptedData
            {
                KeyVersion = _options.CurrentKeyVersion,
                Ciphertext = ciphertext,
                Nonce = nonce,
                Tag = tag
            };

            return encryptedData.ToStorageFormat();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data");
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    /// <summary>
    /// Decrypt data using the appropriate versioned key
    /// </summary>
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            throw new ArgumentException("Encrypted text cannot be null or empty", nameof(encryptedText));
        }

        try
        {
            var encryptedData = EncryptedData.FromStorageFormat(encryptedText);

            if (!_keyCache.TryGetValue(encryptedData.KeyVersion, out var key))
            {
                throw new InvalidOperationException(
                    $"Encryption key version {encryptedData.KeyVersion} not found. " +
                    "This data may have been encrypted with a key that is no longer available.");
            }

            var plaintext = new byte[encryptedData.Ciphertext.Length];

            using (var aes = new AesGcm(key))
            {
                aes.Decrypt(
                    encryptedData.Nonce,
                    encryptedData.Ciphertext,
                    encryptedData.Tag,
                    plaintext);
            }

            return Encoding.UTF8.GetString(plaintext);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Decryption failed - data may be corrupted or tampered");
            throw new InvalidOperationException("Decryption failed", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting data");
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }

    /// <summary>
    /// Check if encrypted data needs re-encryption with current key
    /// </summary>
    public bool NeedsReEncryption(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return false;
        }

        try
        {
            var encryptedData = EncryptedData.FromStorageFormat(encryptedText);
            return encryptedData.KeyVersion != _options.CurrentKeyVersion;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Re-encrypt data with the current encryption key
    /// </summary>
    public string ReEncrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            throw new ArgumentException("Encrypted text cannot be null or empty", nameof(encryptedText));
        }

        // Decrypt with old key, encrypt with new key
        var plaintext = Decrypt(encryptedText);
        return Encrypt(plaintext);
    }

    /// <summary>
    /// Get the key version used for encrypted data
    /// </summary>
    public int GetKeyVersion(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return 0;
        }

        try
        {
            var encryptedData = EncryptedData.FromStorageFormat(encryptedText);
            return encryptedData.KeyVersion;
        }
        catch
        {
            return 0;
        }
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.CurrentKey))
        {
            throw new InvalidOperationException("Current encryption key is not configured");
        }

        try
        {
            var keyBytes = Convert.FromBase64String(_options.CurrentKey);
            if (keyBytes.Length != 32) // 256 bits
            {
                throw new InvalidOperationException("Encryption key must be 256 bits (32 bytes)");
            }
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Encryption key must be valid base64");
        }
    }

    private void InitializeKeyCache()
    {
        // Add current key
        var currentKeyBytes = Convert.FromBase64String(_options.CurrentKey);
        _keyCache[_options.CurrentKeyVersion] = currentKeyBytes;

        _logger.LogInformation(
            "Initialized encryption with key version {Version}",
            _options.CurrentKeyVersion);

        // Add previous keys if configured
        if (!string.IsNullOrWhiteSpace(_options.PreviousKeys))
        {
            var previousKeyPairs = _options.PreviousKeys.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var keyPair in previousKeyPairs)
            {
                var parts = keyPair.Split(':', 2);
                if (parts.Length == 2 && 
                    int.TryParse(parts[0].Trim(), out var version) &&
                    !string.IsNullOrWhiteSpace(parts[1]))
                {
                    try
                    {
                        var keyBytes = Convert.FromBase64String(parts[1].Trim());
                        if (keyBytes.Length == 32)
                        {
                            _keyCache[version] = keyBytes;
                            _logger.LogInformation("Loaded previous encryption key version {Version}", version);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Skipping invalid previous key version {Version} - must be 256 bits",
                                version);
                        }
                    }
                    catch (FormatException)
                    {
                        _logger.LogWarning(
                            "Skipping invalid previous key version {Version} - invalid base64",
                            version);
                    }
                }
            }
        }

        _logger.LogInformation(
            "Encryption service initialized with {Count} key version(s)",
            _keyCache.Count);
    }
}
