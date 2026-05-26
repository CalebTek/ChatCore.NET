namespace ChatCore.Abstractions.DTOs;

using ChatCore.Abstractions.Domain.Enums;

/// <summary>
/// Data transfer object for chat messages.
/// </summary>
public class ChatMessageDto
{
    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the conversation identifier.
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the sender user identifier.
    /// </summary>
    public Guid SenderId { get; set; }

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sequence number.
    /// </summary>
    public long SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the send timestamp.
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the message is deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the message status.
    /// </summary>
    public MessageStatus Status { get; set; }
}