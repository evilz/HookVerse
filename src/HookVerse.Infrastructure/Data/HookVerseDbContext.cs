using Microsoft.EntityFrameworkCore;
using HookVerse.Core.Entities;

namespace HookVerse.Infrastructure.Data;

public class HookVerseDbContext : DbContext
{
    public HookVerseDbContext(DbContextOptions<HookVerseDbContext> options)
        : base(options)
    {
    }

    // DbSets will be added as entities are created
    // public DbSet<Webhook> Webhooks => Set<Webhook>();
    // public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    // public DbSet<Subscription> Subscriptions => Set<Subscription>();
    // public DbSet<Tenant> Tenants => Set<Tenant>();

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
