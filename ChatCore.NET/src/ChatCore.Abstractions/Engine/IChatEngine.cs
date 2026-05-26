namespace ChatCore.Abstractions.Engine;

using ChatCore.Abstractions.DTOs;
using ChatCore.Abstractions.Queries;
using ChatCore.Abstractions.Requests;
using ChatCore.Abstractions.Results;

/// <summary>
/// Defines the core chat engine.
/// </summary>
public interface IChatEngine
{
    /// <summary>
    /// Sends a message.
    /// </summary>
    /// <param name="request">The send message request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result containing the sent message DTO.</returns>
    Task<ChatResult<ChatMessageDto>> SendAsync(ChatMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks messages as read.
    /// </summary>
    /// <param name="request">The mark as read request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<ChatResult<bool>> MarkAsReadAsync(MarkAsReadRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves messages from a conversation.
    /// </summary>
    /// <param name="query">The get messages query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result containing paginated messages.</returns>
    Task<ChatResult<PaginatedResult<ChatMessageDto>>> GetMessagesAsync(GetMessagesQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves conversations for a user.
    /// </summary>
    /// <param name="query">The get conversations query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result containing conversations.</returns>
    Task<ChatResult<IEnumerable<ConversationDto>>> GetConversationsAsync(GetConversationsQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a message.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="userId">The user identifier (must be the sender).</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<ChatResult<bool>> DeleteMessageAsync(Guid messageId, Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}