using Microsoft.EntityFrameworkCore;
using HookVerse.Core.Entities;

namespace HookVerse.Infrastructure.Data;

public class HookVerseDbContext : DbContext
{
    public HookVerseDbContext(DbContextOptions<HookVerseDbContext> options)
        : base(options)
    {
    }

    // DbSets for User Story 1 entities
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<SchemaDefinition> SchemaDefinitions => Set<SchemaDefinition>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<DeliveryAttempt> DeliveryAttempts => Set<DeliveryAttempt>();

    // DbSets for User Story 4 - Mock Endpoints
    public DbSet<MockEndpoint> MockEndpoints => Set<MockEndpoint>();
    public DbSet<MockEndpointRequest> MockEndpointRequests => Set<MockEndpointRequest>();

    // DbSets for User Story 7 - GDPR Compliance
    public DbSet<GdprRequest> GdprRequests => Set<GdprRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HookVerseDbContext).Assembly);

        // Configure schema
        modelBuilder.HasDefaultSchema("hookverse");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Add audit fields before saving
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            if (entry.Entity is IEntity entity)
            {
                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
