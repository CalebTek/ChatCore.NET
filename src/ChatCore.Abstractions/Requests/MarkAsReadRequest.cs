namespace ChatCore.Abstractions.Requests;

/// <summary>
/// Request to mark messages as read.
/// </summary>
public class MarkAsReadRequest
{
    /// <summary>
    /// Gets or sets the conversation identifier.
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the last read sequence number.
    /// </summary>
    public long LastReadSequence { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }
}