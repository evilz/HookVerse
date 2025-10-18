using HookVerse.Core.Entities;
using HookVerse.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HookVerse.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Subscription entity.
/// </summary>
public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions", "hookverse");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SubscriberId)
            .IsRequired();

        builder.Property(e => e.EventTypeId)
            .IsRequired();

        builder.Property(e => e.EndpointUrl)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.Secret)
            .IsRequired()
            .HasMaxLength(256);
        // TODO: Implement Secret encryption at rest

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.FilterExpression)
            .HasMaxLength(500);

        builder.Property(e => e.CustomHeaders);
        // TODO: Implement CustomHeaders encryption at rest

        builder.Property(e => e.AuthType)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(AuthType.None);

        builder.Property(e => e.AuthConfig);
        // TODO: Implement AuthConfig encryption at rest

        builder.Property(e => e.LastDeliveryAt);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.SubscriberId)
            .HasDatabaseName("IX_Subscriptions_SubscriberId");

        builder.HasIndex(e => e.EventTypeId)
            .HasDatabaseName("IX_Subscriptions_EventTypeId");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Subscriptions_IsActive");

        builder.HasIndex(e => new { e.EventTypeId, e.IsActive })
            .HasDatabaseName("IX_Subscriptions_EventTypeId_IsActive");

        // Relationships
        builder.HasOne(e => e.Subscriber)
            .WithMany(s => s.Subscriptions)
            .HasForeignKey(e => e.SubscriberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.EventType)
            .WithMany(et => et.Subscriptions)
            .HasForeignKey(e => e.EventTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.DeliveryAttempts)
            .WithOne(d => d.Subscription)
            .HasForeignKey(d => d.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
