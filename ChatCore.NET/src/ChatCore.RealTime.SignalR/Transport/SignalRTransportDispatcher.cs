namespace ChatCore.RealTime.SignalR.Transport;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Repositories;
using ChatCore.Abstractions.Transport;
using Microsoft.AspNetCore.SignalR;
using ChatCore.RealTime.SignalR.Hubs;

/// <summary>
/// SignalR implementation of <see cref="ITransportDispatcher"/>.
/// </summary>
public class SignalRTransportDispatcher : ITransportDispatcher
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IUserConnectionRepository _connections;

    public SignalRTransportDispatcher(IHubContext<ChatHub> hubContext, IUserConnectionRepository connections)
    {
        _hubContext = hubContext;
        _connections = connections;
    }

    public async Task DispatchAsync(
        ChatMessage message,
        Guid conversationId,
        Guid tenantId,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var groupName = $"conversation_{conversationId}";

            // Map message to DTO for transport
            var messageData = new
            {
                message.Id,
                message.ConversationId,
                message.SenderId,
                message.Content,
                message.SequenceNumber,
                message.SentAt,
                message.IsDeleted
            };

            if (excludeUserId.HasValue)
            {
                // Resolve all active connection IDs for the excluded user and omit them
                var excludedConnections = await _connections.GetByUserIdAsync(excludeUserId.Value, cancellationToken);
                var excludedConnectionIds = excludedConnections.Select(c => c.ConnectionId).ToList();

                if (excludedConnectionIds.Count > 0)
                {
                    await _hubContext.Clients
                        .GroupExcept(groupName, excludedConnectionIds)
                        .SendAsync("MessageReceived", messageData, cancellationToken);
                }
                else
                {
                    // Excluded user has no active connections — broadcast to everyone
                    await _hubContext.Clients.Group(groupName)
                        .SendAsync("MessageReceived", messageData, cancellationToken);
                }
            }
            else
            {
                await _hubContext.Clients.Group(groupName)
                    .SendAsync("MessageReceived", messageData, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Log dispatch errors but don't throw
            System.Diagnostics.Debug.WriteLine($"Error dispatching message: {ex.Message}");
        }
    }
}
