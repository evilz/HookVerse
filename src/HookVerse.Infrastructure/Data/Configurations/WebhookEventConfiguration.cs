using HookVerse.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HookVerse.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for WebhookEvent entity.
/// </summary>
public class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("WebhookEvents", "hookverse");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventTypeId)
            .IsRequired();

        builder.Property(e => e.SubscriberId)
            .IsRequired();

        builder.Property(e => e.Payload)
            .IsRequired();
        // TODO: Implement payload encryption at rest using AES-256-GCM

        builder.Property(e => e.PayloadHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.PayloadSizeBytes)
            .IsRequired();

        builder.Property(e => e.TraceId)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.ScheduledFor);

        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        builder.Property(e => e.Metadata);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.EventTypeId)
            .HasDatabaseName("IX_WebhookEvents_EventTypeId");

        builder.HasIndex(e => e.SubscriberId)
            .HasDatabaseName("IX_WebhookEvents_SubscriberId");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_WebhookEvents_CreatedAt");

        builder.HasIndex(e => e.ScheduledFor)
            .HasDatabaseName("IX_WebhookEvents_ScheduledFor");

        builder.HasIndex(e => new { e.SubscriberId, e.CreatedAt })
            .HasDatabaseName("IX_WebhookEvents_SubscriberId_CreatedAt");

        // Relationships
        builder.HasOne(e => e.EventType)
            .WithMany(et => et.WebhookEvents)
            .HasForeignKey(e => e.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Subscriber)
            .WithMany(s => s.WebhookEvents)
            .HasForeignKey(e => e.SubscriberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.DeliveryAttempts)
            .WithOne(d => d.WebhookEvent)
            .HasForeignKey(d => d.WebhookEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
