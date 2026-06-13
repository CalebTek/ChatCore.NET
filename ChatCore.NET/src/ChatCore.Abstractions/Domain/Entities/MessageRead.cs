namespace ChatCore.Abstractions.Domain.Entities;

/// <summary>
/// Represents a read receipt for a message.
/// </summary>
public class MessageRead
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
    /// Required to enforce the composite foreign key to <see cref="Conversation"/> (Id, TenantId),
    /// which prevents cross-tenant read receipts from referencing another tenant's conversation.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the last read sequence number.
    /// </summary>
    public long LastReadSequence { get; private set; }

    /// <summary>
    /// Gets the timestamp of the read receipt.
    /// </summary>
    public DateTime ReadAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageRead"/> class.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="lastReadSequence">The last read sequence number.</param>
    /// <param name="readAt">The read timestamp.</param>
    public MessageRead(Guid conversationId, Guid userId, Guid tenantId, long lastReadSequence, DateTime readAt)
    {
        ConversationId  = conversationId;
        UserId          = userId;
        TenantId        = tenantId;
        LastReadSequence = lastReadSequence;
        ReadAt          = readAt;
    }

    /// <summary>
    /// Updates the read receipt.
    /// </summary>
    /// <param name="lastReadSequence">The new last read sequence number.</param>
    /// <param name="readAt">The new read timestamp.</param>
    public void Update(long lastReadSequence, DateTime readAt)
    {
        LastReadSequence = lastReadSequence;
        ReadAt          = readAt;
    }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
#pragma warning disable CS8618
    protected MessageRead()
    {
    }
#pragma warning restore CS8618
}
