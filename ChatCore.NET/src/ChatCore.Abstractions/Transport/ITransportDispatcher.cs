namespace ChatCore.Abstractions.Transport;

using ChatCore.Abstractions.Domain.Entities;

/// <summary>
/// Defines transport abstraction for dispatching messages to participants.
/// </summary>
public interface ITransportDispatcher
{
    /// <summary>
    /// Dispatches a message to all participants in a conversation.
    /// </summary>
    /// <param name="message">The message to dispatch.</param>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="excludeUserId">The user identifier to exclude from dispatch (typically the sender).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DispatchAsync(ChatMessage message, Guid conversationId, Guid tenantId, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
}