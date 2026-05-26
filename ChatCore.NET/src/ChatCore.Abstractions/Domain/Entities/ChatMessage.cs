namespace ChatCore.Abstractions.Domain.Entities;

/// <summary>
/// Represents a chat message.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Gets the message identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the conversation identifier.
    /// </summary>
    public Guid ConversationId { get; private set; }

    /// <summary>
    /// Gets the user identifier of the sender.
    /// </summary>
    public Guid SenderId { get; private set; }

    /// <summary>
    /// Gets the tenant identifier for multi-tenancy support.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the message content.
    /// </summary>
    public string Content { get; private set; }

    /// <summary>
    /// Gets the sequence number for ordering guarantee.
    /// </summary>
    public long SequenceNumber { get; private set; }

    /// <summary>
    /// Gets the timestamp when the message was sent.
    /// </summary>
    public DateTime SentAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the message is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Gets the idempotency key to prevent duplicate sends.
    /// </summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessage"/> class.
    /// </summary>
    /// <param name="id">The message identifier.</param>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="senderId">The sender user identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="content">The message content.</param>
    /// <param name="sequenceNumber">The sequence number.</param>
    /// <param name="sentAt">The send timestamp.</param>
    /// <param name="idempotencyKey">The idempotency key.</param>
    public ChatMessage(
        Guid id,
        Guid conversationId,
        Guid senderId,
        Guid tenantId,
        string content,
        long sequenceNumber,
        DateTime sentAt,
        string? idempotencyKey = null)
    {
        Id = id;
        ConversationId = conversationId;
        SenderId = senderId;
        TenantId = tenantId;
        Content = content;
        SequenceNumber = sequenceNumber;
        SentAt = sentAt;
        IsDeleted = false;
        IdempotencyKey = idempotencyKey;
    }

    /// <summary>
    /// Soft deletes the message.
    /// </summary>
    public void SoftDelete()
    {
        IsDeleted = true;
        Content = "[deleted]";
    }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
#pragma warning disable CS8618
    protected ChatMessage()
    {
    }
#pragma warning restore CS8618
}