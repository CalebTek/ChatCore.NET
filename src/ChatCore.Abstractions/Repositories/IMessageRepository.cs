namespace ChatCore.Abstractions.Repositories;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Results;

/// <summary>
/// Repository abstraction for message operations.
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// Creates a new message.
    /// </summary>
    /// <param name="message">The message to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateAsync(ChatMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a message by identifier.
    /// </summary>
    /// <param name="id">The message identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The message, or null if not found.</returns>
    Task<ChatMessage?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves messages from a conversation using seek-based pagination.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="lastSeenSequence">The last seen sequence number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated result of messages.</returns>
    Task<PaginatedResult<ChatMessage>> GetByConversationIdAsync(Guid conversationId, Guid tenantId, long lastSeenSequence, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a message by idempotency key to support deduplication.
    /// </summary>
    /// <param name="idempotencyKey">The idempotency key.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The message, or null if not found.</returns>
    Task<ChatMessage?> GetByIdempotencyKeyAsync(string idempotencyKey, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a message.
    /// </summary>
    /// <param name="message">The message to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(ChatMessage message, CancellationToken cancellationToken = default);
}