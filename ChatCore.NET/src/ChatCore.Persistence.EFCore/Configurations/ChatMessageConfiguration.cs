namespace ChatCore.Persistence.EFCore.Configurations;

using ChatCore.Abstractions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity Framework configuration for ChatMessage.
/// </summary>
public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(m => new { m.ConversationId, m.SequenceNumber });

        builder.Property(m => m.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(m => m.ConversationId)
            .IsRequired();

        builder.Property(m => m.SenderId)
            .IsRequired();

        builder.Property(m => m.TenantId)
            .IsRequired();

        builder.Property(m => m.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(m => m.SequenceNumber)
            .IsRequired();

        builder.Property(m => m.SentAt)
            .IsRequired();

        builder.Property(m => m.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.IdempotencyKey)
            .HasMaxLength(100);

        // Critical index for seek-based pagination
        builder.HasIndex(m => new { m.TenantId, m.ConversationId, m.SequenceNumber })
            .HasDatabaseName("IX_Messages_TenantId_ConversationId_SequenceNumber");

        // Index for idempotency key lookup
        builder.HasIndex(m => new { m.TenantId, m.IdempotencyKey })
            .HasDatabaseName("IX_Messages_TenantId_IdempotencyKey")
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(m => new { m.ConversationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}