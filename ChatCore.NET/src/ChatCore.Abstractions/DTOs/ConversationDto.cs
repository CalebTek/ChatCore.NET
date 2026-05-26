namespace ChatCore.Abstractions.DTOs;

using ChatCore.Abstractions.Domain.Enums;

/// <summary>
/// Data transfer object for conversations.
/// </summary>
public class ConversationDto
{
    /// <summary>
    /// Gets or sets the conversation identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the conversation type.
    /// </summary>
    public ConversationType Type { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the participant identifiers.
    /// </summary>
    public IEnumerable<Guid> ParticipantIds { get; set; } = Enumerable.Empty<Guid>();
}