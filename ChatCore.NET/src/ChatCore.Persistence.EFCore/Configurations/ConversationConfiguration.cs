namespace ChatCore.Persistence.EFCore.Configurations;

using ChatCore.Abstractions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity Framework configuration for Conversation.
/// </summary>
public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(c => new { c.Id, c.TenantId });

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.Type)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.LastSequenceNumber)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.RowVersion)
            .IsRowVersion();

        builder.HasIndex(c => new { c.TenantId, c.CreatedAt });
    }
}