using HookVerse.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HookVerse.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for MockEndpoint entity.
/// </summary>
public class MockEndpointConfiguration : IEntityTypeConfiguration<MockEndpoint>
{
    public void Configure(EntityTypeBuilder<MockEndpoint> builder)
    {
        builder.ToTable("MockEndpoints");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SubscriberId)
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.UrlPath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.ResponseStatus)
            .IsRequired();

        builder.Property(e => e.ResponseBody)
            .HasColumnType("text");

        builder.Property(e => e.ResponseContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ResponseDelayMs)
            .IsRequired();

        builder.Property(e => e.ResponseHeaders)
            .HasColumnType("jsonb");

        builder.Property(e => e.IsActive)
            .IsRequired();

        builder.Property(e => e.RequestCount)
            .IsRequired();

        builder.Property(e => e.LastRequestAt);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.SubscriberId)
            .HasDatabaseName("IX_MockEndpoints_SubscriberId");

        builder.HasIndex(e => e.UrlPath)
            .IsUnique()
            .HasDatabaseName("IX_MockEndpoints_UrlPath");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_MockEndpoints_IsActive");

        // Relationships
        builder.HasOne(e => e.Subscriber)
            .WithMany()
            .HasForeignKey(e => e.SubscriberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Requests)
            .WithOne(r => r.MockEndpoint)
            .HasForeignKey(r => r.MockEndpointId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
