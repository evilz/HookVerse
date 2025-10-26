# HookVerse Deployment Guide

**Last Updated**: October 26, 2025  
**Version**: 2.0.0 (Aspire Edition)

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Manifest Generation](#manifest-generation)
4. [Kubernetes Deployment](#kubernetes-deployment)
5. [Azure Container Apps Deployment](#azure-container-apps-deployment)
6. [Environment Configuration](#environment-configuration)
7. [Database Migrations](#database-migrations)
8. [Monitoring and Observability](#monitoring-and-observability)
9. [Troubleshooting](#troubleshooting)

---

## Overview

HookVerse uses **.NET Aspire 9.5** to generate deployment manifests automatically. Aspire can generate:

- **Kubernetes manifests** (YAML) for self-hosted or cloud Kubernetes clusters
- **Azure Bicep templates** for Azure Container Apps with managed services

This approach ensures:
- ✅ **Environment parity**: Same service topology across dev and production
- ✅ **Single source of truth**: AppHost defines the entire application
- ✅ **Zero manual YAML**: Manifests are generated, not hand-written
- ✅ **Type-safe configuration**: Compile-time validation of service dependencies

### Deployment Targets

| Environment | Target | Database | Message Queue | Cache |
|-------------|--------|----------|---------------|-------|
| **Development** | Local Docker | PostgreSQL (container) | RabbitMQ (container) | Redis (container) |
| **Staging** | Kubernetes | PostgreSQL (managed) | RabbitMQ (managed) | Redis (managed) |
| **Production** | Azure Container Apps | Azure SQL Database | Azure Service Bus | Azure Cache for Redis |

---

## Prerequisites

### For All Deployments

- **.NET 10 SDK** with Aspire workload: `dotnet workload install aspire`
- **Docker Desktop** (for container image building)
- **Git** repository access

### For Kubernetes Deployment

- **kubectl** CLI installed and configured
- Access to a Kubernetes cluster (AKS, EKS, GKE, or self-hosted)
- **Container registry** (Azure Container Registry, Docker Hub, etc.)
- **Managed database** instances (or Kubernetes-hosted PostgreSQL)

### For Azure Deployment

- **Azure CLI** (`az`) installed and logged in
- **Azure subscription** with appropriate permissions
- **Azure Container Apps** environment created
- **Azure SQL Database**, **Service Bus**, and **Redis Cache** provisioned

### Verify Installation

```powershell
# Check .NET SDK
dotnet --version  # Should be 10.0.x

# Check Aspire workload
dotnet workload list | findstr aspire

# Check Docker
docker --version

# Check kubectl (for Kubernetes)
kubectl version --client

# Check Azure CLI (for Azure)
az --version
```

---

## Manifest Generation

Aspire generates deployment manifests using the `dotnet publish` command with the manifest publisher.

### Generate Kubernetes Manifests

```powershell
# Navigate to AppHost directory
cd src/HookVerse.AppHost

# Generate Kubernetes manifests
dotnet publish `
  --os linux `
  --arch x64 `
  /t:GenerateAspireManifest `
  /p:AspireManifestPublishingMode=Kubernetes `
  /p:AspireManifestOutputPath=../../aspire/manifests/kubernetes

# Output: YAML files in aspire/manifests/kubernetes/
```

**Generated Files**:
- `deployment.yaml` - Kubernetes Deployments for all services
- `service.yaml` - Kubernetes Services (ClusterIP, LoadBalancer)
- `configmap.yaml` - Configuration data
- `secret.yaml` - Sensitive configuration (base64 encoded)
- `ingress.yaml` - HTTP(S) routing rules (if configured)

### Generate Azure Bicep Templates

```powershell
# Navigate to AppHost directory
cd src/HookVerse.AppHost

# Generate Azure Bicep templates
dotnet publish `
  --os linux `
  --arch x64 `
  /t:GenerateAspireManifest `
  /p:AspireManifestPublishingMode=Azure `
  /p:AspireManifestOutputPath=../../aspire/manifests/azure

# Output: Bicep files in aspire/manifests/azure/
```

**Generated Files**:
- `main.bicep` - Azure Container Apps and managed services
- `parameters.json` - Environment-specific parameters
- `resources.bicep` - Individual resource definitions

### Manifest Generation Options

You can customize manifest generation with these parameters:

```powershell
# Include resource limits and requests
/p:AspireIncludeResourceLimits=true

# Set default replica count
/p:AspireDefaultReplicaCount=3

# Specify container registry
/p:AspireContainerRegistry=myregistry.azurecr.io

# Set image tag
/p:AspireImageTag=v1.0.0
```

### Automated Manifest Generation Script

Create `aspire/templates/generate-manifests.ps1`:

```powershell
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Kubernetes", "Azure", "Both")]
    [string]$Target = "Both",
    
    [Parameter(Mandatory=$false)]
    [string]$ImageTag = "latest"
)

$ErrorActionPreference = "Stop"

Write-Host "Generating Aspire deployment manifests..." -ForegroundColor Cyan
Write-Host "Target: $Target" -ForegroundColor Yellow
Write-Host "Image Tag: $ImageTag" -ForegroundColor Yellow

# Navigate to AppHost
Push-Location "$PSScriptRoot/../../src/HookVerse.AppHost"

try {
    if ($Target -eq "Kubernetes" -or $Target -eq "Both") {
        Write-Host "`nGenerating Kubernetes manifests..." -ForegroundColor Green
        dotnet publish `
            --os linux `
            --arch x64 `
            /t:GenerateAspireManifest `
            /p:AspireManifestPublishingMode=Kubernetes `
            /p:AspireManifestOutputPath=../../aspire/manifests/kubernetes `
            /p:AspireImageTag=$ImageTag
        
        Write-Host "✓ Kubernetes manifests generated" -ForegroundColor Green
    }
    
    if ($Target -eq "Azure" -or $Target -eq "Both") {
        Write-Host "`nGenerating Azure Bicep templates..." -ForegroundColor Green
        dotnet publish `
            --os linux `
            --arch x64 `
            /t:GenerateAspireManifest `
            /p:AspireManifestPublishingMode=Azure `
            /p:AspireManifestOutputPath=../../aspire/manifests/azure `
            /p:AspireImageTag=$ImageTag
        
        Write-Host "✓ Azure Bicep templates generated" -ForegroundColor Green
    }
    
    Write-Host "`n✅ Manifest generation complete!" -ForegroundColor Green
    Write-Host "Output directory: $PSScriptRoot/../manifests/" -ForegroundColor Yellow
}
catch {
    Write-Host "❌ Error generating manifests: $_" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
```

**Usage**:
```powershell
# Generate both Kubernetes and Azure manifests
.\aspire\templates\generate-manifests.ps1

# Generate only Kubernetes manifests
.\aspire\templates\generate-manifests.ps1 -Target Kubernetes

# Generate with specific image tag
.\aspire\templates\generate-manifests.ps1 -ImageTag v1.2.3
```

---

## Kubernetes Deployment

### Step 1: Build and Push Container Images

```powershell
# Set your container registry
$REGISTRY = "myregistry.azurecr.io"
$VERSION = "1.0.0"

# Login to container registry
az acr login --name myregistry

# Build and push API
docker build -t $REGISTRY/hookverse-api:$VERSION -f src/HookVerse.Api/Dockerfile .
docker push $REGISTRY/hookverse-api:$VERSION

# Build and push Worker
docker build -t $REGISTRY/hookverse-worker:$VERSION -f src/HookVerse.Worker/Dockerfile .
docker push $REGISTRY/hookverse-worker:$VERSION

# Build and push Dashboard
docker build -t $REGISTRY/hookverse-dashboard:$VERSION -f src/HookVerse.Dashboard/Dockerfile .
docker push $REGISTRY/hookverse-dashboard:$VERSION
```

### Step 2: Create Kubernetes Namespace

```powershell
# Create namespace
kubectl create namespace hookverse

# Set as default namespace (optional)
kubectl config set-context --current --namespace=hookverse
```

### Step 3: Configure Secrets

```powershell
# Create database connection secret
kubectl create secret generic hookverse-db `
  --from-literal=ConnectionStrings__DefaultConnection="Host=postgres.example.com;Port=5432;Database=hookverse;Username=hookverse;Password=YOUR_PASSWORD"

# Create RabbitMQ secret
kubectl create secret generic hookverse-rabbitmq `
  --from-literal=RabbitMQ__Host="rabbitmq.example.com" `
  --from-literal=RabbitMQ__Username="hookverse" `
  --from-literal=RabbitMQ__Password="YOUR_PASSWORD"

# Create Redis secret
kubectl create secret generic hookverse-redis `
  --from-literal=Redis__ConnectionString="redis.example.com:6379,password=YOUR_PASSWORD"
```

### Step 4: Apply Kubernetes Manifests

```powershell
# Apply manifests
kubectl apply -f aspire/manifests/kubernetes/

# Verify deployments
kubectl get deployments
kubectl get services
kubectl get pods

# Check pod logs
kubectl logs -f deployment/hookverse-api
```

### Step 5: Expose Services (Optional)

**Using LoadBalancer**:
```yaml
# ingress.yaml
apiVersion: v1
kind: Service
metadata:
  name: hookverse-api-lb
spec:
  type: LoadBalancer
  selector:
    app: hookverse-api
  ports:
  - port: 80
    targetPort: 8080
```

**Using Ingress**:
```yaml
# ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: hookverse-ingress
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
spec:
  tls:
  - hosts:
    - api.hookverse.example.com
    secretName: hookverse-tls
  rules:
  - host: api.hookverse.example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: hookverse-api
            port:
              number: 80
```

```powershell
# Apply ingress
kubectl apply -f ingress.yaml

# Get external IP
kubectl get ingress hookverse-ingress
```

### Step 6: Database Migrations

```powershell
# Option 1: Run migration job
kubectl apply -f - <<EOF
apiVersion: batch/v1
kind: Job
metadata:
  name: hookverse-migration
spec:
  template:
    spec:
      containers:
      - name: migration
        image: myregistry.azurecr.io/hookverse-api:1.0.0
        command:
        - dotnet
        - ef
        - database
        - update
        - --startup-project
        - /app/HookVerse.Api.dll
        envFrom:
        - secretRef:
            name: hookverse-db
      restartPolicy: OnFailure
EOF

# Option 2: Run in existing pod
kubectl exec -it deployment/hookverse-api -- \
  dotnet ef database update --startup-project /app/HookVerse.Api.dll
```

### Kubernetes Deployment Checklist

- [ ] Container images built and pushed to registry
- [ ] Namespace created
- [ ] Secrets configured (database, RabbitMQ, Redis)
- [ ] Manifests applied successfully
- [ ] All pods running and healthy
- [ ] Services accessible
- [ ] Database migrations applied
- [ ] Ingress/LoadBalancer configured (if needed)
- [ ] TLS certificates configured (if using HTTPS)
- [ ] Health checks responding

---

## Azure Container Apps Deployment

### Step 1: Set Up Azure Resources

```powershell
# Variables
$RESOURCE_GROUP = "rg-hookverse-prod"
$LOCATION = "eastus"
$ENVIRONMENT = "hookverse-env"
$REGISTRY = "hookverseacr"

# Login to Azure
az login

# Create resource group
az group create `
  --name $RESOURCE_GROUP `
  --location $LOCATION

# Create Container Apps environment
az containerapp env create `
  --name $ENVIRONMENT `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION

# Create Azure Container Registry
az acr create `
  --resource-group $RESOURCE_GROUP `
  --name $REGISTRY `
  --sku Basic

# Enable admin user (for pushing images)
az acr update --name $REGISTRY --admin-enabled true
```

### Step 2: Create Managed Services

**Azure SQL Database**:
```powershell
$SQL_SERVER = "hookverse-sql"
$SQL_DB = "hookverse"
$SQL_ADMIN = "hookverseadmin"
$SQL_PASSWORD = "YourSecurePassword123!"

# Create SQL Server
az sql server create `
  --name $SQL_SERVER `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION `
  --admin-user $SQL_ADMIN `
  --admin-password $SQL_PASSWORD

# Create database
az sql db create `
  --resource-group $RESOURCE_GROUP `
  --server $SQL_SERVER `
  --name $SQL_DB `
  --service-objective S0

# Allow Azure services
az sql server firewall-rule create `
  --resource-group $RESOURCE_GROUP `
  --server $SQL_SERVER `
  --name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0
```

**Azure Service Bus** (RabbitMQ alternative):
```powershell
$SERVICEBUS_NS = "hookverse-sb"

# Create Service Bus namespace
az servicebus namespace create `
  --resource-group $RESOURCE_GROUP `
  --name $SERVICEBUS_NS `
  --location $LOCATION `
  --sku Standard

# Create queue
az servicebus queue create `
  --resource-group $RESOURCE_GROUP `
  --namespace-name $SERVICEBUS_NS `
  --name webhook-deliveries
```

**Azure Cache for Redis**:
```powershell
$REDIS_NAME = "hookverse-redis"

# Create Redis cache
az redis create `
  --location $LOCATION `
  --name $REDIS_NAME `
  --resource-group $RESOURCE_GROUP `
  --sku Basic `
  --vm-size c0
```

### Step 3: Deploy Using Bicep

```powershell
# Navigate to Bicep directory
cd aspire/manifests/azure

# Validate Bicep template
az bicep build --file main.bicep

# Create parameters file (parameters.json)
$parameters = @{
    '$schema' = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
    contentVersion = '1.0.0.0'
    parameters = @{
        environmentName = @{ value = $ENVIRONMENT }
        location = @{ value = $LOCATION }
        containerRegistry = @{ value = "$REGISTRY.azurecr.io" }
        sqlServerName = @{ value = $SQL_SERVER }
        sqlDatabaseName = @{ value = $SQL_DB }
        sqlAdminPassword = @{ value = $SQL_PASSWORD }
        serviceBusNamespace = @{ value = $SERVICEBUS_NS }
        redisName = @{ value = $REDIS_NAME }
    }
} | ConvertTo-Json -Depth 10

$parameters | Out-File parameters.json -Encoding utf8

# Deploy
az deployment group create `
  --resource-group $RESOURCE_GROUP `
  --template-file main.bicep `
  --parameters parameters.json

# Get deployment outputs
az deployment group show `
  --resource-group $RESOURCE_GROUP `
  --name main `
  --query properties.outputs
```

### Step 4: Configure Application Settings

```powershell
# Set environment variables for API
az containerapp update `
  --name hookverse-api `
  --resource-group $RESOURCE_GROUP `
  --set-env-vars `
    "ConnectionStrings__DefaultConnection=secretref:sql-connection-string" `
    "ServiceBus__ConnectionString=secretref:servicebus-connection-string" `
    "Redis__ConnectionString=secretref:redis-connection-string"

# Set secrets
az containerapp secret set `
  --name hookverse-api `
  --resource-group $RESOURCE_GROUP `
  --secrets `
    sql-connection-string="Server=$SQL_SERVER.database.windows.net;Database=$SQL_DB;User Id=$SQL_ADMIN;Password=$SQL_PASSWORD;" `
    servicebus-connection-string="$(az servicebus namespace authorization-rule keys list --resource-group $RESOURCE_GROUP --namespace-name $SERVICEBUS_NS --name RootManageSharedAccessKey --query primaryConnectionString -o tsv)" `
    redis-connection-string="$(az redis list-keys --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query primaryKey -o tsv)"
```

### Step 5: Database Migrations

```powershell
# Run migration as a job in Container Apps
az containerapp job create `
  --name hookverse-migration `
  --resource-group $RESOURCE_GROUP `
  --environment $ENVIRONMENT `
  --trigger-type Manual `
  --replica-timeout 1800 `
  --replica-retry-limit 1 `
  --image $REGISTRY.azurecr.io/hookverse-api:latest `
  --cpu 0.5 `
  --memory 1Gi `
  --command "/bin/sh" "-c" "dotnet ef database update --startup-project /app/HookVerse.Api.dll" `
  --env-vars "ConnectionStrings__DefaultConnection=secretref:sql-connection-string" `
  --secrets sql-connection-string="..."

# Execute migration job
az containerapp job start `
  --name hookverse-migration `
  --resource-group $RESOURCE_GROUP
```

### Azure Deployment Checklist

- [ ] Azure resources created (Container Apps environment, ACR, SQL, Service Bus, Redis)
- [ ] Container images pushed to Azure Container Registry
- [ ] Bicep templates deployed successfully
- [ ] Application settings configured
- [ ] Secrets set securely
- [ ] Database migrations applied
- [ ] Container apps running and healthy
- [ ] Custom domain configured (optional)
- [ ] TLS certificates configured
- [ ] Monitoring enabled (Application Insights)

---

## Environment Configuration

### Development

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=hookverse;Username=hookverse;Password=hookverse123"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

### Staging/Production

Use **environment variables** or **Azure Key Vault**:

```powershell
# Set environment variables
export ConnectionStrings__DefaultConnection="..."
export RabbitMQ__ConnectionString="..."
export Redis__ConnectionString="..."

# Or use Azure Key Vault references
@Microsoft.KeyVault(SecretUri=https://myvault.vault.azure.net/secrets/DbConnectionString/)
```

### Configuration Priority

1. **Environment variables** (highest priority)
2. **User Secrets** (development only)
3. **appsettings.{Environment}.json**
4. **appsettings.json** (lowest priority)

---

## Database Migrations

### Apply Migrations on Deployment

**Kubernetes Job**:
```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: hookverse-migration
spec:
  template:
    spec:
      containers:
      - name: migration
        image: myregistry.azurecr.io/hookverse-api:1.0.0
        command:
        - dotnet
        - ef
        - database
        - update
      restartPolicy: OnFailure
```

**Azure Container Apps Job**:
```powershell
az containerapp job create `
  --name hookverse-migration `
  --environment $ENVIRONMENT `
  --trigger-type Manual `
  --image hookverseacr.azurecr.io/hookverse-api:latest `
  --command "dotnet ef database update"
```

### Migration Best Practices

1. **Test migrations locally** before deploying
2. **Backup database** before running migrations in production
3. **Run migrations before deploying new app version**
4. **Monitor migration execution** (use logs)
5. **Have rollback plan** ready

---

## Monitoring and Observability

### OpenTelemetry Integration

HookVerse uses OpenTelemetry for:
- **Distributed tracing** (spans across services)
- **Metrics** (request rates, latencies)
- **Logging** (structured logs with correlation IDs)

### Kubernetes Monitoring

**Prometheus + Grafana**:
```powershell
# Install Prometheus
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm install prometheus prometheus-community/kube-prometheus-stack

# Access Grafana
kubectl port-forward svc/prometheus-grafana 3000:80
# Login: admin/prom-operator
```

### Azure Monitoring

**Application Insights**:
```powershell
# Create Application Insights
az monitor app-insights component create `
  --app hookverse-insights `
  --location $LOCATION `
  --resource-group $RESOURCE_GROUP

# Get instrumentation key
$INSTRUMENTATION_KEY = az monitor app-insights component show `
  --app hookverse-insights `
  --resource-group $RESOURCE_GROUP `
  --query instrumentationKey -o tsv

# Set in Container App
az containerapp update `
  --name hookverse-api `
  --resource-group $RESOURCE_GROUP `
  --set-env-vars "APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=$INSTRUMENTATION_KEY"
```

**View telemetry**:
- Azure Portal → Application Insights → Live Metrics
- Application Map (service dependencies)
- Transaction search (distributed traces)
- Failures (exceptions and errors)

---

## Troubleshooting

### Common Issues

#### 1. Container Image Pull Errors

**Symptom**: Pods stuck in `ImagePullBackOff`

**Solution**:
```powershell
# Verify image exists
docker pull myregistry.azurecr.io/hookverse-api:latest

# Check image pull secret
kubectl get secrets
kubectl describe pod <pod-name>

# Create image pull secret if missing
kubectl create secret docker-registry acr-secret `
  --docker-server=myregistry.azurecr.io `
  --docker-username=myregistry `
  --docker-password=<password>
```

#### 2. Database Connection Failures

**Symptom**: Application logs show connection timeouts

**Solution**:
```powershell
# Test database connectivity
kubectl run -it --rm debug --image=postgres:15 --restart=Never -- \
  psql -h postgres.example.com -U hookverse -d hookverse

# Check secrets
kubectl get secret hookverse-db -o yaml

# Verify firewall rules (Azure SQL)
az sql server firewall-rule list `
  --resource-group $RESOURCE_GROUP `
  --server $SQL_SERVER
```

#### 3. Service Discovery Issues

**Symptom**: Services can't communicate

**Solution**:
```powershell
# Check service endpoints
kubectl get endpoints

# Test service connectivity
kubectl run -it --rm debug --image=curlimages/curl --restart=Never -- \
  curl http://hookverse-api:80/health

# Check network policies
kubectl get networkpolicies
```

#### 4. Health Check Failures

**Symptom**: Container Apps show unhealthy status

**Solution**:
```powershell
# View health check configuration
az containerapp show `
  --name hookverse-api `
  --resource-group $RESOURCE_GROUP `
  --query properties.template.containers[0].probes

# Check application logs
az containerapp logs show `
  --name hookverse-api `
  --resource-group $RESOURCE_GROUP `
  --tail 100

# Test health endpoint manually
curl https://hookverse-api.example.com/health
```

---

## Additional Resources

- **Aspire Documentation**: https://learn.microsoft.com/dotnet/aspire
- **Kubernetes Documentation**: https://kubernetes.io/docs/
- **Azure Container Apps**: https://learn.microsoft.com/azure/container-apps/
- **HookVerse Guides**:
  - [Development Guide](DEVELOPMENT.md)
  - [Running Guide](RUNNING.md)
  - [API Guide](API-GUIDE.md)

---

## Quick Reference

### Manifest Generation

```powershell
# Kubernetes
dotnet publish /t:GenerateAspireManifest /p:AspireManifestPublishingMode=Kubernetes

# Azure
dotnet publish /t:GenerateAspireManifest /p:AspireManifestPublishingMode=Azure
```

### Kubernetes Commands

```powershell
# Deploy
kubectl apply -f aspire/manifests/kubernetes/

# Check status
kubectl get pods
kubectl logs -f deployment/hookverse-api

# Update image
kubectl set image deployment/hookverse-api hookverse-api=myregistry.azurecr.io/hookverse-api:v2
```

### Azure Commands

```powershell
# Deploy Bicep
az deployment group create --template-file main.bicep

# Update container app
az containerapp update --name hookverse-api --image hookverseacr.azurecr.io/hookverse-api:v2

# View logs
az containerapp logs show --name hookverse-api --tail 100
```

---

**Happy Deploying!** 🚀
