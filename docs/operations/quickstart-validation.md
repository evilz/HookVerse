# Quickstart Validation

Automated validation of the HookVerse quickstart.md setup guide.

## Overview

The quickstart validation script ensures that all setup steps in `quickstart.md` work correctly on a fresh environment. This helps maintain quality and catch issues before users encounter them.

## Running Validation

### Full Validation

Run complete validation including prerequisites:

```powershell
.\.specify\scripts\powershell\validate-quickstart.ps1
```

### Skip Prerequisites

If software is already installed:

```powershell
.\.specify\scripts\powershell\validate-quickstart.ps1 -SkipPrerequisites
```

### Skip Docker Checks

If containers are already running:

```powershell
.\.specify\scripts\powershell\validate-quickstart.ps1 -SkipDocker
```

### Specify Environment

```powershell
.\.specify\scripts\powershell\validate-quickstart.ps1 -Environment "Staging"
```

## Validation Steps

### 1. Prerequisites

The script checks for required software:

- **.NET 10 SDK**: Verifies version starts with "10."
- **Docker**: Verifies Docker is installed and running
- **Git**: Verifies Git is installed

**Expected Output**:
```
========================================
Step 1: Validating Prerequisites
========================================
[INFO] Checking .NET 10 SDK...
[SUCCESS] .NET 10 SDK found: 10.0.0
[INFO] Checking Docker...
[SUCCESS] Docker found: Docker version 24.0.6
[INFO] Checking Git...
[SUCCESS] Git found: git version 2.42.0
```

### 2. Docker Dependencies

Checks that required containers are running:

- **PostgreSQL**: Database server (port 5432)
- **RabbitMQ**: Message broker (port 5672)
- **Redis**: Cache (port 6379, optional)

**Expected Output**:
```
========================================
Step 2: Validating Docker Dependencies
========================================
[INFO] Checking PostgreSQL container...
[SUCCESS] PostgreSQL container running: postgres
[INFO] Checking RabbitMQ container...
[SUCCESS] RabbitMQ container running: rabbitmq
[INFO] Checking Redis container...
[WARNING] Redis container not running (optional)
```

### 3. Database

Validates database setup:

- **EF Core Tools**: Checks if `dotnet ef` is installed
- **Migrations**: Could check if migrations are up to date (future enhancement)

**Expected Output**:
```
========================================
Step 3: Validating Database
========================================
[INFO] Checking database migrations...
[SUCCESS] EF Core tools found: 10.0.0
```

### 4. API Endpoints

Tests that the API is running and responding:

- **Health Endpoint**: GET /health
- **Metrics Endpoint**: GET /metrics

**Expected Output**:
```
========================================
Step 4: Validating API
========================================
[INFO] Checking if API is running...
[SUCCESS] API health endpoint responded: Healthy
[SUCCESS] Metrics endpoint responded
```

### 5. End-to-End Test

Tests webhook delivery flow (manual test required):

1. Create event type
2. Create subscription
3. Send webhook
4. Verify delivery

**Note**: Currently requires manual testing. Automated test to be added.

## Validation Report

The script generates a summary report:

```
========================================
Validation Report
========================================

Prerequisites Results:
  [Pass] .NET 10 SDK: 10.0.0
  [Pass] Docker: Docker version 24.0.6
  [Pass] Git: git version 2.42.0

Docker Results:
  [Pass] PostgreSQL: postgres
  [Pass] RabbitMQ: rabbitmq
  [Warning] Redis: Not running (optional)

Database Results:
  [Pass] EF Core Tools: 10.0.0

API Results:
  [Pass] Health Endpoint: Healthy
  [Pass] Metrics Endpoint: OK

EndToEnd Results:
  [Warning] Webhook Delivery: Manual test required

Summary:
  Total Tests: 10
  Passed: 8
  Failed: 0
  Warnings: 2
  Success Rate: 80.0%

[SUCCESS] All validations passed!
```

## Exit Codes

- **0**: All tests passed or warnings only
- **1**: One or more tests failed

## CI/CD Integration

### GitHub Actions

Add to `.github/workflows/quickstart-validation.yml`:

```yaml
name: Quickstart Validation

on:
  push:
    branches: [main, develop]
    paths:
      - 'specs/001-webhook-delivery-platform/quickstart.md'
      - 'src/**'
  pull_request:
    branches: [main, develop]
  schedule:
    - cron: '0 6 * * 1'  # Weekly on Mondays at 6 AM UTC

jobs:
  validate-quickstart:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Start Docker dependencies
        run: |
          cd docker
          docker-compose up -d
          sleep 10  # Wait for services to start
      
      - name: Run validation script
        run: |
          pwsh ./.specify/scripts/powershell/validate-quickstart.ps1
      
      - name: Stop Docker dependencies
        if: always()
        run: |
          cd docker
          docker-compose down
```

## Extending Validation

### Adding New Checks

1. Add a new validation category to `$ValidationResults`:

```powershell
$ValidationResults = @{
    Prerequisites = @()
    Docker = @()
    Database = @()
    API = @()
    Worker = @()      # New category
    Monitoring = @()
    EndToEnd = @()
}
```

2. Add validation logic:

```powershell
Write-Step "Step X: Validating Worker"

Write-Info "Checking worker process..."
try {
    $workerProcess = Get-Process | Where-Object { $_.Name -like "*HookVerse.Worker*" }
    if ($workerProcess) {
        Write-Success "Worker process running: $($workerProcess.Id)"
        $ValidationResults.Worker += @{ 
            Name = "Worker Process"; 
            Status = "Pass"; 
            Details = "PID: $($workerProcess.Id)" 
        }
    } else {
        Write-Failure "Worker process not running"
        $ValidationResults.Worker += @{ 
            Name = "Worker Process"; 
            Status = "Fail"; 
            Details = "Not running" 
        }
    }
} catch {
    Write-Failure "Failed to check worker process"
    $ValidationResults.Worker += @{ 
        Name = "Worker Process"; 
        Status = "Fail"; 
        Details = $_.Exception.Message 
    }
}
```

### Adding End-to-End Tests

Enhance the E2E test section:

```powershell
Write-Step "Step 5: End-to-End Webhook Delivery Test"

# 1. Create event type
Write-Info "Creating test event type..."
$eventTypeBody = @{
    name = "test.event"
    description = "Test event type"
} | ConvertTo-Json

$eventTypeResponse = Invoke-RestMethod `
    -Uri "$apiUrl/api/v1/event-types" `
    -Method Post `
    -Body $eventTypeBody `
    -ContentType "application/json"

Write-Success "Event type created: $($eventTypeResponse.id)"

# 2. Create subscription
Write-Info "Creating test subscription..."
$subscriptionBody = @{
    eventTypeId = $eventTypeResponse.id
    url = "https://webhook.site/your-unique-id"
    active = $true
} | ConvertTo-Json

$subscriptionResponse = Invoke-RestMethod `
    -Uri "$apiUrl/api/v1/subscriptions" `
    -Method Post `
    -Body $subscriptionBody `
    -ContentType "application/json"

Write-Success "Subscription created: $($subscriptionResponse.id)"

# 3. Send webhook
Write-Info "Sending test webhook..."
$webhookBody = @{
    eventType = "test.event"
    payload = @{
        message = "Test webhook"
        timestamp = Get-Date -Format "o"
    }
} | ConvertTo-Json

$webhookResponse = Invoke-RestMethod `
    -Uri "$apiUrl/api/v1/webhooks" `
    -Method Post `
    -Body $webhookBody `
    -ContentType "application/json"

Write-Success "Webhook sent: $($webhookResponse.id)"

# 4. Verify delivery
Start-Sleep -Seconds 5  # Wait for processing

$eventResponse = Invoke-RestMethod `
    -Uri "$apiUrl/api/v1/webhook-events/$($webhookResponse.id)" `
    -Method Get

if ($eventResponse.status -eq "Delivered") {
    Write-Success "Webhook delivered successfully"
    $ValidationResults.EndToEnd += @{ 
        Name = "Webhook Delivery"; 
        Status = "Pass"; 
        Details = "Successfully delivered" 
    }
} else {
    Write-Failure "Webhook delivery failed: $($eventResponse.status)"
    $ValidationResults.EndToEnd += @{ 
        Name = "Webhook Delivery"; 
        Status = "Fail"; 
        Details = "Status: $($eventResponse.status)" 
    }
}

# Cleanup
Write-Info "Cleaning up test data..."
Invoke-RestMethod `
    -Uri "$apiUrl/api/v1/subscriptions/$($subscriptionResponse.id)" `
    -Method Delete

Invoke-RestMethod `
    -Uri "$apiUrl/api/v1/event-types/$($eventTypeResponse.id)" `
    -Method Delete
```

## Troubleshooting

### Issue: Prerequisites check fails

**Symptom**: .NET 10 SDK not found

**Solution**:
```bash
# Download and install .NET 10 SDK
# https://dotnet.microsoft.com/download/dotnet/10.0
```

### Issue: Docker containers not running

**Symptom**: PostgreSQL/RabbitMQ container checks fail

**Solution**:
```bash
cd docker
docker-compose up -d

# Check container status
docker ps

# Check container logs
docker logs postgres
docker logs rabbitmq
```

### Issue: API health endpoint fails

**Symptom**: Connection refused or timeout

**Solution**:
```bash
# Check if API is running
dotnet run --project src/HookVerse.Api

# Check API logs
# Look for startup errors or port conflicts
```

### Issue: Database migrations not applied

**Symptom**: Database connection errors

**Solution**:
```bash
# Apply migrations
dotnet ef database update \
  --project src/HookVerse.Infrastructure \
  --startup-project src/HookVerse.Api

# Verify migrations
dotnet ef migrations list \
  --project src/HookVerse.Infrastructure \
  --startup-project src/HookVerse.Api
```

## Best Practices

### 1. Run Before Releases

Always run quickstart validation before:
- Creating a release
- Updating documentation
- Major infrastructure changes

### 2. Test on Fresh Environment

Periodically test on completely fresh environments:
- Clean VM
- Docker container
- GitHub Codespace

### 3. Document Any Manual Steps

If validation finds manual steps are required, document them clearly in the report.

### 4. Keep Script Updated

Update the validation script when:
- Adding new features
- Changing setup procedures
- Updating prerequisites

## Related Documentation

- [Quickstart Guide](../../specs/001-webhook-delivery-platform/quickstart.md)
- [Deployment Guide](./kubernetes-deployment.md)
- [Operations Runbook](../operations/runbook.md)
