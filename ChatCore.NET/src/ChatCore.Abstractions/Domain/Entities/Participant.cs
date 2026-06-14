namespace ChatCore.Abstractions.Domain.Entities;

/// <summary>
/// Represents a participant in a conversation.
/// </summary>
public class Participant
{
    /// <summary>
    /// Gets the conversation identifier.
    /// </summary>
    public Guid ConversationId { get; private set; }

    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the tenant identifier.
    /// Required so the foreign key to <see cref="Conversation"/> can reference
    /// the composite primary key (Id, TenantId) and enforce tenant isolation.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the timestamp when the user joined.
    /// </summary>
    public DateTime JoinedAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Participant"/> class.
    /// </summary>
    public Participant(Guid conversationId, Guid userId, Guid tenantId, DateTime joinedAt)
    {
        ConversationId = conversationId;
        UserId         = userId;
        TenantId       = tenantId;
        JoinedAt       = joinedAt;
    }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
#pragma warning disable CS8618
    protected Participant()
    {
    }
#pragma warning restore CS8618
}
