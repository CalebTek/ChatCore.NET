namespace ChatCore.Abstractions.Repositories;

using ChatCore.Abstractions.Domain.Entities;

/// <summary>
/// Repository abstraction for conversation operations.
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    /// <param name="conversation">The conversation to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateAsync(Conversation conversation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a conversation by identifier.
    /// </summary>
    /// <param name="id">The conversation identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The conversation, or null if not found.</returns>
    Task<Conversation?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves conversations for a specific user with pagination.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="skip">The number of items to skip.</param>
    /// <param name="take">The number of items to take.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of conversations.</returns>
    Task<IEnumerable<Conversation>> GetByUserIdAsync(Guid userId, Guid tenantId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user is a participant in a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the user is a participant; otherwise, false.</returns>
    Task<bool> IsUserParticipantAsync(Guid conversationId, Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a conversation.
    /// </summary>
    /// <param name="conversation">The conversation to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default);
}