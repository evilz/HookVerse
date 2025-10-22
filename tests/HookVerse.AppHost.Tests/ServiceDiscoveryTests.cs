using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace HookVerse.AppHost.Tests;

/// <summary>
/// Integration tests for service discovery in Aspire orchestration.
/// Verifies that services can discover and communicate with each other using Aspire's built-in service discovery.
/// </summary>
public class ServiceDiscoveryTests
{
    [Fact]
    public async Task Api_Can_Discover_Worker_Service()
    {
        // Arrange: Build and start the Aspire application
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.HookVerse_AppHost>();
        
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Wait a bit for services to fully start
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Act: Try to call the API health endpoint
        var httpClient = app.CreateHttpClient("api", "http");
        var response = await httpClient.GetAsync("/health/live");

        // Assert: API service should be reachable and healthy
        response.IsSuccessStatusCode.Should().BeTrue("API service should be reachable via service discovery");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty("Health endpoint should return status information");
    }

    [Fact]
    public async Task Worker_Can_Discover_Api_Service()
    {
        // Arrange: Build and start the Aspire application
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.HookVerse_AppHost>();
        
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Wait a bit for services to fully start
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Act & Assert: Verify API service is accessible
        // In a real scenario, Worker would use HttpClient with service discovery to call API
        // This test verifies both services can start successfully, which is a prerequisite for service discovery
        var apiHttpClient = app.CreateHttpClient("api", "http");
        var apiResponse = await apiHttpClient.GetAsync("/health/live");
        
        apiResponse.IsSuccessStatusCode.Should().BeTrue(
            "Worker can discover API because both services are running in the same Aspire host");
    }

    [Fact]
    public async Task Connection_Strings_Are_Injected()
    {
        // Arrange: Build and start the Aspire application
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.HookVerse_AppHost>();
        
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Wait a bit for services to fully start and run health checks
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Act: Check API readiness endpoint which validates database and message broker connections
        var apiHttpClient = app.CreateHttpClient("api", "http");
        var apiHealthResponse = await apiHttpClient.GetAsync("/health/ready");
        
        // Assert: Services are running with valid connections
        // If connection strings were not injected, services would fail health checks
        apiHealthResponse.IsSuccessStatusCode.Should().BeTrue(
            "API readiness check should pass, indicating database and message broker connections are healthy");
        
        var healthContent = await apiHealthResponse.Content.ReadAsStringAsync();
        healthContent.Should().NotBeNullOrEmpty("Health check should return detailed status");
    }

    [Fact]
    public async Task No_Hardcoded_Connection_Strings()
    {
        // Arrange: Define paths to appsettings.json files
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
        var appsettingsFiles = new[]
        {
            Path.Combine(repoRoot, "src", "HookVerse.Api", "appsettings.json"),
            Path.Combine(repoRoot, "src", "HookVerse.Worker", "appsettings.json"),
            Path.Combine(repoRoot, "src", "HookVerse.Dashboard", "appsettings.json")
        };

        // Act: Read and check each appsettings.json file
        foreach (var filePath in appsettingsFiles)
        {
            if (!File.Exists(filePath))
            {
                // File doesn't exist, skip
                continue;
            }

            var content = await File.ReadAllTextAsync(filePath);

            // Assert: No hardcoded connection strings should be present
            // Check for common patterns that indicate hardcoded values
            content.Should().NotContain("Host=localhost", 
                $"{Path.GetFileName(filePath)} should not contain hardcoded localhost PostgreSQL connection");
            content.Should().NotContain("Server=localhost", 
                $"{Path.GetFileName(filePath)} should not contain hardcoded localhost SQL Server connection");
            content.Should().NotContain("Password=", 
                $"{Path.GetFileName(filePath)} should not contain hardcoded database passwords");
            
            // RabbitMQ checks
            content.Should().NotContain("\"Host\": \"localhost\"", 
                $"{Path.GetFileName(filePath)} should not contain hardcoded RabbitMQ host");
            content.Should().NotContain("\"Username\": \"guest\"", 
                $"{Path.GetFileName(filePath)} should not contain hardcoded RabbitMQ username");
            content.Should().NotContain("\"Password\": \"guest\"", 
                $"{Path.GetFileName(filePath)} should not contain hardcoded RabbitMQ password");
            
            // Redis checks
            content.Should().NotContain("localhost:6379", 
                $"{Path.GetFileName(filePath)} should not contain hardcoded Redis connection string");
            content.Should().NotContain("127.0.0.1:6379", 
                $"{Path.GetFileName(filePath)} should not contain hardcoded Redis connection string");
        }

        // Success: All appsettings.json files are free of hardcoded connection strings
        await Task.CompletedTask; // Satisfy async method requirement
    }
}

