using HookVerse.Core.Entities;
using HookVerse.Core.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HookVerse.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for GdprRequest entity.
/// </summary>
public class GdprRequestConfiguration : IEntityTypeConfiguration<GdprRequest>
{
    public void Configure(EntityTypeBuilder<GdprRequest> builder)
    {
        builder.ToTable("GdprRequests");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .ValueGeneratedNever();

        builder.Property(g => g.RequestType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(g => g.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(g => g.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(g => g.ExportFilePath)
            .HasMaxLength(500);

        builder.Property(g => g.Metadata)
            .HasColumnType("jsonb");

        builder.Property(g => g.CreatedAt)
            .IsRequired();

        builder.Property(g => g.UpdatedAt);

        builder.Property(g => g.CompletedAt);

        // Indexes
        builder.HasIndex(g => g.SubscriberId)
            .HasDatabaseName("IX_GdprRequests_SubscriberId");

        builder.HasIndex(g => g.Status)
            .HasDatabaseName("IX_GdprRequests_Status");

        builder.HasIndex(g => g.RequestType)
            .HasDatabaseName("IX_GdprRequests_RequestType");

        builder.HasIndex(g => g.CreatedAt)
            .HasDatabaseName("IX_GdprRequests_CreatedAt");

        // Relationships
        builder.HasOne(g => g.Subscriber)
            .WithMany()
            .HasForeignKey(g => g.SubscriberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
