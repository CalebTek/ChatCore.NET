namespace ChatCore.Persistence.EFCore.Configurations;

using ChatCore.Abstractions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity Framework configuration for UserConnection.
/// </summary>
public class UserConnectionConfiguration : IEntityTypeConfiguration<UserConnection>
{
    public void Configure(EntityTypeBuilder<UserConnection> builder)
    {
        builder.HasKey(c => new { c.UserId, c.ConnectionId });

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.ConnectionId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.ConnectedAt)
            .IsRequired();

        builder.HasIndex(c => c.ConnectionId);
    }
}