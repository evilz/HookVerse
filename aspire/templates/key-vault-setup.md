# Azure Key Vault Configuration for HookVerse

This guide explains how to configure Azure Key Vault for secure secrets management in production deployments.

## Overview

Azure Key Vault is used to store sensitive configuration values (connection strings, API keys, passwords) in production environments. Aspire-generated Bicep templates can reference Key Vault secrets instead of storing values directly in configuration.

## Prerequisites

1. Azure subscription
2. Azure CLI installed and authenticated
3. Aspire AppHost configured for Azure deployment

## Setup Steps

### 1. Create Azure Key Vault

```bash
# Set variables
RESOURCE_GROUP="rg-hookverse-prod"
LOCATION="eastus"
KEY_VAULT_NAME="kv-hookverse-prod"  # Must be globally unique

# Create resource group (if not exists)
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create Key Vault
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-rbac-authorization true
```

### 2. Store Secrets in Key Vault

```bash
# Database connection string
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "SqlConnectionString" \
  --value "Server=tcp:hookverse-sql.database.windows.net,1433;Database=hookverse;..."

# Service Bus connection string
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "ServiceBusConnectionString" \
  --value "Endpoint=sb://hookverse-sb.servicebus.windows.net/;SharedAccessKeyName=..."

# Redis connection string
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "RedisConnectionString" \
  --value "hookverse-redis.redis.cache.windows.net:6380,password=...,ssl=True"

# SMTP password
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "SmtpPassword" \
  --value "your-smtp-password"

# External API keys
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "DatadogApiKey" \
  --value "your-datadog-api-key"
```

### 3. Configure AppHost for Key Vault

Set the Key Vault name in configuration:

**Option A: Environment Variable**
```bash
export Azure__KeyVault__Name="kv-hookverse-prod"
```

**Option B: appsettings.Production.json**
```json
{
  "Azure": {
    "KeyVault": {
      "Name": "kv-hookverse-prod"
    }
  }
}
```

**Option C: User Secrets (for testing locally)**
```bash
dotnet user-secrets set "Azure:KeyVault:Name" "kv-hookverse-prod" --project src/HookVerse.AppHost
```

### 4. Grant Access to Container Apps

After deploying to Azure Container Apps, grant the Container App managed identity access to Key Vault:

```bash
# Get Container App principal ID
PRINCIPAL_ID=$(az containerapp show \
  --name hookverse-api \
  --resource-group $RESOURCE_GROUP \
  --query identity.principalId -o tsv)

# Assign Key Vault Secrets User role
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Key Vault Secrets User" \
  --scope /subscriptions/<subscription-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME
```

Repeat for worker and dashboard apps.

### 5. Update Bicep Templates to Reference Key Vault

After generating Bicep templates, you can reference Key Vault secrets:

**Generated Bicep (aspire/manifests/azure/main.bicep):**
```bicep
param keyVaultName string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource apiContainerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'hookverse-api'
  properties: {
    configuration: {
      secrets: [
        {
          name: 'sql-connection-string'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/SqlConnectionString'
          identity: 'system'
        }
        {
          name: 'servicebus-connection-string'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/ServiceBusConnectionString'
          identity: 'system'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          env: [
            {
              name: 'ConnectionStrings__postgres'
              secretRef: 'sql-connection-string'
            }
            {
              name: 'ConnectionStrings__rabbitmq'
              secretRef: 'servicebus-connection-string'
            }
          ]
        }
      ]
    }
  }
}
```

### 6. Deploy with Key Vault References

```bash
# Generate Bicep templates with Key Vault name
dotnet run --project src/HookVerse.AppHost \
  -- --publisher manifest \
  --output-path aspire/manifests/azure \
  --format bicep

# Deploy to Azure
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file aspire/manifests/azure/main.bicep \
  --parameters keyVaultName=$KEY_VAULT_NAME
```

## Configuration in Application Code

### Accessing Key Vault Secrets

In production, secrets are automatically injected via environment variables:

```csharp
// In Program.cs or service configuration
var connectionString = builder.Configuration.GetConnectionString("postgres");
// Value comes from Key Vault via Container Apps secret reference
```

### Optional: Direct Key Vault Access

For advanced scenarios, you can access Key Vault directly:

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

var keyVaultUri = new Uri(builder.Configuration["Azure:KeyVault:Uri"]!);
var client = new SecretClient(keyVaultUri, new DefaultAzureCredential());

var secret = await client.GetSecretAsync("DatadogApiKey");
var apiKey = secret.Value.Value;
```

**Required NuGet Package:**
```bash
dotnet add package Azure.Security.KeyVault.Secrets
dotnet add package Azure.Identity
```

## Environment-Specific Configuration

### Local Development
- Use .NET User Secrets: `dotnet user-secrets set "Key" "Value"`
- Secrets stored locally: `%APPDATA%/Microsoft/UserSecrets/<UserSecretsId>/secrets.json`

### Staging
- Use Azure Key Vault with separate vault: `kv-hookverse-staging`
- Reference via Container Apps configuration

### Production
- Use Azure Key Vault: `kv-hookverse-prod`
- Enable soft delete and purge protection
- Use Azure RBAC for access control
- Enable diagnostic logging

## Security Best Practices

1. **Never Commit Secrets**: Keep secrets out of version control
2. **Rotate Secrets Regularly**: Update Key Vault secrets periodically
3. **Use Managed Identities**: Avoid storing credentials for Key Vault access
4. **Separate Vaults by Environment**: staging, production
5. **Enable Audit Logging**: Track secret access
6. **Use RBAC**: Principle of least privilege
7. **Enable Soft Delete**: Prevent accidental deletion
8. **Network Isolation**: Use private endpoints in production

## Troubleshooting

### Container App Can't Access Key Vault

**Issue**: `Azure.RequestFailedException: Access denied`

**Solution**:
```bash
# Verify managed identity is enabled
az containerapp show --name hookverse-api --resource-group $RESOURCE_GROUP --query identity

# Grant access
az role assignment create \
  --assignee <principal-id> \
  --role "Key Vault Secrets User" \
  --scope <key-vault-resource-id>
```

### Secret Not Found

**Issue**: `SecretNotFound: Secret not found: SqlConnectionString`

**Solution**:
```bash
# List all secrets
az keyvault secret list --vault-name $KEY_VAULT_NAME --query "[].name"

# Check secret exists
az keyvault secret show --vault-name $KEY_VAULT_NAME --name SqlConnectionString
```

### Invalid Secret Reference Format

**Issue**: `Invalid secret reference format`

**Solution**: Ensure Key Vault URL format is correct:
```
https://<vault-name>.vault.azure.net/secrets/<secret-name>
```

## References

- [Azure Key Vault Documentation](https://learn.microsoft.com/azure/key-vault/)
- [Container Apps Secrets](https://learn.microsoft.com/azure/container-apps/manage-secrets)
- [Aspire Azure Deployment](https://learn.microsoft.com/dotnet/aspire/deployment/azure/aca-deployment)
- [Managed Identity Overview](https://learn.microsoft.com/entra/identity/managed-identities-azure-resources/overview)

## Example: Complete Setup Script

```bash
#!/bin/bash
# complete-keyvault-setup.sh

RESOURCE_GROUP="rg-hookverse-prod"
LOCATION="eastus"
KEY_VAULT_NAME="kv-hookverse-prod"

# Create Key Vault
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-rbac-authorization true

# Store all secrets
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "SqlConnectionString" --value "$SQL_CONN_STRING"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "ServiceBusConnectionString" --value "$SB_CONN_STRING"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "RedisConnectionString" --value "$REDIS_CONN_STRING"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "SmtpPassword" --value "$SMTP_PASSWORD"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "DatadogApiKey" --value "$DATADOG_API_KEY"

# Generate and deploy Bicep with Key Vault reference
dotnet run --project src/HookVerse.AppHost -- --publisher manifest --output-path aspire/manifests/azure --format bicep
az deployment group create --resource-group $RESOURCE_GROUP --template-file aspire/manifests/azure/main.bicep --parameters keyVaultName=$KEY_VAULT_NAME

echo "✓ Key Vault setup complete"
echo "✓ Secrets stored"
echo "✓ Bicep templates deployed"
echo ""
echo "Next: Grant Container Apps access to Key Vault"
```

---

**Last Updated**: October 26, 2025  
**Maintained By**: HookVerse Team
