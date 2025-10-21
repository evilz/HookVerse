# Azure Bicep Templates

This directory contains Azure Bicep templates generated from the HookVerse Aspire AppHost configuration.

## Generation

To generate Azure Bicep templates, run:

```powershell
# From repository root
.\aspire\templates\generate-azure-manifests.ps1
```

Or use dotnet publish directly:

```bash
dotnet publish src/HookVerse.AppHost/HookVerse.AppHost.csproj /p:PublishProfile=azure
```

## Validation

Validate generated Bicep templates using Azure CLI:

```bash
az bicep build --file main.bicep
```

## Deployment

Deploy to Azure using Azure CLI:

```bash
# Create resource group
az group create --name hookverse-rg --location eastus

# Deploy Bicep template
az deployment group create \
  --resource-group hookverse-rg \
  --template-file main.bicep \
  --parameters parameters.json
```

## Parameters

### parameters.json Template

Create a `parameters.json` file to customize deployment:

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "environment": {
      "value": "production"
    },
    "location": {
      "value": "eastus"
    },
    "containerAppEnvironmentName": {
      "value": "hookverse-env"
    },
    "sqlServerName": {
      "value": "hookverse-sql"
    },
    "sqlDatabaseName": {
      "value": "hookverse-db"
    },
    "sqlAdministratorLogin": {
      "value": "hookvadmin"
    },
    "sqlAdministratorLoginPassword": {
      "value": "REPLACE_WITH_SECURE_PASSWORD"
    },
    "serviceBusNamespaceName": {
      "value": "hookverse-sb"
    },
    "redisCacheName": {
      "value": "hookverse-redis"
    },
    "keyVaultName": {
      "value": "hookverse-kv"
    }
  }
}
```

### Parameter Descriptions

- **environment**: Deployment environment (development, staging, production)
- **location**: Azure region for resources
- **containerAppEnvironmentName**: Name for Container Apps Environment
- **sqlServerName**: Azure SQL Server name (must be globally unique)
- **sqlDatabaseName**: Database name
- **sqlAdministratorLogin**: SQL Server admin username
- **sqlAdministratorLoginPassword**: SQL Server admin password (use Key Vault in production)
- **serviceBusNamespaceName**: Azure Service Bus namespace name
- **redisCacheName**: Azure Redis Cache name
- **keyVaultName**: Azure Key Vault name (must be globally unique)

## Environment-Specific Configurations

### Development

```json
{
  "environment": "development",
  "location": "eastus",
  "skuTier": "Basic"
}
```

### Staging

```json
{
  "environment": "staging",
  "location": "eastus",
  "skuTier": "Standard"
}
```

### Production

```json
{
  "environment": "production",
  "location": "eastus",
  "skuTier": "Premium"
}
```

## Security Best Practices

1. **Never commit** `parameters.json` with sensitive values to version control
2. Use **Azure Key Vault** for secrets and connection strings
3. Enable **managed identities** for services to access Azure resources
4. Use **Azure RBAC** for fine-grained access control
5. Enable **diagnostic logs** for monitoring and auditing

## Resources Created

The generated Bicep template creates:

- **Container Apps Environment**: Hosts all containerized services
- **Container Apps**: Api, Worker, Dashboard services
- **Azure SQL Database**: PostgreSQL-compatible database
- **Azure Service Bus**: Message queue for webhook delivery
- **Azure Redis Cache**: Distributed caching
- **Azure Key Vault**: Secrets management
- **Application Insights**: Application monitoring
- **Log Analytics Workspace**: Centralized logging

## CI/CD Integration

### Azure DevOps Pipeline

```yaml
- task: AzureCLI@2
  displayName: 'Deploy to Azure'
  inputs:
    azureSubscription: 'Azure-Subscription'
    scriptType: 'bash'
    scriptLocation: 'inlineScript'
    inlineScript: |
      az deployment group create \
        --resource-group $(resourceGroup) \
        --template-file aspire/manifests/azure/main.bicep \
        --parameters aspire/manifests/azure/parameters.$(environment).json
```

### GitHub Actions

```yaml
- name: Deploy to Azure
  uses: azure/arm-deploy@v1
  with:
    subscriptionId: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
    resourceGroupName: hookverse-rg
    template: aspire/manifests/azure/main.bicep
    parameters: aspire/manifests/azure/parameters.${{ env.ENVIRONMENT }}.json
```

## Troubleshooting

### Validation Errors

If Bicep validation fails:

```bash
# Check syntax
az bicep build --file main.bicep

# Show detailed errors
az deployment group validate \
  --resource-group hookverse-rg \
  --template-file main.bicep \
  --parameters parameters.json
```

### Deployment Failures

Check deployment logs:

```bash
# List deployments
az deployment group list --resource-group hookverse-rg

# Get deployment details
az deployment group show \
  --resource-group hookverse-rg \
  --name <deployment-name>
```

## Learn More

- [Azure Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure Container Apps Documentation](https://learn.microsoft.com/azure/container-apps/)
- [.NET Aspire Azure Deployment](https://learn.microsoft.com/dotnet/aspire/deployment/azure/)
