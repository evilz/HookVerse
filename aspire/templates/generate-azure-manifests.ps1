#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates Azure Bicep templates from HookVerse Aspire AppHost.

.DESCRIPTION
    This script uses `dotnet publish` with the azure.pubxml profile to generate
    Azure Bicep templates for deploying HookVerse to Azure Container Apps.
    Validates the generated Bicep files using az bicep build.

.PARAMETER SkipValidation
    Skip validation of generated Bicep templates

.PARAMETER OutputPath
    Custom output path for generated manifests (default: aspire/manifests/azure/)

.EXAMPLE
    ./generate-azure-manifests.ps1
    Generates and validates Azure Bicep templates

.EXAMPLE
    ./generate-azure-manifests.ps1 -SkipValidation
    Generates templates without validation
#>

[CmdletBinding()]
param(
    [switch]$SkipValidation,
    [string]$OutputPath = "$PSScriptRoot\..\..\aspire\manifests\azure"
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Generating Azure Bicep templates for HookVerse..." -ForegroundColor Cyan

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
    Write-Host "📦 Publishing AppHost with azure profile..." -ForegroundColor Yellow
    
    $publishArgs = @(
        "publish"
        $appHostProject
        "/p:PublishProfile=azure"
        "--configuration", "Release"
    )
    
    & dotnet @publishArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ Manifest generation failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    
    $stopwatch.Stop()
    $elapsed = $stopwatch.Elapsed.TotalSeconds
    
    Write-Host "✅ Templates generated in $($elapsed.ToString('F2'))s" -ForegroundColor Green
    
    # Validate Bicep files if requested
    if (-not $SkipValidation) {
        Write-Host "`n🔍 Validating Azure Bicep templates..." -ForegroundColor Yellow
        
        # Check if Azure CLI is available
        $azPath = Get-Command az -ErrorAction SilentlyContinue
        if (-not $azPath) {
            Write-Warning "⚠️ Azure CLI (az) not found - skipping validation"
            Write-Warning "Install Azure CLI to enable Bicep validation"
        }
        else {
            # Validate all Bicep files
            $bicepFiles = Get-ChildItem -Path $outputPath -Filter "*.bicep" -Recurse
            $validationErrors = 0
            
            foreach ($file in $bicepFiles) {
                Write-Host "  Validating $($file.Name)..." -NoNewline
                
                try {
                    $result = & az bicep build --file $file.FullName --stdout 2>&1 | Out-Null
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
                Write-Host "`n✅ All Bicep templates are valid!" -ForegroundColor Green
            }
            else {
                Write-Error "❌ $validationErrors template(s) failed validation"
                exit 1
            }
        }
    }
    
    Write-Host "`n📁 Templates location: $outputPath" -ForegroundColor Cyan
    Write-Host "`n🎉 Azure Bicep template generation complete!" -ForegroundColor Green
    
    if ($elapsed -gt 30) {
        Write-Warning "⚠️ Generation took longer than 30 seconds ($($elapsed.ToString('F2'))s)"
        Write-Warning "This exceeds the SC-004 success criteria target"
    }
}
catch {
    Write-Error "❌ An error occurred: $_"
    exit 1
}
