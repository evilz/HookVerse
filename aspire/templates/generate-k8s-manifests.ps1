#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates Kubernetes manifests from HookVerse Aspire AppHost.

.DESCRIPTION
    This script uses `dotnet publish` with the kubernetes.pubxml profile to generate
    Kubernetes YAML manifests for deploying HookVerse to a Kubernetes cluster.
    Validates the generated manifests using kubectl --dry-run=client.

.PARAMETER Validate
    Whether to validate the generated manifests using kubectl (default: true)

.PARAMETER OutputPath
    Custom output path for generated manifests (default: aspire/manifests/kubernetes/)

.EXAMPLE
    ./generate-k8s-manifests.ps1
    Generates and validates Kubernetes manifests

.EXAMPLE
    ./generate-k8s-manifests.ps1 -Validate:$false
    Generates manifests without validation
#>

[CmdletBinding()]
param(
    [switch]$SkipValidation,
    [string]$OutputPath = "$PSScriptRoot\..\..\aspire\manifests\kubernetes"
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Generating Kubernetes manifests for HookVerse..." -ForegroundColor Cyan

# Resolve paths
$appHostProject = Join-Path $PSScriptRoot "..\..\src\HookVerse.AppHost\HookVerse.AppHost.csproj"
$outputPath = Resolve-Path $OutputPath -ErrorAction SilentlyContinue
if (-not $outputPath) {
    $outputPath = New-Item -Path $OutputPath -ItemType Directory -Force | Select-Object -ExpandProperty FullName
}

# Verify AppHost project exists
if (-not (Test-Path $appHostProject)) {
    Write-Error "❌ AppHost project not found: $appHostProject"
    exit 1
}

# Measure generation time
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    # Generate manifests using dotnet publish
    Write-Host "📦 Publishing AppHost with kubernetes profile..." -ForegroundColor Yellow
    
    $publishArgs = @(
        "publish"
        $appHostProject
        "/p:PublishProfile=kubernetes"
        "--configuration", "Release"
    )
    
    & dotnet @publishArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ Manifest generation failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    
    $stopwatch.Stop()
    $elapsed = $stopwatch.Elapsed.TotalSeconds
    
    Write-Host "✅ Manifests generated in $($elapsed.ToString('F2'))s" -ForegroundColor Green
    
    # Validate manifests unless skipped
    if (-not $SkipValidation) {
        Write-Host "`n🔍 Validating Kubernetes manifests..." -ForegroundColor Yellow
        
        # Check if kubectl is available
        $kubectlPath = Get-Command kubectl -ErrorAction SilentlyContinue
        if (-not $kubectlPath) {
            Write-Warning "⚠️ kubectl not found - skipping validation"
            Write-Warning "Install kubectl to enable manifest validation"
        }
        else {
            # Validate all YAML files
            $yamlFiles = Get-ChildItem -Path $outputPath -Filter "*.yaml" -Recurse
            $validationErrors = 0
            
            foreach ($file in $yamlFiles) {
                Write-Host "  Validating $($file.Name)..." -NoNewline
                
                try {
                    $result = & kubectl apply --dry-run=client -f $file.FullName 2>&1
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host " ✓" -ForegroundColor Green
                    }
                    else {
                        Write-Host " ✗" -ForegroundColor Red
                        Write-Host "    Error: $result" -ForegroundColor Red
                        $validationErrors++
                    }
                }
                catch {
                    Write-Host " ✗" -ForegroundColor Red
                    Write-Host "    Error: $_" -ForegroundColor Red
                    $validationErrors++
                }
            }
            
            if ($validationErrors -eq 0) {
                Write-Host "`n✅ All manifests are valid!" -ForegroundColor Green
            }
            else {
                Write-Error "❌ $validationErrors manifest(s) failed validation"
                exit 1
            }
        }
    }
    
    Write-Host "`n📁 Manifests location: $outputPath" -ForegroundColor Cyan
    Write-Host "`n🎉 Kubernetes manifest generation complete!" -ForegroundColor Green
    
    if ($elapsed -gt 30) {
        Write-Warning "⚠️ Generation took longer than 30 seconds ($($elapsed.ToString('F2'))s)"
        Write-Warning "This exceeds the SC-004 success criteria target"
    }
}
catch {
    Write-Error "❌ An error occurred: $_"
    exit 1
}
