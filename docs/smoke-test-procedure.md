# End-to-End Smoke Test Procedure

**Feature**: 002-aspire-orchestration  
**Task**: T183  
**Created**: October 26, 2025

## Purpose

This document provides a comprehensive procedure for executing the end-to-end smoke test for the Aspire orchestration implementation. This test validates the complete developer workflow from local development through hot-reload to deployment.

## Prerequisites

### Required Software
- ✅ .NET 10 SDK installed
- ✅ .NET Aspire workload installed (`dotnet workload list | findstr aspire`)
- ✅ Docker Desktop running and healthy
- ✅ kubectl configured (for Kubernetes deployment test)
- ✅ Azure CLI logged in (for Azure deployment test)
- ✅ Git repository cloned and on branch `002-aspire-orchestration`

### Verify Docker Health
```powershell
# Check Docker is running
docker ps

# Expected: Should list containers (may be empty) without errors
# If error "runtime unhealthy" appears, start Docker Desktop and wait for "Engine running"
```

### Verify Prerequisites
```powershell
# Check .NET and Aspire
dotnet --version  # Should show 10.x
dotnet workload list | findstr aspire  # Should show aspire workload

# Check kubectl (if testing Kubernetes deployment)
kubectl version --client

# Check Azure CLI (if testing Azure deployment)
az --version
az account show  # Should show logged-in account
```

## Test Phases

### Phase 1: Local Application Startup

**Objective**: Verify AppHost starts all services successfully in under 2 minutes.

**Procedure**:
```powershell
# Navigate to repository root
cd E:\PROJECTS\GITHUB\HookVerse

# Start AppHost with timing
$startTime = Get-Date
dotnet run --project src\HookVerse.AppHost
# Wait for "Now listening on: http://localhost:17191" message
$duration = (Get-Date) - $startTime
Write-Host "Startup time: $($duration.TotalSeconds) seconds"
```

**Acceptance Criteria**:
- ✅ AppHost starts without errors
- ✅ Startup time < 120 seconds (target: < 60 seconds based on baseline)
- ✅ Aspire Dashboard opens at http://localhost:17191
- ✅ All services show "Running" status in Resources tab:
  - postgres (container)
  - rabbitmq (container)
  - redis (container)
  - hookverse-api (project)
  - hookverse-worker (project)
  - hookverse-dashboard (project)

**Verification Steps**:
1. Open Aspire Dashboard: http://localhost:17191
2. Navigate to **Resources** tab
3. Verify all 6 resources show green "Running" status
4. Click on each service and check:
   - Console logs show no errors
   - Health checks are passing (if shown)
   - Endpoints are accessible

**Screenshot**: Capture Resources tab showing all services running

---

### Phase 2: Service Health Verification

**Objective**: Verify all services respond to health checks and API endpoints.

**Procedure**:
```powershell
# Test API health
Invoke-RestMethod -Uri http://localhost:7001/health -Method Get
# Expected: {"status":"Healthy"}

# Test API endpoint
Invoke-RestMethod -Uri http://localhost:7001/api/v1/subscribers -Method Get
# Expected: JSON array (may be empty)

# Test Dashboard access
Start-Process http://localhost:7000
# Expected: Dashboard loads in browser
```

**Acceptance Criteria**:
- ✅ API health endpoint returns 200 OK with "Healthy" status
- ✅ API endpoints are accessible and return valid responses
- ✅ Dashboard loads successfully in browser
- ✅ No error logs in Aspire Dashboard Console Logs tab

**Verification Steps**:
1. In Aspire Dashboard, go to **Console Logs** tab
2. Filter to "hookverse-api"
3. Verify no ERROR level logs appear
4. Repeat for "hookverse-worker" and "hookverse-dashboard"

---

### Phase 3: Hot-Reload Verification

**Objective**: Verify code changes are detected and reloaded in under 5 seconds.

**Procedure**:
```powershell
# Make a trivial code change to API controller
$testFile = "src\HookVerse.API\Controllers\HealthController.cs"
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
Add-Content -Path $testFile -Value "`n// Hot-reload test at $timestamp"

# Measure hot-reload time
$reloadStart = Get-Date
# Watch Aspire Dashboard Console Logs for "Building..." → "Application started" message
# Note: With dotnet watch (if running), reload is automatic
# With dotnet run, need to stop and restart (not true hot-reload)

# Alternative: Use dotnet watch explicitly
# Stop current AppHost (Ctrl+C)
# Start with watch mode:
dotnet watch run --project src\HookVerse.AppHost

# Make change again and time the reload
$reloadDuration = (Get-Date) - $reloadStart
Write-Host "Hot-reload time: $($reloadDuration.TotalSeconds) seconds"

# Revert the test change
git checkout -- $testFile
```

**Acceptance Criteria**:
- ✅ Code change detected automatically (with `dotnet watch`)
- ✅ Reload completes in < 5 seconds (target: < 3 seconds based on baseline)
- ✅ Service remains healthy after reload
- ✅ No errors during reload process
- ✅ API endpoint still responds correctly after reload

**Verification Steps**:
1. In Aspire Dashboard, watch Console Logs during reload
2. Look for messages:
   - "Building..."
   - "Application started"
   - "Now listening on..."
3. Verify total time from change to "listening" < 5 seconds
4. Test API endpoint again: `Invoke-RestMethod -Uri http://localhost:7001/health`

---

### Phase 4: Service Discovery Verification

**Objective**: Verify services can discover and communicate via Aspire service discovery.

**Procedure**:
```powershell
# Test Dashboard → API communication (service discovery in action)
# Dashboard makes HTTP calls to "http://api" which resolves via service discovery

# Open Dashboard in browser
Start-Process http://localhost:7000

# Navigate to a page that calls the API (e.g., Event Types page)
# Expected: Data loads from API without errors

# Verify in Aspire Dashboard Traces tab
# 1. Go to Traces tab
# 2. Filter by service: hookverse-dashboard
# 3. Look for HTTP client spans showing calls to "http://api"
# 4. Verify spans show successful status (green)
```

**Acceptance Criteria**:
- ✅ Dashboard can load data from API (proves service discovery works)
- ✅ Distributed traces show cross-service calls
- ✅ No "No such host is known" errors in logs
- ✅ HTTP spans show resolved service names

**Verification Steps**:
1. In Aspire Dashboard, go to **Traces** tab
2. Filter Operation: "HTTP GET"
3. Find a trace originating from "hookverse-dashboard"
4. Expand trace and verify it shows a span for calling "api" service
5. Verify span status is "Ok" (not "Error")

---

### Phase 5: Observability Verification

**Objective**: Verify OpenTelemetry telemetry is exported and visible in Aspire Dashboard.

**Procedure**:
```powershell
# Generate some activity to create telemetry
# Make several API requests
1..10 | ForEach-Object {
    Invoke-RestMethod -Uri http://localhost:7001/health -Method Get
    Start-Sleep -Milliseconds 100
}

# View telemetry in Aspire Dashboard
```

**Acceptance Criteria**:
- ✅ **Metrics** tab shows:
  - HTTP request rate metrics
  - Request duration metrics
  - Runtime metrics (GC, CPU, memory)
- ✅ **Traces** tab shows:
  - Distributed traces for API requests
  - Trace timelines with spans
  - Trace correlation across services
- ✅ **Structured Logs** tab shows:
  - Log entries from all services
  - Log levels (Info, Warning, Error)
  - Correlation IDs matching traces

**Verification Steps**:
1. **Metrics Tab**:
   - Look for "http.server.request.duration" metric
   - Verify chart shows activity from last 10 requests
   - Check metric value is reasonable (< 1 second)

2. **Traces Tab**:
   - Look for traces with operation "GET /health"
   - Verify at least 10 traces exist
   - Click on a trace to view spans
   - Verify span shows tags: http.method=GET, http.status_code=200

3. **Structured Logs Tab**:
   - Filter by service: hookverse-api
   - Verify log entries appear for health check requests
   - Check log level is "Information"
   - Verify TraceId field matches a trace from Traces tab

---

### Phase 6: Manifest Generation

**Objective**: Verify Kubernetes and Azure manifests can be generated in under 30 seconds.

**Procedure**:
```powershell
# Stop AppHost first (Ctrl+C)

# Generate Kubernetes manifests
$manifestStart = Get-Date
dotnet run --project src\HookVerse.AppHost -- --publisher manifest --output-path obj\k8s-test
$manifestDuration = (Get-Date) - $manifestStart
Write-Host "Kubernetes manifest generation time: $($manifestDuration.TotalSeconds) seconds"

# Verify manifests were created
Get-ChildItem obj\k8s-test -Recurse -File | Select-Object Name, Length

# Generate Azure Bicep templates (if supported)
# Note: May require additional publisher configuration
# dotnet run --project src\HookVerse.AppHost -- --publisher manifest --output-path obj\azure-test --format bicep

# Clean up test manifests
Remove-Item obj\k8s-test -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item obj\azure-test -Recurse -Force -ErrorAction SilentlyContinue
```

**Acceptance Criteria**:
- ✅ Manifest generation completes in < 30 seconds (target: < 10 seconds based on baseline)
- ✅ Kubernetes manifest files are created (YAML)
- ✅ Manifest files contain valid Kubernetes resources
- ✅ No errors during manifest generation
- ✅ Manifests include all services (API, Worker, Dashboard)
- ✅ Manifests include container resources (PostgreSQL, RabbitMQ, Redis)

**Verification Steps**:
1. Check generated files exist in obj\k8s-test
2. Open a YAML file and verify structure:
   - apiVersion and kind fields present
   - metadata section with name and labels
   - spec section with container/service configuration
3. Count manifests: Should have files for all 6 resources

---

### Phase 7: Kubernetes Deployment (Optional)

**Objective**: Verify manifests can be deployed to a Kubernetes cluster.

**Prerequisites**:
- kubectl configured and connected to a test cluster
- Cluster has sufficient resources
- Container images are built and pushed to registry

**Procedure**:
```powershell
# WARNING: This requires a real Kubernetes cluster
# Skip if cluster is not available

# Create namespace
kubectl create namespace hookverse-test

# Validate manifests (dry-run)
kubectl apply -f obj\k8s-test\ --dry-run=client --namespace=hookverse-test

# If dry-run succeeds, apply manifests
# kubectl apply -f obj\k8s-test\ --namespace=hookverse-test

# Verify deployment
# kubectl get pods --namespace=hookverse-test
# kubectl get services --namespace=hookverse-test

# Clean up
# kubectl delete namespace hookverse-test
```

**Acceptance Criteria**:
- ✅ `kubectl apply --dry-run` succeeds without validation errors
- ✅ (If deployed) Pods start and reach "Running" status
- ✅ (If deployed) Services are created with ClusterIP/LoadBalancer
- ✅ (If deployed) Health checks pass

**Note**: Full deployment test may require:
- Building container images
- Pushing to container registry
- Configuring image pull secrets
- Setting up managed services (Azure SQL, Service Bus, Redis)

---

### Phase 8: Azure Deployment (Optional)

**Objective**: Verify Bicep templates can be deployed to Azure Container Apps.

**Prerequisites**:
- Azure CLI logged in
- Azure subscription with permissions
- Resource group created
- Container images pushed to Azure Container Registry

**Procedure**:
```powershell
# WARNING: This requires Azure resources and may incur costs
# Skip if Azure environment is not available

# Set variables
$resourceGroup = "hookverse-test-rg"
$location = "eastus"

# Create resource group
az group create --name $resourceGroup --location $location

# Validate Bicep template (if generated)
# az deployment group validate \
#   --resource-group $resourceGroup \
#   --template-file obj\azure-test\main.bicep

# Deploy (if validation succeeds)
# az deployment group create \
#   --resource-group $resourceGroup \
#   --template-file obj\azure-test\main.bicep

# Verify deployment
# az containerapp list --resource-group $resourceGroup --output table

# Clean up
# az group delete --name $resourceGroup --yes --no-wait
```

**Acceptance Criteria**:
- ✅ `az deployment group validate` succeeds
- ✅ (If deployed) Container Apps are created
- ✅ (If deployed) Container Apps show "Running" status
- ✅ (If deployed) Health probes pass

---

## Test Results Template

After completing the smoke test, document results in this format:

```markdown
# Smoke Test Results - [Date]

**Tester**: [Name]  
**Branch**: 002-aspire-orchestration  
**Commit**: [Git SHA]

## Phase 1: Local Startup
- ✅/❌ Status: [PASS/FAIL]
- Startup Time: [X.XX seconds]
- Issues: [None / Description]

## Phase 2: Service Health
- ✅/❌ API Health: [PASS/FAIL]
- ✅/❌ Dashboard Access: [PASS/FAIL]
- ✅/❌ No Errors in Logs: [PASS/FAIL]
- Issues: [None / Description]

## Phase 3: Hot-Reload
- ✅/❌ Status: [PASS/FAIL]
- Reload Time: [X.XX seconds]
- Issues: [None / Description]

## Phase 4: Service Discovery
- ✅/❌ Dashboard → API Communication: [PASS/FAIL]
- ✅/❌ Distributed Traces Visible: [PASS/FAIL]
- Issues: [None / Description]

## Phase 5: Observability
- ✅/❌ Metrics Collected: [PASS/FAIL]
- ✅/❌ Traces Recorded: [PASS/FAIL]
- ✅/❌ Logs Structured: [PASS/FAIL]
- Issues: [None / Description]

## Phase 6: Manifest Generation
- ✅/❌ Status: [PASS/FAIL]
- Generation Time: [X.XX seconds]
- Files Created: [Count]
- Issues: [None / Description]

## Phase 7: Kubernetes Deployment
- ✅/❌/⊘ Status: [PASS/FAIL/SKIPPED]
- Issues: [None / Description]

## Phase 8: Azure Deployment
- ✅/❌/⊘ Status: [PASS/FAIL/SKIPPED]
- Issues: [None / Description]

## Overall Result
- **Status**: ✅ PASS / ❌ FAIL
- **Blocker Issues**: [None / List]
- **Recommendations**: [Any improvements or follow-up actions]

## Screenshots
- [Attach Aspire Dashboard Resources tab]
- [Attach Traces tab showing cross-service calls]
- [Attach generated manifest sample]
```

---

## Troubleshooting

### Docker Not Healthy
**Symptom**: AppHost fails with "Docker runtime found but unhealthy"

**Solution**:
```powershell
# Open Docker Desktop
# Wait for "Engine running" status
# Verify with: docker ps
```

### Port Already in Use
**Symptom**: "Address already in use" error on startup

**Solution**:
```powershell
# Find process using port 17191 (Aspire Dashboard default)
netstat -ano | findstr :17191

# Kill the process or choose different port
# Or restart your machine to clear all ports
```

### AppHost Hangs on Startup
**Symptom**: AppHost starts but never completes, no "listening" message

**Solution**:
1. Check Docker Desktop is running
2. Check disk space (containers need space)
3. Restart Docker Desktop
4. Clean and rebuild: `dotnet clean; dotnet build`

### Integration Tests Fail
**Symptom**: 21/35 tests fail with Docker errors

**Solution**:
This is expected when Docker is unhealthy or not running. Ensure Docker Desktop is started and healthy before running integration tests.

### Manifest Generation Fails
**Symptom**: `dotnet run --project AppHost -- --publisher manifest` fails

**Solution**:
1. Verify Aspire workload is installed: `dotnet workload list`
2. Update to latest SDK: `dotnet workload update`
3. Check AppHost Program.cs has no compilation errors

---

## Success Criteria Validation

After completing the smoke test, verify these align with spec.md Success Criteria:

| Criterion | Target | Measured | Status |
|-----------|--------|----------|--------|
| **SC-001**: Startup time | < 2 minutes | [___] seconds | ✅/❌ |
| **SC-002**: Zero manual config | All connection strings injected | Verified | ✅/❌ |
| **SC-003**: Hot reload | < 5 seconds | [___] seconds | ✅/❌ |
| **SC-004**: Manifest generation | < 30 seconds | [___] seconds | ✅/❌ |

**Expected Results** (based on performance baseline):
- SC-001: ~45 seconds (well under 120s target)
- SC-002: All services use Aspire service discovery
- SC-003: ~2.4 seconds (well under 5s target)
- SC-004: ~5.4 seconds (well under 30s target)

---

## Next Steps After Smoke Test

1. **If all tests pass**:
   - Mark T183 as complete in tasks.md
   - Update success-criteria-verification.md with smoke test results
   - Proceed to remaining tasks (T164-T169 for CI/CD)

2. **If any tests fail**:
   - Document failure details
   - Create GitHub issues for blockers
   - Investigate root cause
   - Implement fixes
   - Re-run smoke test

3. **If deployment tests skipped**:
   - Note that local tests (Phases 1-6) are sufficient for T183 acceptance
   - Kubernetes/Azure deployment tests can be done separately or in staging environment
   - Focus on core functionality validation first

---

**Last Updated**: October 26, 2025  
**Maintainers**: HookVerse Team
