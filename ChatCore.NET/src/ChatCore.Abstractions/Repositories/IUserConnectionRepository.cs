namespace ChatCore.Abstractions.Repositories;

using ChatCore.Abstractions.Domain.Entities;

/// <summary>
/// Repository abstraction for user connection (presence) operations.
/// </summary>
public interface IUserConnectionRepository
{
    /// <summary>
    /// Adds a user connection.
    /// </summary>
    /// <param name="connection">The connection to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(UserConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a user connection.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all connections for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of user connections.</returns>
    Task<IEnumerable<UserConnection>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user is online (has active connections).
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the user is online; otherwise, false.</returns>
    Task<bool> IsUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the distinct set of user identifiers that currently have at least one active connection.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of online user identifiers with no duplicates.</returns>
    Task<IEnumerable<Guid>> GetDistinctOnlineUserIdsAsync(CancellationToken cancellationToken = default);
}
