namespace ChatCore.Abstractions.Presence;

/// <summary>
/// Defines presence tracking abstraction.
/// </summary>
public interface IPresenceProvider
{
    /// <summary>
    /// Marks a user as online.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkOnlineAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a user as offline.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkOfflineAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user is online.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the user is online; otherwise, false.</returns>
    Task<bool> IsOnlineAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all online users.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of online user identifiers.</returns>
    Task<IEnumerable<Guid>> GetOnlineUsersAsync(CancellationToken cancellationToken = default);
}