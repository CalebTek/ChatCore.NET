namespace ChatCore.Abstractions.Requests;

/// <summary>
/// Request to send a chat message.
/// </summary>
public class ChatMessageRequest
{
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
    /// Gets or sets the idempotency key to prevent duplicate sends.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }
}