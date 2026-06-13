namespace ChatCore.RealTime.SignalR.Presence;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Presence;
using ChatCore.Abstractions.Repositories;

/// <summary>
/// Database-backed implementation of <see cref="IPresenceProvider"/>.
/// </summary>
public class DatabasePresenceProvider : IPresenceProvider
{
    private readonly IUserConnectionRepository _connections;

    public DatabasePresenceProvider(IUserConnectionRepository connections)
    {
        _connections = connections;
    }

    public async Task MarkOnlineAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default)
    {
        var connection = new UserConnection(userId, connectionId, DateTime.UtcNow);
        await _connections.AddAsync(connection, cancellationToken);
    }

    public async Task MarkOfflineAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default)
    {
        await _connections.RemoveAsync(userId, connectionId, cancellationToken);
    }

    public async Task<bool> IsOnlineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _connections.IsUserOnlineAsync(userId, cancellationToken);
    }

    public async Task<IEnumerable<Guid>> GetOnlineUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _connections.GetDistinctOnlineUserIdsAsync(cancellationToken);
    }
}
