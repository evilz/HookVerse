# Vulnerability Scanner Script for HookVerse
# Scans .NET dependencies for known security vulnerabilities

param(
    [switch]$IncludeTransitive,
    [switch]$CheckOutdated,
    [switch]$FailOnVulnerabilities,
    [ValidateSet('Critical', 'High', 'Moderate', 'Low')]
    [string]$MinimumSeverity = 'Moderate',
    [switch]$Json,
    [switch]$Detailed
)

# Set default for IncludeTransitive
if (-not $PSBoundParameters.ContainsKey('IncludeTransitive')) {
    $IncludeTransitive = $true
}

$ErrorActionPreference = 'Continue'
$VulnerabilitiesFound = $false
$ReportPath = Join-Path $PSScriptRoot "..\..\vulnerability-reports"

# Create reports directory if it doesn't exist
if (-not (Test-Path $ReportPath)) {
    New-Item -ItemType Directory -Path $ReportPath -Force | Out-Null
}

$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$ReportFile = Join-Path $ReportPath "vulnerability-report-$Timestamp.txt"

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  HookVerse Dependency Vulnerability Scanner" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Check if dotnet is installed
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ .NET SDK is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

Write-Host "✓ .NET SDK Version: $dotnetVersion" -ForegroundColor Green
Write-Host ""

# Navigate to repository root
$RepoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Push-Location $RepoRoot

try {
    Write-Host "Scanning for vulnerable packages..." -ForegroundColor Yellow
    Write-Host "  Minimum Severity: $MinimumSeverity" -ForegroundColor Gray
    Write-Host "  Include Transitive: $IncludeTransitive" -ForegroundColor Gray
    Write-Host ""

    # Build vulnerability scan command
    $VulnArgs = @('list', 'package', '--vulnerable')
    if ($IncludeTransitive) {
        $VulnArgs += '--include-transitive'
    }
    if ($Json) {
        $VulnArgs += '--format', 'json'
    }

    # Run vulnerability scan
    $VulnOutput = & dotnet @VulnArgs 2>&1 | Tee-Object -FilePath $ReportFile

    # Parse output for vulnerabilities
    $HasVulnerabilities = $VulnOutput | Select-String "has the following vulnerable packages"
    
    if ($HasVulnerabilities) {
        $VulnerabilitiesFound = $true
        Write-Host ""
        Write-Host "❌ VULNERABILITIES DETECTED!" -ForegroundColor Red
        Write-Host ""
        
        # Parse and display vulnerabilities
        $CurrentProject = $null
        
        foreach ($line in $VulnOutput) {
            if ($line -match "Project \`(.+?)\`") {
                $CurrentProject = $Matches[1]
                Write-Host "[Project] $CurrentProject" -ForegroundColor Cyan
            }
            elseif ($line -match "Top-level Package\s+Requested\s+Resolved\s+Severity\s+Advisory URL") {
                Write-Host "   $line" -ForegroundColor Gray
            }
            elseif ($line -match ">\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(.+)") {
                $Package = $Matches[1]
                $Requested = $Matches[2]
                $Resolved = $Matches[3]
                $Severity = $Matches[4]
                $Advisory = $Matches[5]
                
                $Color = switch ($Severity) {
                    'Critical' { 'Red' }
                    'High' { 'Red' }
                    'Moderate' { 'Yellow' }
                    'Low' { 'Gray' }
                    default { 'White' }
                }
                
                Write-Host "   > " -NoNewline
                Write-Host "$Package " -ForegroundColor White -NoNewline
                Write-Host "$Requested → $Resolved " -ForegroundColor Gray -NoNewline
                Write-Host "[$Severity] " -ForegroundColor $Color -NoNewline
                Write-Host $Advisory -ForegroundColor Blue
            }
            elseif ($line -match "Transitive Package\s+Resolved\s+Severity\s+Advisory URL") {
                Write-Host ""
                Write-Host "   Transitive Dependencies:" -ForegroundColor DarkGray
                Write-Host "   $line" -ForegroundColor Gray
            }
        }
        
        Write-Host ""
        Write-Host "Report saved to: $ReportFile" -ForegroundColor Gray
    }
    else {
        Write-Host "✅ No vulnerabilities detected" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "───────────────────────────────────────────────────────" -ForegroundColor DarkGray

    # Check for outdated packages
    if ($CheckOutdated) {
        Write-Host ""
        Write-Host "Checking for outdated packages..." -ForegroundColor Yellow
        Write-Host ""

        $OutdatedArgs = @('list', 'package', '--outdated')
        if ($IncludeTransitive) {
            $OutdatedArgs += '--include-transitive'
        }

        $OutdatedFile = Join-Path $ReportPath "outdated-packages-$Timestamp.txt"
        $OutdatedOutput = & dotnet @OutdatedArgs 2>&1 | Tee-Object -FilePath $OutdatedFile

        $HasOutdated = $OutdatedOutput | Select-String "has the following updates"
        
        if ($HasOutdated) {
            Write-Host "[Outdated] Outdated packages found:" -ForegroundColor Yellow
            Write-Host $OutdatedOutput
            Write-Host ""
            Write-Host "Outdated packages report saved to: $OutdatedFile" -ForegroundColor Gray
        }
        else {
            Write-Host "✅ All packages are up to date" -ForegroundColor Green
        }
        
        Write-Host ""
        Write-Host "───────────────────────────────────────────────────────" -ForegroundColor DarkGray
    }

    # Display summary
    Write-Host ""
    Write-Host "SCAN SUMMARY" -ForegroundColor Cyan
    Write-Host "───────────────────────────────────────────────────────" -ForegroundColor DarkGray
    Write-Host "  Scan Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
    Write-Host "  Report Location: $ReportPath" -ForegroundColor Gray
    
    if ($VulnerabilitiesFound) {
        Write-Host "  Status: " -NoNewline -ForegroundColor Gray
        Write-Host "VULNERABILITIES FOUND" -ForegroundColor Red
        Write-Host ""
        Write-Host "RECOMMENDATIONS:" -ForegroundColor Yellow
        Write-Host "  1. Review vulnerability details in report file" -ForegroundColor White
        Write-Host "  2. Update affected packages: dotnet add package PackageName" -ForegroundColor White
        Write-Host "  3. Run tests after updates: dotnet test" -ForegroundColor White
        Write-Host "  4. Re-scan after fixes: .\scripts\powershell\scan-vulnerabilities.ps1" -ForegroundColor White
    }
    else {
        Write-Host "  Status: " -NoNewline -ForegroundColor Gray
        Write-Host "CLEAN" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan

    # Fail if requested and vulnerabilities found
    if ($FailOnVulnerabilities -and $VulnerabilitiesFound) {
        Write-Host ""
        Write-Host "Failing build due to vulnerabilities..." -ForegroundColor Red
        exit 1
    }

    if ($VulnerabilitiesFound) {
        exit 1
    }
}
finally {
    Pop-Location
}

exit 0
