using HookVerse.Core.Entities;
using HookVerse.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HookVerse.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for DeliveryAttempt entity.
/// </summary>
public class DeliveryAttemptConfiguration : IEntityTypeConfiguration<DeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<DeliveryAttempt> builder)
    {
        builder.ToTable("DeliveryAttempts", "hookverse");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.WebhookEventId)
            .IsRequired();

        builder.Property(e => e.SubscriptionId)
            .IsRequired();

        builder.Property(e => e.AttemptNumber)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(DeliveryStatus.Pending);

        builder.Property(e => e.StartedAt)
            .IsRequired();

        builder.Property(e => e.CompletedAt);

        builder.Property(e => e.DurationMs);

        builder.Property(e => e.RequestHeaders);

        builder.Property(e => e.RequestBody);

        builder.Property(e => e.ResponseStatus);

        builder.Property(e => e.ResponseHeaders);

        builder.Property(e => e.ResponseBody)
            .HasMaxLength(10000);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(e => e.Signature)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.TraceId)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.NextRetryAt);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.WebhookEventId)
            .HasDatabaseName("IX_DeliveryAttempts_WebhookEventId");

        builder.HasIndex(e => e.SubscriptionId)
            .HasDatabaseName("IX_DeliveryAttempts_SubscriptionId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_DeliveryAttempts_Status");

        builder.HasIndex(e => e.StartedAt)
            .HasDatabaseName("IX_DeliveryAttempts_StartedAt");

        builder.HasIndex(e => new { e.WebhookEventId, e.AttemptNumber })
            .HasDatabaseName("IX_DeliveryAttempts_WebhookEventId_AttemptNumber");

        builder.HasIndex(e => new { e.Status, e.NextRetryAt })
            .HasDatabaseName("IX_DeliveryAttempts_Status_NextRetryAt");

        // Relationships
        builder.HasOne(e => e.WebhookEvent)
            .WithMany(w => w.DeliveryAttempts)
            .HasForeignKey(e => e.WebhookEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Subscription)
            .WithMany(s => s.DeliveryAttempts)
            .HasForeignKey(e => e.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
