namespace ChatCore.Abstractions.Domain.Entities;

/// <summary>
/// Represents a user's active connection for presence tracking.
/// </summary>
public class UserConnection
{
    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the connection identifier (typically from SignalR).
    /// </summary>
    public string ConnectionId { get; private set; }

    /// <summary>
    /// Gets the timestamp when the connection was established.
    /// </summary>
    public DateTime ConnectedAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserConnection"/> class.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="connectedAt">The connection timestamp.</param>
    public UserConnection(Guid userId, string connectionId, DateTime connectedAt)
    {
        UserId = userId;
        ConnectionId = connectionId;
        ConnectedAt = connectedAt;
    }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
#pragma warning disable CS8618
    protected UserConnection()
    {
    }
#pragma warning restore CS8618
}