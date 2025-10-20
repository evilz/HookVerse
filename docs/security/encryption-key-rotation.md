# Encryption Key Rotation

HookVerse implements encryption key rotation with automatic re-encryption to enhance security for sensitive data at rest.

## Overview

Encryption key rotation involves:

1. **Versioned Keys**: Each encryption key has a version number
2. **Backward Compatibility**: Old keys are retained to decrypt existing data
3. **Automatic Re-encryption**: Background process re-encrypts data with new keys
4. **Zero Downtime**: Rotation happens without service interruption

## What Gets Encrypted

HookVerse encrypts the following sensitive data at rest:

| Data | Entity | Field | Purpose |
|------|--------|-------|---------|
| Webhook Payload | `WebhookEvent` | `Payload` | Event data sent to subscribers |
| Subscription Secret | `Subscription` | `Secret` | HMAC signature secret |
| Custom Headers | `Subscription` | `CustomHeaders` | Custom HTTP headers (may contain auth tokens) |
| Auth Configuration | `Subscription` | `AuthConfig` | Authentication configuration (credentials) |

## Encryption Algorithm

- **Algorithm**: AES-256-GCM (Galois/Counter Mode)
- **Key Size**: 256 bits (32 bytes)
- **Nonce**: 96 bits (12 bytes), randomly generated per encryption
- **Authentication Tag**: 128 bits (16 bytes)

**Storage Format**:
```
{version}|{nonce_base64}|{tag_base64}|{ciphertext_base64}
```

Example:
```
1|abc123xyz789==|def456uvw123==|ghijklmno789pqrstu123...
```

## Configuration

### Application Settings

Configure encryption in `appsettings.json`:

```json
{
  "Encryption": {
    "CurrentKey": "BASE64_ENCODED_32_BYTE_KEY",
    "CurrentKeyVersion": 2,
    "PreviousKeys": "1:OLD_BASE64_KEY",
    "Algorithm": "AES-256-GCM"
  },
  "EncryptionKeyRotation": {
    "MaxKeyLifetime": "180.00:00:00",
    "ReEncryptionBatchSize": 100,
    "AutoReEncryptOnRotation": true,
    "ReEncryptionBatchTimeout": "00:00:30"
  }
}
```

### Kubernetes Secrets

For production, store keys in Kubernetes secrets:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: hookverse-encryption-keys
  namespace: hookverse
type: Opaque
data:
  current-key: "BASE64_ENCODED_KEY"  # Base64 encode the already base64-encoded key
  current-key-version: "Mg=="        # Base64("2")
  previous-keys: "MTo..."            # Base64("1:OLD_KEY")
```

Mount in deployment:

```yaml
env:
  - name: Encryption__CurrentKey
    valueFrom:
      secretKeyRef:
        name: hookverse-encryption-keys
        key: current-key
  - name: Encryption__CurrentKeyVersion
    valueFrom:
      secretKeyRef:
        name: hookverse-encryption-keys
        key: current-key-version
  - name: Encryption__PreviousKeys
    valueFrom:
      secretKeyRef:
        name: hookverse-encryption-keys
        key: previous-keys
```

## Key Generation

### Generate New Encryption Key

Using PowerShell:
```powershell
# Generate 32-byte (256-bit) random key
$key = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($key)
$base64Key = [Convert]::ToBase64String($key)
Write-Output $base64Key
```

Using OpenSSL:
```bash
# Generate 32-byte random key and base64 encode
openssl rand -base64 32
```

Using .NET:
```csharp
using System.Security.Cryptography;

var key = new byte[32];
using (var rng = RandomNumberGenerator.Create())
{
    rng.GetBytes(key);
}
var base64Key = Convert.ToBase64String(key);
Console.WriteLine(base64Key);
```

**Important**: Store the key securely! Never commit keys to source control.

## Key Rotation Process

### Step 1: Generate New Key

```bash
# Generate new key
NEW_KEY=$(openssl rand -base64 32)
echo "New key: $NEW_KEY"
```

### Step 2: Update Configuration

Update Kubernetes secret with new key:

```bash
# Current key becomes previous key
PREVIOUS_KEYS="2:$(kubectl get secret hookverse-encryption-keys -n hookverse \
  -o jsonpath='{.data.current-key}' | base64 -d)"

# Update secret with new key
kubectl create secret generic hookverse-encryption-keys \
  --from-literal=current-key="$NEW_KEY" \
  --from-literal=current-key-version="3" \
  --from-literal=previous-keys="$PREVIOUS_KEYS" \
  --dry-run=client -o yaml | kubectl apply -f -
```

### Step 3: Rolling Restart

Restart pods to pick up new configuration:

```bash
kubectl rollout restart deployment/hookverse-api -n hookverse
kubectl rollout restart deployment/hookverse-worker -n hookverse

# Wait for rollout to complete
kubectl rollout status deployment/hookverse-api -n hookverse
kubectl rollout status deployment/hookverse-worker -n hookverse
```

### Step 4: Re-encrypt Data

Trigger re-encryption via API:

```bash
# Re-encrypt all data
curl -X POST https://api.hookverse.com/api/v1/encryptionkeyrotation/rotate \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

Or incrementally:

```bash
# Re-encrypt webhook events in batches
curl -X POST "https://api.hookverse.com/api/v1/encryptionkeyrotation/reencrypt/webhook-events?maxRecords=1000" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Re-encrypt subscriptions in batches
curl -X POST "https://api.hookverse.com/api/v1/encryptionkeyrotation/reencrypt/subscriptions?maxRecords=1000" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

### Step 5: Verify Re-encryption

Check re-encryption progress:

```bash
curl https://api.hookverse.com/api/v1/encryptionkeyrotation/statistics \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

Response:
```json
{
  "totalWebhookEvents": 50000,
  "totalSubscriptions": 1000,
  "webhookEventsByKeyVersion": {
    "2": 5000,
    "3": 45000
  },
  "subscriptionsByKeyVersion": {
    "2": 100,
    "3": 900
  },
  "currentKeyVersion": 3,
  "recordsNeedingReEncryption": 5100,
  "reEncryptionProgress": 89.8
}
```

### Step 6: Remove Old Keys

Once re-encryption is complete (100%), remove old keys:

```bash
# Update secret to remove old keys
kubectl create secret generic hookverse-encryption-keys \
  --from-literal=current-key="$NEW_KEY" \
  --from-literal=current-key-version="3" \
  --from-literal=previous-keys="" \
  --dry-run=client -o yaml | kubectl apply -f -
```

## Automated Re-encryption

### Background Service

Create a background service for continuous re-encryption:

```csharp
public class ReEncryptionBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (!stoppingToken.IsCancellationRequested && 
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _serviceProvider.CreateScope();
            var rotationService = scope.ServiceProvider
                .GetRequiredService<EncryptionKeyRotationService>();

            // Re-encrypt in batches
            await rotationService.ReEncryptWebhookEventsAsync(
                maxRecords: 1000, 
                stoppingToken);
            
            await rotationService.ReEncryptSubscriptionsAsync(
                maxRecords: 1000, 
                stoppingToken);
        }
    }
}
```

## Best Practices

### 1. Regular Rotation Schedule

- Rotate encryption keys every **180 days** (6 months)
- Document rotation dates and key versions
- Set calendar reminders for upcoming rotations

### 2. Secure Key Storage

- **Development**: Use user secrets or environment variables
- **Production**: Use Kubernetes secrets or cloud key management (Azure Key Vault, AWS KMS, GCP Secret Manager)
- **Never** commit keys to source control
- **Never** log encryption keys

### 3. Key Backup

```bash
# Backup current encryption key before rotation
kubectl get secret hookverse-encryption-keys -n hookverse -o yaml > \
  encryption-keys-backup-$(date +%Y%m%d).yaml

# Store backup securely (encrypted storage, password manager, etc.)
```

### 4. Gradual Re-encryption

For large datasets, re-encrypt gradually:

```bash
# Re-encrypt in batches over time
for i in {1..10}; do
  curl -X POST "https://api.hookverse.com/api/v1/encryptionkeyrotation/reencrypt/webhook-events?maxRecords=10000" \
    -H "Authorization: Bearer $ADMIN_TOKEN"
  sleep 300  # Wait 5 minutes between batches
done
```

### 5. Monitoring

Track re-encryption progress with Prometheus metrics:

```promql
# Records needing re-encryption
hookverse_encryption_records_needing_reencryption

# Re-encryption progress percentage
hookverse_encryption_reencryption_progress_percent

# Current key version
hookverse_encryption_current_key_version
```

## Security Considerations

### 1. Key Access Control

Limit access to encryption keys:

```yaml
# Kubernetes RBAC for secret access
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: encryption-key-reader
  namespace: hookverse
rules:
  - apiGroups: [""]
    resources: ["secrets"]
    resourceNames: ["hookverse-encryption-keys"]
    verbs: ["get"]
```

### 2. Audit Logging

All encryption operations are logged:

```
[2025-10-20 12:00:00] Encryption key rotation requested
[2025-10-20 12:00:05] Current key version: 2. Records needing re-encryption: 50000
[2025-10-20 12:05:30] Re-encrypted 50000 webhook events
[2025-10-20 12:06:00] Re-encrypted 1000 subscriptions
[2025-10-20 12:06:05] Encryption key rotation completed. Re-encrypted 51000 records
```

### 3. Key Version Tracking

Each encrypted value includes its key version:

```csharp
// Decrypt automatically uses correct key version
var decrypted = _encryptionService.Decrypt(encryptedValue);

// Check if re-encryption is needed
if (_encryptionService.NeedsReEncryption(encryptedValue))
{
    var reEncrypted = _encryptionService.ReEncrypt(encryptedValue);
}
```

### 4. Disaster Recovery

In case of key loss:

1. **If old keys are lost**: Data encrypted with those keys is **unrecoverable**
2. **Always maintain backups** of previous keys until 100% re-encryption
3. **Use cloud key management** services for automatic backup and versioning

## Troubleshooting

### Issue: Decryption fails after key rotation

**Cause**: Previous keys not properly configured

**Solution**: Verify `PreviousKeys` configuration includes all old key versions:

```json
{
  "Encryption": {
    "CurrentKeyVersion": 3,
    "PreviousKeys": "1:OLD_KEY_V1,2:OLD_KEY_V2"
  }
}
```

### Issue: Re-encryption is slow

**Cause**: Large dataset, insufficient batch size

**Solution**: Increase batch size and run multiple re-encryption jobs:

```json
{
  "EncryptionKeyRotation": {
    "ReEncryptionBatchSize": 500,  // Increase from 100
    "ReEncryptionBatchTimeout": "00:01:00"  // Increase timeout
  }
}
```

### Issue: Out of memory during re-encryption

**Cause**: Loading too many records into memory

**Solution**: Reduce batch size or use pagination:

```bash
# Re-encrypt in smaller batches
curl -X POST "https://api.hookverse.com/api/v1/encryptionkeyrotation/reencrypt/webhook-events?maxRecords=100"
```

### Issue: CryptographicException during decryption

**Possible Causes**:
1. Data corruption
2. Wrong key version
3. Tampered data (authentication tag mismatch)

**Solution**: Check logs for specific error and key version in use.

## Related Documentation

- [API Key Rotation](./api-key-rotation.md)
- [Security Best Practices](./best-practices.md)
- [Kubernetes Secrets Management](../deployment/secrets.md)
- [GDPR Data Protection](../gdpr/data-protection.md)
