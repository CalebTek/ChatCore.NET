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
    /// Configures Entity Framework Core as the ChatCore storage provider.
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    public ChatCoreBuilder UseEntityFramework(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("A non-empty connection string is required.", nameof(connectionString));

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
    /// Configures SignalR as the real-time transport for ChatCore.
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
    /// Registers a custom message interceptor. Interceptors execute in registration order.
    /// </summary>
    /// <typeparam name="T">The interceptor type, which must implement <see cref="IMessageInterceptor"/>.</typeparam>
    public ChatCoreBuilder AddInterceptor<T>() where T : class, IMessageInterceptor
    {
        _services.AddScoped<IMessageInterceptor, T>();
        _interceptors.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Validates configuration and registers the core ChatCore services.
    /// Call this last, after <see cref="UseEntityFramework"/> and <see cref="UseSignalR"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="UseEntityFramework"/> or <see cref="UseSignalR"/> have not been called.
    /// </exception>
    public IServiceCollection Build()
    {
        if (_connectionString is null)
            throw new InvalidOperationException(
                "A storage provider must be configured. Call UseEntityFramework(connectionString) before Build().");

        if (!_useSignalR)
            throw new InvalidOperationException(
                "A real-time transport must be configured. Call UseSignalR() before Build().");

        // Register core services
        _services.AddScoped<IClock, SystemClock>();
        _services.AddScoped<IInterceptorPipeline, InterceptorPipeline>();
        _services.AddScoped<IChatEngine, ChatEngine>();

        return _services;
    }
}
