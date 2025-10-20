using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HookVerse.Core.Entities;

namespace HookVerse.Infrastructure.Data.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.HashedKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.KeyPrefix)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.IsActive)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        // Index on TenantId for faster lookups
        builder.HasIndex(a => a.TenantId);

        // Index on HashedKey for authentication
        builder.HasIndex(a => a.HashedKey)
            .IsUnique();

        // Ignore navigation property since Tenant table doesn't exist
        // In this system, TenantId maps to Subscriber.Id
        builder.Ignore(a => a.Tenant);
    }
}
