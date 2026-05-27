namespace ChatCore.Persistence.EFCore.Configurations;

using ChatCore.Abstractions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity Framework configuration for MessageRead.
/// </summary>
public class MessageReadConfiguration : IEntityTypeConfiguration<MessageRead>
{
    public void Configure(EntityTypeBuilder<MessageRead> builder)
    {
        builder.HasKey(r => new { r.ConversationId, r.UserId });

        builder.Property(r => r.ConversationId)
            .IsRequired();

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.LastReadSequence)
            .IsRequired();

        builder.Property(r => r.ReadAt)
            .IsRequired();

        builder.HasIndex(r => r.UserId);

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(r => new { r.ConversationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}