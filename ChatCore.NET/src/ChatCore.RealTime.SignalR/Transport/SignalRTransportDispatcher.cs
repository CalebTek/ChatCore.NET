namespace ChatCore.RealTime.SignalR.Transport;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Repositories;
using ChatCore.Abstractions.Transport;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ChatCore.RealTime.SignalR.Hubs;

/// <summary>
/// SignalR implementation of <see cref="ITransportDispatcher"/>.
/// </summary>
/// <remarks>
/// Injects <see cref="IServiceScopeFactory"/> instead of scoped repositories directly.
/// <see cref="DispatchAsync"/> is called from a fire-and-forget <c>Task.Run</c> in
/// <c>ChatEngine.SendAsync</c>, which runs after the originating HTTP request scope has
/// been disposed. Creating a fresh DI scope here ensures the <c>ChatCoreDbContext</c>
/// used to resolve excluded connection IDs is always live regardless of when the task runs.
/// </remarks>
public class SignalRTransportDispatcher : ITransportDispatcher
{
    private readonly IHubContext<ChatHub>              _hubContext;
    private readonly IServiceScopeFactory              _scopeFactory;
    private readonly ILogger<SignalRTransportDispatcher> _logger;

    public SignalRTransportDispatcher(
        IHubContext<ChatHub>               hubContext,
        IServiceScopeFactory               scopeFactory,
        ILogger<SignalRTransportDispatcher> logger)
    {
        _hubContext   = hubContext;
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    public async Task DispatchAsync(
        ChatMessage       message,
        Guid              conversationId,
        Guid              tenantId,
        Guid?             excludeUserId     = null,
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
                // Create a fresh scope so DbContext is always live even when called
                // from a fire-and-forget Task.Run after the request scope is disposed.
                await using var scope       = _scopeFactory.CreateAsyncScope();
                var             connections = scope.ServiceProvider
                                                 .GetRequiredService<IUserConnectionRepository>();

                var excludedConnectionIds = (await connections.GetByUserIdAsync(
                                                excludeUserId.Value, cancellationToken))
                                            .Select(c => c.ConnectionId)
                                            .ToList();

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
            _logger.LogError(ex,
                "Failed to dispatch message {MessageId} to conversation {ConversationId}",
                message.Id, conversationId);
        }
    }
}
