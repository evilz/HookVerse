using HookVerse.Core.Entities;
using HookVerse.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HookVerse.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for SchemaDefinition entity.
/// </summary>
public class SchemaDefinitionConfiguration : IEntityTypeConfiguration<SchemaDefinition>
{
    public void Configure(EntityTypeBuilder<SchemaDefinition> builder)
    {
        builder.ToTable("SchemaDefinitions", "hookverse");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventTypeId)
            .IsRequired();

        builder.Property(e => e.Format)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Content)
            .IsRequired();

        builder.Property(e => e.ContentHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Unique index on EventTypeId (1:1 relationship)
        builder.HasIndex(e => e.EventTypeId)
            .IsUnique()
            .HasDatabaseName("IX_SchemaDefinitions_EventTypeId");

        builder.HasIndex(e => e.ContentHash)
            .HasDatabaseName("IX_SchemaDefinitions_ContentHash");

        // Relationship
        builder.HasOne(e => e.EventType)
            .WithOne(et => et.SchemaDefinition)
            .HasForeignKey<SchemaDefinition>(e => e.EventTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
