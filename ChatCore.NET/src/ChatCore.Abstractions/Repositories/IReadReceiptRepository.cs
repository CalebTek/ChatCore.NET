namespace ChatCore.Abstractions.Repositories;

using ChatCore.Abstractions.Domain.Entities;

/// <summary>
/// Repository abstraction for read receipt operations.
/// </summary>
public interface IReadReceiptRepository
{
    /// <summary>
    /// Creates or updates a read receipt.
    /// </summary>
    /// <param name="read">The read receipt to create or update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateOrUpdateAsync(MessageRead read, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a read receipt by conversation and user.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The read receipt, or null if not found.</returns>
    Task<MessageRead?> GetByConversationAndUserAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all read receipts for a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of read receipts.</returns>
    Task<IEnumerable<MessageRead>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
}