using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using HookVerse.Infrastructure.Data;
// Testcontainers types are optional for local runs; add using if package restored
using Microsoft.Data.Sqlite;

namespace HookVerse.Integration.Tests;

/// <summary>
/// Base class for integration tests using TestContainers for PostgreSQL and RabbitMQ.
/// Provides a clean database and message broker for each test class.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private SqliteConnection? _connection;
    protected WebApplicationFactory<Program>? _factory;
    protected HttpClient? _client;

    // IntegrationTestBase uses an in-memory SQLite connection for fast tests.

    /// <summary>
    /// Initialize test containers and web application factory.
    /// Called automatically by xUnit before each test class.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        // Use an in-memory SQLite connection to run tests quickly without Docker
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    services.RemoveAll<DbContextOptions<HookVerseDbContext>>();
                    services.RemoveAll<HookVerseDbContext>();

                    // Add SQLite in-memory context
                    services.AddDbContext<HookVerseDbContext>(options =>
                    {
                        options.UseSqlite(_connection);
                    });

                    // Build service provider and run migrations
                    var serviceProvider = services.BuildServiceProvider();
                    using var scope = serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<HookVerseDbContext>();
                    dbContext.Database.EnsureCreated();
                });
            });

        _client = _factory.CreateClient();
    }

    /// <summary>
    /// Clean up test containers and factory.
    /// Called automatically by xUnit after each test class.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();

        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
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
