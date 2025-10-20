using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using HookVerse.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace HookVerse.Integration.Tests;

/// <summary>
/// Base class for integration tests using TestContainers for PostgreSQL and RabbitMQ.
/// Provides a clean database and message broker for each test class.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private RabbitMqContainer? _rabbitMqContainer;
    protected WebApplicationFactory<Program>? _factory;
    protected HttpClient? _client;

    /// <summary>
    /// Gets the connection string for the test database.
    /// </summary>
    protected string ConnectionString => _postgresContainer?.GetConnectionString() ?? throw new InvalidOperationException("PostgreSQL container not initialized");

    /// <summary>
    /// Gets the RabbitMQ connection string.
    /// </summary>
    protected string RabbitMqConnectionString => $"amqp://guest:guest@{_rabbitMqContainer?.Hostname}:{_rabbitMqContainer?.GetMappedPublicPort(5672)}";

    /// <summary>
    /// Initialize test containers and web application factory.
    /// Called automatically by xUnit before each test class.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("hookverse_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _postgresContainer.StartAsync();

        // Start RabbitMQ container
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.13-alpine")
            .WithUsername("guest")
            .WithPassword("guest")
            .WithCleanUp(true)
            .Build();

        await _rabbitMqContainer.StartAsync();

        // Create web application factory with test containers
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    services.RemoveAll<DbContextOptions<HookVerseDbContext>>();
                    services.RemoveAll<HookVerseDbContext>();

                    // Add test database context
                    services.AddDbContext<HookVerseDbContext>(options =>
                    {
                        options.UseNpgsql(ConnectionString);
                    });

                    // Override message bus configuration to use test container
                    services.Configure<MessageBusOptions>(options =>
                    {
                        options.Transport = "RabbitMQ";
                        options.Host = _rabbitMqContainer.Hostname;
                        options.Port = _rabbitMqContainer.GetMappedPublicPort(5672);
                        options.Username = "guest";
                        options.Password = "guest";
                    });

                    // Build service provider and run migrations
                    var serviceProvider = services.BuildServiceProvider();
                    using var scope = serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<HookVerseDbContext>();
                    dbContext.Database.Migrate();
                });
            });

        _client = _factory.CreateClient();
    }

    /// <summary>
    /// Clean up test containers and factory.
    /// Called automatically by xUnit after each test class.
    /// </summary>
    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();

        if (_postgresContainer != null)
        {
            await _postgresContainer.StopAsync();
            await _postgresContainer.DisposeAsync();
        }

        if (_rabbitMqContainer != null)
        {
            await _rabbitMqContainer.StopAsync();
            await _rabbitMqContainer.DisposeAsync();
        }
    }

    /// <summary>
    /// Returns the HttpClient configured for the test application.
    /// </summary>
    protected HttpClient CreateClient()
    {
        if (_client == null)
            throw new InvalidOperationException("Test client not initialized. Make sure InitializeAsync() was called.");

        return _client;
    }

    /// <summary>
    /// Gets a scoped service from the test application.
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        if (_factory is null)
            throw new InvalidOperationException("Factory not initialized");

        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets the database context for direct data manipulation in tests.
    /// </summary>
    protected HookVerseDbContext GetDbContext()
    {
        return GetService<HookVerseDbContext>();
    }

    /// <summary>
    /// Seeds test data into the database.
    /// </summary>
    protected async Task SeedTestDataAsync<T>(params T[] entities) where T : class
    {
        var dbContext = GetDbContext();
        await dbContext.Set<T>().AddRangeAsync(entities);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Clears all data from a specific table.
    /// </summary>
    protected async Task ClearTableAsync<T>() where T : class
    {
        var dbContext = GetDbContext();
        dbContext.Set<T>().RemoveRange(dbContext.Set<T>());
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Executes an action within a database transaction that is rolled back.
    /// Useful for testing without affecting test database state.
    /// </summary>
    protected async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<HookVerseDbContext, Task<TResult>> action)
    {
        var dbContext = GetDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var result = await action(dbContext);
            await transaction.RollbackAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

/// <summary>
/// Options for configuring the message bus in tests.
/// </summary>
public class MessageBusOptions
{
    public string Transport { get; set; } = "RabbitMQ";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}
