namespace ChatCore.Persistence.EFCore.Configurations;

using ChatCore.Abstractions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity Framework configuration for Participant.
/// </summary>
public class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.HasKey(p => new { p.ConversationId, p.UserId });

        builder.Property(p => p.ConversationId).IsRequired();
        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.JoinedAt).IsRequired();

        builder.HasIndex(p => p.UserId);

        // Composite FK — matches Conversation's composite PK (Id, TenantId)
        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(p => new { p.ConversationId, p.TenantId })
            .HasPrincipalKey(c => new { c.Id, c.TenantId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
