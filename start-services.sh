#!/bin/bash
# HookVerse Quick Start Script
# Starts Docker services and verifies they're healthy

set -e

echo -e "\033[36m🚀 HookVerse Quick Start\033[0m"
echo -e "\033[36m========================\033[0m"
echo ""

# Check if Docker is installed
echo -e "\033[33mChecking Docker installation...\033[0m"
if command -v docker &> /dev/null; then
    DOCKER_VERSION=$(docker --version)
    echo -e "\033[32m✓ Docker found: $DOCKER_VERSION\033[0m"
else
    echo -e "\033[31m✗ Docker not found. Please install Docker.\033[0m"
    echo -e "\033[31m  Download from: https://www.docker.com/products/docker-desktop\033[0m"
    exit 1
fi

# Check if Docker daemon is running
echo -e "\033[33mChecking Docker daemon...\033[0m"
if docker ps &> /dev/null; then
    echo -e "\033[32m✓ Docker daemon is running\033[0m"
else
    echo -e "\033[31m✗ Docker daemon is not running. Please start Docker.\033[0m"
    exit 1
fi

echo ""
echo -e "\033[33mStarting Docker services...\033[0m"
echo ""

# Start services
docker-compose up -d

echo ""
echo -e "\033[33mWaiting for services to become healthy...\033[0m"
echo ""

# Wait for services to be healthy
MAX_ATTEMPTS=30
ATTEMPT=0
ALL_HEALTHY=false

while [ "$ALL_HEALTHY" = false ] && [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
    ATTEMPT=$((ATTEMPT + 1))
    sleep 2
    
    # Check health status
    UNHEALTHY=$(docker-compose ps --format json | jq -r 'select(.Health != "healthy" and .State == "running") | .Name' | wc -l)
    
    if [ "$UNHEALTHY" -eq 0 ]; then
        ALL_HEALTHY=true
    else
        echo -e "  Waiting... ($ATTEMPT/$MAX_ATTEMPTS) - Some services not yet healthy"
    fi
done

echo ""

if [ "$ALL_HEALTHY" = true ]; then
    echo -e "\033[32m✓ All services are healthy!\033[0m"
else
    echo -e "\033[33m⚠ Services started but some may not be fully healthy yet\033[0m"
    echo -e "\033[33m  Run 'docker-compose ps' to check status\033[0m"
fi

echo ""
echo -e "\033[36mService Status:\033[0m"
docker-compose ps

echo ""
echo -e "\033[32mServices are ready! 🎉\033[0m"
echo ""
echo -e "\033[36mAccess points:\033[0m"
echo -e "  PostgreSQL:       localhost:5432"
echo -e "  RabbitMQ AMQP:    localhost:5672"
echo -e "  RabbitMQ UI:      http://localhost:15672 (guest/guest)"
echo -e "  Redis:            localhost:6379"
echo ""
echo -e "\033[36mNext steps:\033[0m"
echo -e "  1. Run migrations:  cd src/HookVerse.Api && dotnet ef database update"
echo -e "  2. Start API:       cd src/HookVerse.Api && dotnet run"
echo -e "  3. Start Worker:    cd src/HookVerse.Worker && dotnet run"
echo -e "  4. Start Dashboard: cd src/HookVerse.Dashboard && dotnet run"
echo ""
echo -e "\033[33mTo stop services:   docker-compose down\033[0m"
echo -e "\033[33mFor help:           See DOCKER.md\033[0m"
echo ""
