namespace ChatCore.AspNetCore.Extensions;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for IServiceCollection to register ChatCore.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ChatCore to the service collection.
    /// </summary>
    public static ChatCoreBuilder AddChatCore(this IServiceCollection services)
    {
        return new ChatCoreBuilder(services);
    }
}