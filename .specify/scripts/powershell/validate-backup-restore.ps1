<#
.SYNOPSIS
    Validates database backup and restore procedures for HookVerse.

.DESCRIPTION
    This script tests the complete backup and restore workflow:
    1. Creates a test database backup
    2. Validates backup integrity
    3. Performs restore to a test database
    4. Verifies data integrity after restore
    5. Cleans up test resources

.PARAMETER DatabaseHost
    Database host (default: localhost).

.PARAMETER DatabasePort
    Database port (default: 5432).

.PARAMETER DatabaseName
    Database name (default: hookverse).

.PARAMETER DatabaseUser
    Database user (default: hookverse).

.PARAMETER DatabasePassword
    Database password.

.PARAMETER BackupDir
    Backup directory (default: ./backups).

.PARAMETER SkipRestore
    Skip restore validation (backup only).

.EXAMPLE
    .\validate-backup-restore.ps1 -DatabasePassword "your_password"
    Runs full backup and restore validation.

.EXAMPLE
    .\validate-backup-restore.ps1 -SkipRestore
    Tests backup creation only.
#>

param(
    [string]$DatabaseHost = "localhost",
    [int]$DatabasePort = 5432,
    [string]$DatabaseName = "hookverse",
    [string]$DatabaseUser = "hookverse",
    [string]$DatabasePassword = "",
    [string]$BackupDir = "./backups",
    [switch]$SkipRestore
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

Write-Step "Database Backup and Restore Validation"

# Validate prerequisites
Write-Step "Step 1: Validating Prerequisites"

# Check pg_dump
Write-Info "Checking pg_dump..."
try {
    $pgDumpVersion = pg_dump --version
    Write-Success "pg_dump found: $pgDumpVersion"
} catch {
    Write-Failure "pg_dump not found. Please install PostgreSQL client tools."
    exit 1
}

# Check psql
Write-Info "Checking psql..."
try {
    $psqlVersion = psql --version
    Write-Success "psql found: $psqlVersion"
} catch {
    Write-Failure "psql not found. Please install PostgreSQL client tools."
    exit 1
}

# Set password environment variable
if ($DatabasePassword) {
    $env:PGPASSWORD = $DatabasePassword
}

# Test database connection
Write-Info "Testing database connection..."
try {
    $connectionTest = psql -h $DatabaseHost -p $DatabasePort -U $DatabaseUser -d $DatabaseName -c "SELECT 1" -t -A
    if ($connectionTest -eq "1") {
        Write-Success "Database connection successful"
    } else {
        Write-Failure "Database connection test returned unexpected result"
        exit 1
    }
} catch {
    Write-Failure "Cannot connect to database: $($_.Exception.Message)"
    exit 1
}

# Create backup directory
Write-Step "Step 2: Preparing Backup Directory"

if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir | Out-Null
    Write-Info "Created backup directory: $BackupDir"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupFile = Join-Path $BackupDir "hookverse-backup-$timestamp.sql"
$backupGzFile = "$backupFile.gz"

Write-Success "Backup will be saved to: $backupFile"

# Get pre-backup statistics
Write-Step "Step 3: Collecting Pre-Backup Statistics"

Write-Info "Gathering database statistics..."

$preBackupStats = @{}

# Table counts
$tables = @("Tenants", "ApiKeys", "EventTypes", "Subscriptions", "WebhookEvents", "WebhookDeliveryAttempts")

foreach ($table in $tables) {
    try {
        $count = psql -h $DatabaseHost -p $DatabasePort -U $DatabaseUser -d $DatabaseName `
            -c "SELECT COUNT(*) FROM `"$table`"" -t -A
        $preBackupStats[$table] = [int]$count
        Write-Info "  ${table}: $count rows"
    } catch {
        Write-Info "  ${table}: Table not found or error (may not exist yet)"
        $preBackupStats[$table] = 0
    }
}

# Database size
try {
    $dbSize = psql -h $DatabaseHost -p $DatabasePort -U $DatabaseUser -d $DatabaseName `
        -c "SELECT pg_size_pretty(pg_database_size('$DatabaseName'))" -t -A
    Write-Info "  Database size: $dbSize"
    $preBackupStats["DatabaseSize"] = $dbSize
} catch {
    Write-Info "  Database size: Unable to determine"
}

# Create backup
Write-Step "Step 4: Creating Database Backup"

Write-Info "Running pg_dump..."
$backupStartTime = Get-Date

try {
    pg_dump -h $DatabaseHost `
        -p $DatabasePort `
        -U $DatabaseUser `
        -d $DatabaseName `
        -F p `
        -f $backupFile `
        --verbose
    
    $backupEndTime = Get-Date
    $backupDuration = ($backupEndTime - $backupStartTime).TotalSeconds
    
    Write-Success "Backup completed in $($backupDuration.ToString('F2')) seconds"
} catch {
    Write-Failure "Backup failed: $($_.Exception.Message)"
    exit 1
}

# Validate backup file
Write-Info "Validating backup file..."

if (Test-Path $backupFile) {
    $backupSize = (Get-Item $backupFile).Length
    $backupSizeMB = [math]::Round($backupSize / 1MB, 2)
    Write-Success "Backup file created: $backupSizeMB MB"
    
    # Compress backup
    Write-Info "Compressing backup..."
    try {
        if (Get-Command gzip -ErrorAction SilentlyContinue) {
            gzip -c $backupFile > $backupGzFile
            $compressedSize = (Get-Item $backupGzFile).Length
            $compressedSizeMB = [math]::Round($compressedSize / 1MB, 2)
            $compressionRatio = [math]::Round(($compressedSize / $backupSize) * 100, 1)
            Write-Success "Compressed backup: $compressedSizeMB MB ($compressionRatio% of original)"
        } else {
            Write-Info "gzip not available, skipping compression"
        }
    } catch {
        Write-Info "Compression failed, backup retained uncompressed"
    }
} else {
    Write-Failure "Backup file not created"
    exit 1
}

# Validate backup content
Write-Info "Validating backup content..."
$backupContent = Get-Content $backupFile -TotalCount 50
if ($backupContent -match "PostgreSQL database dump") {
    Write-Success "Backup file appears valid"
} else {
    Write-Failure "Backup file may be corrupted"
    exit 1
}

# Skip restore if requested
if ($SkipRestore) {
    Write-Step "Restore Validation Skipped"
    Write-Success "Backup validation completed successfully"
    Write-Info "Backup saved to: $backupFile"
    exit 0
}

# Restore to test database
Write-Step "Step 5: Testing Restore Procedure"

$testDbName = "$DatabaseName`_restore_test_$timestamp"

Write-Info "Creating test database: $testDbName"

try {
    # Create test database
    psql -h $DatabaseHost -p $DatabasePort -U $DatabaseUser -d postgres `
        -c "CREATE DATABASE `"$testDbName`"" | Out-Null
    
    Write-Success "Test database created"
} catch {
    Write-Failure "Failed to create test database: $($_.Exception.Message)"
    exit 1
}

Write-Info "Restoring backup to test database..."
$restoreStartTime = Get-Date

try {
    psql -h $DatabaseHost `
        -p $DatabasePort `
        -U $DatabaseUser `
        -d $testDbName `
        -f $backupFile `
        --quiet
    
    $restoreEndTime = Get-Date
    $restoreDuration = ($restoreEndTime - $restoreStartTime).TotalSeconds
    
    Write-Success "Restore completed in $($restoreDuration.ToString('F2')) seconds"
} catch {
    Write-Failure "Restore failed: $($_.Exception.Message)"
    
    # Cleanup
    Write-Info "Cleaning up test database..."
    psql -h $DatabaseHost -p $DatabasePort -U $DatabaseUser -d postgres `
        -c "DROP DATABASE IF EXISTS `"$testDbName`"" | Out-Null
    
    exit 1
}

# Validate restored data
Write-Step "Step 6: Validating Restored Data"

Write-Info "Comparing table counts..."

$validationPassed = $true

foreach ($table in $tables) {
    try {
        $restoredCount = psql -h $DatabaseHost -p $DatabasePort -U $DatabaseUser -d $testDbName `
            -c "SELECT COUNT(*) FROM `"$table`"" -t -A
        $restoredCount = [int]$restoredCount
        $originalCount = $preBackupStats[$table]
        
        if ($restoredCount -eq $originalCount) {
            Write-Success "  ${table}: $restoredCount rows (matches original)"
        } else {
            Write-Failure "  ${table}: $restoredCount rows (expected $originalCount)"
            $validationPassed = $false
        }
    } catch {
        Write-Info "  ${table}: Validation skipped (may not exist)"
    }
}

# Validate database size
try {
    $restoredDbSize = psql -h $DatabaseHost -p $DatabasePort -U $DatabaseUser -d $testDbName `
        -c "SELECT pg_size_pretty(pg_database_size('$testDbName'))" -t -A
    Write-Info "  Restored database size: $restoredDbSize"
} catch {
    Write-Info "  Restored database size: Unable to determine"
}

# Cleanup test database
Write-Step "Step 7: Cleaning Up"

Write-Info "Dropping test database..."
try {
    psql -h $DatabaseHost -p $DatabasePort -U $DatabaseUser -d postgres `
        -c "DROP DATABASE `"$testDbName`"" | Out-Null
    Write-Success "Test database removed"
} catch {
    Write-Failure "Failed to remove test database. Manual cleanup may be required."
}

# Generate report
Write-Step "Validation Report"

Write-Host ""
Write-Host "Backup Information:" -ForegroundColor Cyan
Write-Host "  File: $backupFile" -ForegroundColor White
Write-Host "  Size: $backupSizeMB MB" -ForegroundColor White
Write-Host "  Duration: $($backupDuration.ToString('F2')) seconds" -ForegroundColor White
Write-Host ""

Write-Host "Restore Information:" -ForegroundColor Cyan
Write-Host "  Duration: $($restoreDuration.ToString('F2')) seconds" -ForegroundColor White
Write-Host "  Test Database: $testDbName (removed)" -ForegroundColor White
Write-Host ""

Write-Host "Data Validation:" -ForegroundColor Cyan
foreach ($table in $tables) {
    $originalCount = $preBackupStats[$table]
    Write-Host "  ${table}: $originalCount rows" -ForegroundColor White
}
Write-Host ""

if ($validationPassed) {
    Write-Success "Backup and Restore Validation PASSED"
    Write-Host ""
    Write-Info "Recommendations:"
    Write-Host "  - Schedule regular automated backups" -ForegroundColor Yellow
    Write-Host "  - Test restore procedure monthly" -ForegroundColor Yellow
    Write-Host "  - Store backups in multiple locations" -ForegroundColor Yellow
    Write-Host "  - Retain backups according to compliance requirements" -ForegroundColor Yellow
    exit 0
} else {
    Write-Failure "Backup and Restore Validation FAILED"
    Write-Host "  Some data validation checks failed. Review output above." -ForegroundColor Red
    exit 1
}
