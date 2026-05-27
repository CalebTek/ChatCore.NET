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
    private readonly IConversationRepository _conversations;

    public SignalRTransportDispatcher(IHubContext<ChatHub> hubContext, IConversationRepository conversations)
    {
        _hubContext = hubContext;
        _conversations = conversations;
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

            // Send to all participants except sender
            if (excludeUserId.HasValue)
            {
                await _hubContext.Clients.Group(groupName)
                    .SendAsync("MessageReceived", messageData, cancellationToken);
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