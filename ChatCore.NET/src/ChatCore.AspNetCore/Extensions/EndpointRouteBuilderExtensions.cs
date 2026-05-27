namespace ChatCore.AspNetCore.Extensions;

using ChatCore.RealTime.SignalR.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Extension methods for IEndpointRouteBuilder to map ChatCore hubs.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the ChatHub SignalR hub.
    /// </summary>
    public static void MapChatHub(this IEndpointRouteBuilder endpoints, string pattern = "/hubs/chat")
    {
        endpoints.MapHub<ChatHub>(pattern);
    }
}