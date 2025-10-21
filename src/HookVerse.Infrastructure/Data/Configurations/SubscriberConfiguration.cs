using HookVerse.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HookVerse.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Subscriber entity.
/// </summary>
public class SubscriberConfiguration : IEntityTypeConfiguration<Subscriber>
{
    public void Configure(EntityTypeBuilder<Subscriber> builder)
    {
        builder.ToTable("Subscribers", "hookverse");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(e => e.ApiKeyHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.RetentionDays)
            .IsRequired()
            .HasDefaultValue(90);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Unique indexes
        builder.HasIndex(e => e.Email)
            .IsUnique()
            .HasDatabaseName("IX_Subscribers_Email");

        builder.HasIndex(e => e.ApiKeyHash)
            .IsUnique()
            .HasDatabaseName("IX_Subscribers_ApiKeyHash");

        // Relationships
        builder.HasMany(e => e.EventTypes)
            .WithOne(e => e.Subscriber)
            .HasForeignKey(e => e.SubscriberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.WebhookEvents)
            .WithOne(e => e.Subscriber)
            .HasForeignKey(e => e.SubscriberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Subscriptions)
            .WithOne(e => e.Subscriber)
            .HasForeignKey(e => e.SubscriberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
