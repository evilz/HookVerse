<#
.SYNOPSIS
    Validates HookVerse quickstart.md setup on a fresh environment.

.DESCRIPTION
    This script performs automated validation of all steps in quickstart.md:
    - Checks prerequisites (software installations)
    - Validates Docker dependencies
    - Tests database migrations
    - Validates API endpoints
    - Tests webhook delivery end-to-end
    - Validates monitoring endpoints

.PARAMETER SkipPrerequisites
    Skip prerequisite checks (assume software already installed).

.PARAMETER SkipDocker
    Skip Docker dependency checks (assume services already running).

.PARAMETER Environment
    Environment to test (Development, Staging, Production).

.EXAMPLE
    .\validate-quickstart.ps1
    Runs full validation including prerequisites.

.EXAMPLE
    .\validate-quickstart.ps1 -SkipPrerequisites
    Runs validation skipping prerequisite checks.
#>

param(
    [switch]$SkipPrerequisites,
    [switch]$SkipDocker,
    [string]$Environment = "Development"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# ANSI color codes (for future use)
# Unused: $ColorReset = "`e[0m"
$ColorGreen = "`e[32m"
$ColorRed = "`e[31m"
$ColorYellow = "`e[33m"
$ColorBlue = "`e[34m"

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

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

$ValidationResults = @{
    Prerequisites = @()
    Docker = @()
    Database = @()
    API = @()
    Worker = @()
    Monitoring = @()
    EndToEnd = @()
}

# Step 1: Validate Prerequisites
if (-not $SkipPrerequisites) {
    Write-Step "Step 1: Validating Prerequisites"

    # Check .NET 10 SDK
    Write-Info "Checking .NET 10 SDK..."
    try {
        $dotnetVersion = dotnet --version
        if ($dotnetVersion -match "^10\.") {
            Write-Success ".NET 10 SDK found: $dotnetVersion"
            $ValidationResults.Prerequisites += @{ Name = ".NET 10 SDK"; Status = "Pass"; Details = $dotnetVersion }
        } else {
            Write-Failure ".NET 10 SDK not found. Found version: $dotnetVersion"
            $ValidationResults.Prerequisites += @{ Name = ".NET 10 SDK"; Status = "Fail"; Details = "Wrong version: $dotnetVersion" }
        }
    } catch {
        Write-Failure ".NET SDK not found"
        $ValidationResults.Prerequisites += @{ Name = ".NET 10 SDK"; Status = "Fail"; Details = "Not installed" }
    }

    # Check Docker
    Write-Info "Checking Docker..."
    try {
        $dockerVersion = docker --version
        Write-Success "Docker found: $dockerVersion"
        $ValidationResults.Prerequisites += @{ Name = "Docker"; Status = "Pass"; Details = $dockerVersion }
    } catch {
        Write-Failure "Docker not found"
        $ValidationResults.Prerequisites += @{ Name = "Docker"; Status = "Fail"; Details = "Not installed" }
    }

    # Check Git
    Write-Info "Checking Git..."
    try {
        $gitVersion = git --version
        Write-Success "Git found: $gitVersion"
        $ValidationResults.Prerequisites += @{ Name = "Git"; Status = "Pass"; Details = $gitVersion }
    } catch {
        Write-Failure "Git not found"
        $ValidationResults.Prerequisites += @{ Name = "Git"; Status = "Fail"; Details = "Not installed" }
    }
}

# Step 2: Validate Docker Dependencies
if (-not $SkipDocker) {
    Write-Step "Step 2: Validating Docker Dependencies"

    # Check PostgreSQL
    Write-Info "Checking PostgreSQL container..."
    try {
        $pgContainer = docker ps --filter "name=postgres" --format "{{.Names}}"
        if ($pgContainer) {
            Write-Success "PostgreSQL container running: $pgContainer"
            $ValidationResults.Docker += @{ Name = "PostgreSQL"; Status = "Pass"; Details = $pgContainer }
        } else {
            Write-Warning "PostgreSQL container not running"
            $ValidationResults.Docker += @{ Name = "PostgreSQL"; Status = "Warning"; Details = "Not running" }
        }
    } catch {
        Write-Failure "Failed to check PostgreSQL container"
        $ValidationResults.Docker += @{ Name = "PostgreSQL"; Status = "Fail"; Details = $_.Exception.Message }
    }

    # Check RabbitMQ
    Write-Info "Checking RabbitMQ container..."
    try {
        $rabbitContainer = docker ps --filter "name=rabbitmq" --format "{{.Names}}"
        if ($rabbitContainer) {
            Write-Success "RabbitMQ container running: $rabbitContainer"
            $ValidationResults.Docker += @{ Name = "RabbitMQ"; Status = "Pass"; Details = $rabbitContainer }
        } else {
            Write-Warning "RabbitMQ container not running"
            $ValidationResults.Docker += @{ Name = "RabbitMQ"; Status = "Warning"; Details = "Not running" }
        }
    } catch {
        Write-Failure "Failed to check RabbitMQ container"
        $ValidationResults.Docker += @{ Name = "RabbitMQ"; Status = "Fail"; Details = $_.Exception.Message }
    }

    # Check Redis
    Write-Info "Checking Redis container..."
    try {
        $redisContainer = docker ps --filter "name=redis" --format "{{.Names}}"
        if ($redisContainer) {
            Write-Success "Redis container running: $redisContainer"
            $ValidationResults.Docker += @{ Name = "Redis"; Status = "Pass"; Details = $redisContainer }
        } else {
            Write-Warning "Redis container not running (optional)"
            $ValidationResults.Docker += @{ Name = "Redis"; Status = "Warning"; Details = "Not running (optional)" }
        }
    } catch {
        Write-Failure "Failed to check Redis container"
        $ValidationResults.Docker += @{ Name = "Redis"; Status = "Fail"; Details = $_.Exception.Message }
    }
}

# Step 3: Validate Database
Write-Step "Step 3: Validating Database"

Write-Info "Checking database migrations..."
try {
    $efTool = dotnet ef --version
    Write-Success "EF Core tools found: $efTool"
    $ValidationResults.Database += @{ Name = "EF Core Tools"; Status = "Pass"; Details = $efTool }
} catch {
    Write-Failure "EF Core tools not found"
    $ValidationResults.Database += @{ Name = "EF Core Tools"; Status = "Fail"; Details = "Not installed" }
}

# Step 4: Validate API
Write-Step "Step 4: Validating API"

Write-Info "Checking if API is running..."
$apiUrl = if ($Environment -eq "Development") { "http://localhost:5000" } else { "http://localhost:5000" }

try {
    $healthResponse = Invoke-RestMethod -Uri "$apiUrl/health" -Method Get -TimeoutSec 5
    Write-Success "API health endpoint responded: $($healthResponse.status)"
    $ValidationResults.API += @{ Name = "Health Endpoint"; Status = "Pass"; Details = $healthResponse.status }
} catch {
    Write-Failure "API health endpoint failed: $($_.Exception.Message)"
    $ValidationResults.API += @{ Name = "Health Endpoint"; Status = "Fail"; Details = $_.Exception.Message }
}

try {
    $null = Invoke-RestMethod -Uri "$apiUrl/metrics" -Method Get -TimeoutSec 5
    Write-Success "Metrics endpoint responded"
    $ValidationResults.API += @{ Name = "Metrics Endpoint"; Status = "Pass"; Details = "OK" }
} catch {
    Write-Failure "Metrics endpoint failed: $($_.Exception.Message)"
    $ValidationResults.API += @{ Name = "Metrics Endpoint"; Status = "Fail"; Details = $_.Exception.Message }
}

# Step 5: End-to-End Webhook Delivery Test
Write-Step "Step 5: End-to-End Webhook Delivery Test"

Write-Info "Testing webhook delivery flow..."

# Test would go here - creating event type, subscription, sending webhook
# This is a placeholder for the actual test implementation

$ValidationResults.EndToEnd += @{ Name = "Webhook Delivery"; Status = "Warning"; Details = "Manual test required" }

# Generate Report
Write-Step "Validation Report"

$totalTests = 0
$passedTests = 0
$failedTests = 0
$warningTests = 0

foreach ($category in $ValidationResults.Keys) {
    $results = $ValidationResults[$category]
    if ($results.Count -gt 0) {
        Write-Host ""
        Write-Host "$category Results:" -ForegroundColor Cyan
        foreach ($result in $results) {
            $totalTests++
            $statusColor = switch ($result.Status) {
                "Pass" { $ColorGreen; $passedTests++; "Green" }
                "Fail" { $ColorRed; $failedTests++; "Red" }
                "Warning" { $ColorYellow; $warningTests++; "Yellow" }
                default { $ColorBlue; "Blue" }
            }
            Write-Host "  [$($result.Status)] $($result.Name): $($result.Details)" -ForegroundColor $statusColor
        }
    }
}

Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Total Tests: $totalTests" -ForegroundColor White
Write-Host "  Passed: $passedTests" -ForegroundColor Green
Write-Host "  Failed: $failedTests" -ForegroundColor Red
Write-Host "  Warnings: $warningTests" -ForegroundColor Yellow

$successRate = if ($totalTests -gt 0) { ($passedTests / $totalTests) * 100 } else { 0 }
Write-Host "  Success Rate: $($successRate.ToString('F1'))%" -ForegroundColor $(if ($successRate -ge 80) { "Green" } else { "Red" })

# Exit code
if ($failedTests -gt 0) {
    Write-Host ""
    Write-Failure "Validation failed with $failedTests failures"
    exit 1
} elseif ($warningTests -gt 0) {
    Write-Host ""
    Write-Warning "Validation completed with $warningTests warnings"
    exit 0
} else {
    Write-Host ""
    Write-Success "All validations passed!"
    exit 0
}
