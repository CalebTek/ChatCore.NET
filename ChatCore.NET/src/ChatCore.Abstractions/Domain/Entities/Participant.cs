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
    /// Gets the timestamp when the user joined.
    /// </summary>
    public DateTime JoinedAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Participant"/> class.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="joinedAt">The join timestamp.</param>
    public Participant(Guid conversationId, Guid userId, DateTime joinedAt)
    {
        ConversationId = conversationId;
        UserId = userId;
        JoinedAt = joinedAt;
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