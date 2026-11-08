using HookVerse.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HookVerse.Integration.Tests;

public class SqliteCompatibilityTests
{
    [Fact]
    public async Task SqliteProvider_CreatesSchemaAndExecutesQuery()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        var cancellationToken = TestContext.Current.CancellationToken;
        await connection.OpenAsync(cancellationToken);
        var options = new DbContextOptionsBuilder<HookVerseDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new HookVerseDbContext(options);

        await context.Database.EnsureCreatedAsync(cancellationToken);
        Assert.Equal(0, await context.Subscribers.CountAsync(cancellationToken));
    }
}
