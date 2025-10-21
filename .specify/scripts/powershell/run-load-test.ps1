<#
.SYNOPSIS
    Runs load testing for HookVerse webhook delivery platform.

.DESCRIPTION
    This script orchestrates load testing using k6 to validate the system
    can handle 10,000 webhooks/second. It sets up the test environment,
    runs the load test, and generates reports.

.PARAMETER Target
    Target requests per second (default: 10000).

.PARAMETER Duration
    Test duration in minutes (default: 30).

.PARAMETER ApiUrl
    API base URL (default: http://localhost:5000).

.PARAMETER ApiKey
    API key for authentication.

.PARAMETER OutputDir
    Output directory for test results (default: ./load-test-results).

.PARAMETER SkipSetup
    Skip environment setup checks.

.EXAMPLE
    .\run-load-test.ps1
    Runs default load test targeting 10K req/s for 30 minutes.

.EXAMPLE
    .\run-load-test.ps1 -Target 5000 -Duration 15
    Runs load test targeting 5K req/s for 15 minutes.
#>

param(
    [int]$Target = 10000,
    [int]$Duration = 30,
    [string]$ApiUrl = "http://localhost:5000",
    [string]$ApiKey = "",
    [string]$OutputDir = "./load-test-results",
    [switch]$SkipSetup
)

$ErrorActionPreference = "Stop"

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Failure {
    param([string]$Message)
    Write-Host "[FAILURE] $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

Write-Step "HookVerse Load Test"

# Check prerequisites
if (-not $SkipSetup) {
    Write-Step "Step 1: Checking Prerequisites"

    # Check k6
    Write-Info "Checking k6..."
    try {
        $k6Version = k6 version
        Write-Success "k6 found: $k6Version"
    } catch {
        Write-Failure "k6 not found. Please install k6:"
        Write-Host "  Windows: choco install k6" -ForegroundColor Yellow
        Write-Host "  Or download from: https://k6.io/docs/getting-started/installation" -ForegroundColor Yellow
        exit 1
    }

    # Check API availability
    Write-Info "Checking API availability at $ApiUrl..."
    try {
        $healthResponse = Invoke-RestMethod -Uri "$ApiUrl/health" -Method Get -TimeoutSec 5
        Write-Success "API is responding: $($healthResponse.status)"
    } catch {
        Write-Failure "API is not responding at $ApiUrl"
        Write-Host "  Please ensure the API is running before starting load test." -ForegroundColor Yellow
        exit 1
    }
}

# Create output directory
Write-Step "Step 2: Setting Up Test Environment"

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
    Write-Info "Created output directory: $OutputDir"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$testRunDir = Join-Path $OutputDir "run-$timestamp"
New-Item -ItemType Directory -Path $testRunDir | Out-Null

Write-Success "Test results will be saved to: $testRunDir"

# Prepare environment variables
$env:API_URL = $ApiUrl
if ($ApiKey) {
    $env:API_KEY = $ApiKey
}

# Display test configuration
Write-Step "Step 3: Test Configuration"

Write-Host "Target: $Target req/s" -ForegroundColor White
Write-Host "Duration: $Duration minutes" -ForegroundColor White
Write-Host "API URL: $ApiUrl" -ForegroundColor White
Write-Host "Output Dir: $testRunDir" -ForegroundColor White
Write-Host ""

# Confirm before proceeding
Write-Host "This load test will generate significant traffic." -ForegroundColor Yellow
Write-Host "Ensure your infrastructure can handle the load." -ForegroundColor Yellow
Write-Host ""
$confirmation = Read-Host "Continue? (yes/no)"

if ($confirmation -ne "yes" -and $confirmation -ne "y") {
    Write-Info "Load test cancelled by user."
    exit 0
}

# Run load test
Write-Step "Step 4: Running Load Test"

Write-Info "Starting k6 load test..."
Write-Info "This will take approximately $Duration minutes..."

$k6Script = Join-Path $PSScriptRoot "..\k6\load-test.js"
$summaryJson = Join-Path $testRunDir "summary.json"
$summaryHtml = Join-Path $testRunDir "summary.html"

try {
    $startTime = Get-Date
    
    # Run k6 with custom options
    k6 run `
        --out json="$testRunDir/metrics.json" `
        --summary-export="$summaryJson" `
        $k6Script
    
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalMinutes
    
    Write-Success "Load test completed in $($duration.ToString('F2')) minutes"
} catch {
    Write-Failure "Load test failed: $($_.Exception.Message)"
    exit 1
}

# Generate report
Write-Step "Step 5: Generating Report"

if (Test-Path $summaryJson) {
    Write-Info "Parsing results..."
    
    $summary = Get-Content $summaryJson | ConvertFrom-Json
    
    Write-Host ""
    Write-Host "=== Load Test Results ===" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Requests:" -ForegroundColor White
    Write-Host "  Total: $($summary.metrics.http_reqs.values.count)" -ForegroundColor White
    Write-Host "  Rate: $($summary.metrics.http_reqs.values.rate.ToString('F2')) req/s" -ForegroundColor White
    Write-Host ""
    
    Write-Host "Response Times:" -ForegroundColor White
    Write-Host "  Min: $($summary.metrics.http_req_duration.values.min.ToString('F2'))ms" -ForegroundColor White
    Write-Host "  Avg: $($summary.metrics.http_req_duration.values.avg.ToString('F2'))ms" -ForegroundColor White
    Write-Host "  P95: $($summary.metrics.http_req_duration.values['p(95)'].ToString('F2'))ms" -ForegroundColor White
    Write-Host "  P99: $($summary.metrics.http_req_duration.values['p(99)'].ToString('F2'))ms" -ForegroundColor White
    Write-Host "  Max: $($summary.metrics.http_req_duration.values.max.ToString('F2'))ms" -ForegroundColor White
    Write-Host ""
    
    $errorRate = if ($summary.metrics.errors.values.rate) { $summary.metrics.errors.values.rate * 100 } else { 0 }
    $errorColor = if ($errorRate -lt 1) { "Green" } else { "Red" }
    
    Write-Host "Errors:" -ForegroundColor White
    Write-Host "  Rate: $($errorRate.ToString('F2'))%" -ForegroundColor $errorColor
    Write-Host ""
    
    Write-Host "Thresholds:" -ForegroundColor White
    foreach ($threshold in $summary.thresholds.PSObject.Properties) {
        $status = if ($threshold.Value.ok) { "PASS" } else { "FAIL" }
        $color = if ($threshold.Value.ok) { "Green" } else { "Red" }
        Write-Host "  $($threshold.Name): $status" -ForegroundColor $color
    }
    Write-Host ""
    
    # Overall assessment
    $allThresholdsPassed = ($summary.thresholds.PSObject.Properties | Where-Object { -not $_.Value.ok }).Count -eq 0
    
    if ($allThresholdsPassed -and $summary.metrics.http_reqs.values.rate -ge ($Target * 0.9)) {
        Write-Success "Load test PASSED! System handled $($summary.metrics.http_reqs.values.rate.ToString('F2')) req/s"
        Write-Success "Target was $Target req/s (achieved $((($summary.metrics.http_reqs.values.rate / $Target) * 100).ToString('F1'))%)"
    } elseif ($allThresholdsPassed) {
        Write-Host "[WARNING] Load test passed thresholds but didn't reach target throughput" -ForegroundColor Yellow
        Write-Host "  Achieved: $($summary.metrics.http_reqs.values.rate.ToString('F2')) req/s" -ForegroundColor Yellow
        Write-Host "  Target: $Target req/s" -ForegroundColor Yellow
    } else {
        Write-Failure "Load test FAILED! Some thresholds were not met"
        exit 1
    }
    
    Write-Host ""
    Write-Info "Detailed results saved to: $testRunDir"
    Write-Info "Summary JSON: $summaryJson"
    Write-Info "Summary HTML: $summaryHtml"
} else {
    Write-Failure "Summary file not found. Load test may have failed."
    exit 1
}

Write-Step "Load Test Complete"
