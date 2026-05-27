namespace ChatCore.RealTime.SignalR.Hubs;

using ChatCore.Abstractions.Engine;
using ChatCore.Abstractions.Presence;
using ChatCore.Abstractions.Queries;
using ChatCore.Abstractions.Requests;
using Microsoft.AspNetCore.SignalR;

/// <summary>
/// SignalR hub for real-time chat operations.
/// </summary>
public class ChatHub : Hub
{
    private readonly IChatEngine _engine;
    private readonly IPresenceProvider _presence;

    public ChatHub(IChatEngine engine, IPresenceProvider presence)
    {
        _engine = engine;
        _presence = presence;
    }

    /// <summary>
    /// Called when a client connects.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value ?? Context.ConnectionId;
        if (Guid.TryParse(userId, out var userIdGuid))
        {
            await _presence.MarkOnlineAsync(userIdGuid, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("sub")?.Value ?? Context.ConnectionId;
        if (Guid.TryParse(userId, out var userIdGuid))
        {
            await _presence.MarkOfflineAsync(userIdGuid, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Sends a message to a conversation.
    /// </summary>
    public async Task SendMessage(Guid conversationId, string content, Guid tenantId)
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userId, out var userIdGuid))
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        var request = new ChatMessageRequest
        {
            ConversationId = conversationId,
            SenderId = userIdGuid,
            TenantId = tenantId,
            Content = content
        };

        var result = await _engine.SendAsync(request);

        if (result.IsSuccess)
        {
            // Notify all participants in the conversation
            await Clients.Group($"conversation_{conversationId}")
                .SendAsync("MessageReceived", result.Data);
        }
        else
        {
            await Clients.Caller.SendAsync("Error", result.Error);
        }
    }

    /// <summary>
    /// Marks messages as read.
    /// </summary>
    public async Task MarkAsRead(Guid conversationId, long lastReadSequence, Guid tenantId)
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userId, out var userIdGuid))
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        var request = new MarkAsReadRequest
        {
            ConversationId = conversationId,
            UserId = userIdGuid,
            LastReadSequence = lastReadSequence,
            TenantId = tenantId
        };

        var result = await _engine.MarkAsReadAsync(request);

        if (result.IsSuccess)
        {
            // Notify all participants of the read receipt
            await Clients.Group($"conversation_{conversationId}")
                .SendAsync("MessageRead", conversationId, userIdGuid, lastReadSequence);
        }
        else
        {
            await Clients.Caller.SendAsync("Error", result.Error);
        }
    }

    /// <summary>
    /// Joins a conversation group.
    /// </summary>
    public async Task JoinConversation(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        await Clients.Group($"conversation_{conversationId}")
            .SendAsync("UserJoined", Context.ConnectionId);
    }

    /// <summary>
    /// Leaves a conversation group.
    /// </summary>
    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        await Clients.Group($"conversation_{conversationId}")
            .SendAsync("UserLeft", Context.ConnectionId);
    }

    /// <summary>
    /// Broadcasts typing indicator.
    /// </summary>
    public async Task IsTyping(Guid conversationId)
    {
        var userId = Context.User?.FindFirst("sub")?.Value ?? Context.ConnectionId;
        await Clients.Group($"conversation_{conversationId}")
            .SendAsync("UserTyping", userId);
    }

    /// <summary>
    /// Broadcasts stopped typing indicator.
    /// </summary>
    public async Task StoppedTyping(Guid conversationId)
    {
        var userId = Context.User?.FindFirst("sub")?.Value ?? Context.ConnectionId;
        await Clients.Group($"conversation_{conversationId}")
            .SendAsync("UserStoppedTyping", userId);
    }
}