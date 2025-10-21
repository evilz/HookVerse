#!/usr/bin/env pwsh
# HookVerse Quick Start Script
# Starts Docker services and verifies they're healthy

$ErrorActionPreference = "Stop"

Write-Host "🚀 HookVerse Quick Start" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is installed
Write-Host "Checking Docker installation..." -ForegroundColor Yellow
try {
    $dockerVersion = docker --version
    Write-Host "✓ Docker found: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Docker not found. Please install Docker Desktop." -ForegroundColor Red
    Write-Host "  Download from: https://www.docker.com/products/docker-desktop" -ForegroundColor Red
    exit 1
}

# Check if Docker daemon is running
Write-Host "Checking Docker daemon..." -ForegroundColor Yellow
try {
    docker ps | Out-Null
    Write-Host "✓ Docker daemon is running" -ForegroundColor Green
} catch {
    Write-Host "✗ Docker daemon is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Starting Docker services..." -ForegroundColor Yellow
Write-Host ""

# Start services
docker-compose up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Failed to start Docker services" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Waiting for services to become healthy..." -ForegroundColor Yellow
Write-Host ""

# Wait for services to be healthy
$maxAttempts = 30
$attempt = 0
$allHealthy = $false

while (-not $allHealthy -and $attempt -lt $maxAttempts) {
    $attempt++
    Start-Sleep -Seconds 2
    
    $services = docker-compose ps --format json | ConvertFrom-Json
    $unhealthy = $services | Where-Object { $_.Health -ne "healthy" -and $_.State -eq "running" }
    
    if ($unhealthy.Count -eq 0) {
        $allHealthy = $true
    } else {
        Write-Host "  Waiting... ($attempt/$maxAttempts) - Unhealthy: $($unhealthy.Name -join ', ')" -ForegroundColor Gray
    }
}

Write-Host ""

if ($allHealthy) {
    Write-Host "✓ All services are healthy!" -ForegroundColor Green
} else {
    Write-Host "⚠ Services started but some may not be fully healthy yet" -ForegroundColor Yellow
    Write-Host "  Run 'docker-compose ps' to check status" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Service Status:" -ForegroundColor Cyan
docker-compose ps

Write-Host ""
Write-Host "Services are ready! 🎉" -ForegroundColor Green
Write-Host ""
Write-Host "Access points:" -ForegroundColor Cyan
Write-Host "  PostgreSQL:       localhost:5432" -ForegroundColor White
Write-Host "  RabbitMQ AMQP:    localhost:5672" -ForegroundColor White
Write-Host "  RabbitMQ UI:      http://localhost:15672 (guest/guest)" -ForegroundColor White
Write-Host "  Redis:            localhost:6379" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Run migrations:  cd src/HookVerse.Api; dotnet ef database update" -ForegroundColor White
Write-Host "  2. Start API:       cd src/HookVerse.Api; dotnet run" -ForegroundColor White
Write-Host "  3. Start Worker:    cd src/HookVerse.Worker; dotnet run" -ForegroundColor White
Write-Host "  4. Start Dashboard: cd src/HookVerse.Dashboard; dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "To stop services:   docker-compose down" -ForegroundColor Yellow
Write-Host "For help:           See DOCKER.md" -ForegroundColor Yellow
Write-Host ""
