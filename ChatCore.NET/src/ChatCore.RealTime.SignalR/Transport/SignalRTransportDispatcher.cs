namespace ChatCore.RealTime.SignalR.Transport;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Repositories;
using ChatCore.Abstractions.Transport;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ChatCore.RealTime.SignalR.Hubs;

/// <summary>
/// SignalR implementation of <see cref="ITransportDispatcher"/>.
/// </summary>
public class SignalRTransportDispatcher : ITransportDispatcher
{
    private readonly IHubContext<ChatHub>         _hubContext;
    private readonly IUserConnectionRepository    _connections;
    private readonly ILogger<SignalRTransportDispatcher> _logger;

    public SignalRTransportDispatcher(
        IHubContext<ChatHub>              hubContext,
        IUserConnectionRepository         connections,
        ILogger<SignalRTransportDispatcher> logger)
    {
        _hubContext  = hubContext;
        _connections = connections;
        _logger      = logger;
    }

    public async Task DispatchAsync(
        ChatMessage message,
        Guid        conversationId,
        Guid        tenantId,
        Guid?       excludeUserId     = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var groupName = $"conversation_{conversationId}";

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
                var excludedConnections  = await _connections.GetByUserIdAsync(excludeUserId.Value, cancellationToken);
                var excludedConnectionIds = excludedConnections.Select(c => c.ConnectionId).ToList();

                if (excludedConnectionIds.Count > 0)
                {
                    await _hubContext.Clients
                        .GroupExcept(groupName, excludedConnectionIds)
                        .SendAsync("MessageReceived", messageData, cancellationToken);
                }
                else
                {
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
            _logger.LogError(ex,
                "Failed to dispatch message {MessageId} to conversation {ConversationId}",
                message.Id, conversationId);
        }
    }
}
