using HookVerse.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HookVerse.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for MockEndpointRequest entity.
/// </summary>
public class MockEndpointRequestConfiguration : IEntityTypeConfiguration<MockEndpointRequest>
{
    public void Configure(EntityTypeBuilder<MockEndpointRequest> builder)
    {
        builder.ToTable("MockEndpointRequests");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.MockEndpointId)
            .IsRequired();

        builder.Property(e => e.Method)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.Path)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.QueryString)
            .HasMaxLength(2000);

        builder.Property(e => e.Headers)
            .HasColumnType("jsonb");

        builder.Property(e => e.Body)
            .HasColumnType("text");

        builder.Property(e => e.ContentType)
            .HasMaxLength(100);

        builder.Property(e => e.ClientIp)
            .HasMaxLength(50);

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);

        builder.Property(e => e.ResponseStatus)
            .IsRequired();

        builder.Property(e => e.ResponseBody)
            .HasColumnType("text");

        builder.Property(e => e.ReceivedAt)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.MockEndpointId)
            .HasDatabaseName("IX_MockEndpointRequests_MockEndpointId");

        builder.HasIndex(e => e.ReceivedAt)
            .HasDatabaseName("IX_MockEndpointRequests_ReceivedAt");

        // Relationships
        builder.HasOne(e => e.MockEndpoint)
            .WithMany(m => m.Requests)
            .HasForeignKey(e => e.MockEndpointId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
