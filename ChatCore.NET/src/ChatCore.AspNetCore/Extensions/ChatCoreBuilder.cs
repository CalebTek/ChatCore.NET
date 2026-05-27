namespace ChatCore.AspNetCore.Extensions;

using ChatCore.Abstractions.Engine;
using ChatCore.Abstractions.Interceptors;
using ChatCore.Abstractions.Presence;
using ChatCore.Abstractions.Repositories;
using ChatCore.Abstractions.Services;
using ChatCore.Abstractions.Transport;
using ChatCore.Core.Engine;
using ChatCore.Core.Interceptors;
using ChatCore.Core.Services;
using ChatCore.Persistence.EFCore;
using ChatCore.Persistence.EFCore.Repositories;
using ChatCore.RealTime.SignalR.Hubs;
using ChatCore.RealTime.SignalR.Presence;
using ChatCore.RealTime.SignalR.Transport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Fluent builder for configuring ChatCore.
/// </summary>
public class ChatCoreBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<Type> _interceptors = new();
    private string? _connectionString;
    private bool _useSignalR;

    public ChatCoreBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Configures Entity Framework Core for ChatCore.
    /// </summary>
    public ChatCoreBuilder UseEntityFramework(string connectionString)
    {
        _connectionString = connectionString;

        _services.AddDbContext<ChatCoreDbContext>(options =>
            options.UseSqlServer(connectionString));

        _services.AddScoped<IConversationRepository, ConversationRepository>();
        _services.AddScoped<IMessageRepository, MessageRepository>();
        _services.AddScoped<IReadReceiptRepository, ReadReceiptRepository>();
        _services.AddScoped<IUserConnectionRepository, UserConnectionRepository>();

        return this;
    }

    /// <summary>
    /// Configures SignalR for real-time communication.
    /// </summary>
    public ChatCoreBuilder UseSignalR()
    {
        _useSignalR = true;

        _services.AddSignalR();
        _services.AddScoped<ITransportDispatcher, SignalRTransportDispatcher>();
        _services.AddScoped<IPresenceProvider, DatabasePresenceProvider>();

        return this;
    }

    /// <summary>
    /// Adds a custom message interceptor.
    /// </summary>
    public ChatCoreBuilder AddInterceptor<T>() where T : class, IMessageInterceptor
    {
        _services.AddScoped<IMessageInterceptor, T>();
        _interceptors.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Builds the ChatCore configuration.
    /// </summary>
    public IServiceCollection Build()
    {
        // Register core services
        _services.AddScoped<IClock, SystemClock>();
        _services.AddScoped<IInterceptorPipeline, InterceptorPipeline>();
        _services.AddScoped<IChatEngine, ChatEngine>();

        return _services;
    }
}