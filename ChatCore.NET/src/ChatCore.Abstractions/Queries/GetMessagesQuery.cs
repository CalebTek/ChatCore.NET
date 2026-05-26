namespace ChatCore.Abstractions.Queries;

/// <summary>
/// Query to retrieve messages from a conversation.
/// </summary>
public class GetMessagesQuery
{
    /// <summary>
    /// Gets or sets the conversation identifier.
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the user identifier requesting the messages.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the last seen sequence number for seek-based pagination.
    /// </summary>
    public long LastSeenSequence { get; set; } = long.MaxValue;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 50;
}