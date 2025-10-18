using HookVerse.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HookVerse.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for EventType entity.
/// </summary>
public class EventTypeConfiguration : IEntityTypeConfiguration<EventType>
{
    public void Configure(EntityTypeBuilder<EventType> builder)
    {
        builder.ToTable("EventTypes", "hookverse");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.Version)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.SubscriberId)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Unique composite index on Name + Version + SubscriberId
        builder.HasIndex(e => new { e.Name, e.Version, e.SubscriberId })
            .IsUnique()
            .HasDatabaseName("IX_EventTypes_Name_Version_SubscriberId");

        builder.HasIndex(e => e.SubscriberId)
            .HasDatabaseName("IX_EventTypes_SubscriberId");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_EventTypes_IsActive");

        // Relationships
        builder.HasOne(e => e.Subscriber)
            .WithMany(s => s.EventTypes)
            .HasForeignKey(e => e.SubscriberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.SchemaDefinition)
            .WithOne(s => s.EventType)
            .HasForeignKey<SchemaDefinition>(s => s.EventTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.WebhookEvents)
            .WithOne(w => w.EventType)
            .HasForeignKey(w => w.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Subscriptions)
            .WithOne(s => s.EventType)
            .HasForeignKey(s => s.EventTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
