namespace ChatCore.Abstractions.Domain.Enums;

/// <summary>
/// Defines the type of conversation.
/// </summary>
public enum ConversationType
{
    /// <summary>
    /// Direct one-to-one conversation between two users.
    /// </summary>
    Direct = 0,

    /// <summary>
    /// Group conversation with multiple participants.
    /// </summary>
    Group = 1
}