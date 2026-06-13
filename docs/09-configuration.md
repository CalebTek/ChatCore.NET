# Configuration & Deployment

## ⚙️ Overview

ChatCore.NET is configured through the `ChatCoreBuilder` fluent API at startup. This document covers all configuration options, environment-specific settings, and deployment considerations.

---

## 🚀 Registration

The entry point is `AddChatCore()`, which returns a `ChatCoreBuilder` for further configuration. Call `.Build()` at the end to register the core services.

```csharp
// Program.cs / Startup.cs
builder.Services
    .AddChatCore()
    .UseEntityFramework(connectionString)
    .UseSignalR()
    .AddInterceptor<ProfanityFilterInterceptor>()   // Optional
    .AddInterceptor<AuditLogInterceptor>()           // Optional — executed in order
    .Build();

// Map the SignalR hub
app.MapChatHub("/hubs/chat");   // Default; override as needed
```

---

## 🗄️ Entity Framework Configuration

### `UseEntityFramework(string connectionString)`

Registers the `ChatCoreDbContext` using SQL Server and wires up all four EF Core repository implementations.

```csharp
.UseEntityFramework("Server=.;Database=ChatCoreDb;Trusted_Connection=True;")
```

**Registered services:**

| Interface | Implementation |
|-----------|---------------|
| `IConversationRepository` | `ConversationRepository` |
| `IMessageRepository` | `MessageRepository` |
| `IReadReceiptRepository` | `ReadReceiptRepository` |
| `IUserConnectionRepository` | `UserConnectionRepository` |

### Connection String Configuration

Store your connection string in environment-specific settings files — never in source control:

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "ChatCore": "Server=(localdb)\\mssqllocaldb;Database=ChatCoreDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "ChatCore": "Server=prod-sql.internal;Database=ChatCoreDb;User Id=chatcore;Password=<use-secret-manager>"
  }
}
```

```csharp
// Read and pass to builder
var connectionString = builder.Configuration.GetConnectionString("ChatCore")
    ?? throw new InvalidOperationException("ChatCore connection string is required.");

builder.Services
    .AddChatCore()
    .UseEntityFramework(connectionString)
    // ...
```

### Running Migrations

ChatCore.NET uses EF Core code-first migrations. The migration commands target `ChatCore.Persistence.EFCore`.

```bash
# Create a migration
dotnet ef migrations add InitialCreate \
  --project src/ChatCore.Persistence.EFCore \
  --startup-project src/YourApp.API

# Apply migrations to the database
dotnet ef database update \
  --project src/ChatCore.Persistence.EFCore \
  --startup-project src/YourApp.API

# Generate SQL script (for production deployments)
dotnet ef migrations script \
  --project src/ChatCore.Persistence.EFCore \
  --startup-project src/YourApp.API \
  --output migrations.sql \
  --idempotent
```

**Apply migrations on startup (development only):**

```csharp
// Do NOT use this in production — run migrations as a deploy step instead
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ChatCoreDbContext>();
    await db.Database.MigrateAsync();
}
```

---

## 📡 SignalR Configuration

### `UseSignalR()`

Registers SignalR, the `SignalRTransportDispatcher`, and the `DatabasePresenceProvider`.

**Registered services:**

| Interface | Implementation |
|-----------|---------------|
| `ITransportDispatcher` | `SignalRTransportDispatcher` |
| `IPresenceProvider` | `DatabasePresenceProvider` |

### Basic SignalR Options

```csharp
// If you need to customise SignalR settings, call AddSignalR() before AddChatCore()
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors          = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize     = 32 * 1024;   // 32 KB
    options.ClientTimeoutInterval         = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval             = TimeSpan.FromSeconds(15);
    options.HandshakeTimeout              = TimeSpan.FromSeconds(15);
});

builder.Services
    .AddChatCore()
    .UseEntityFramework(connectionString)
    .UseSignalR()
    .Build();
```

### Redis Backplane (Horizontal Scaling)

For deployments with multiple application instances (load balancing), you must configure a SignalR backplane so hub messages are shared across nodes. Without this, clients connected to different instances will not receive each other's messages.

```bash
dotnet add package Microsoft.AspNetCore.SignalR.StackExchangeRedis
```

```csharp
builder.Services.AddSignalR()
    .AddStackExchangeRedis(
        builder.Configuration.GetConnectionString("Redis")!,
        options =>
        {
            options.Configuration.ChannelPrefix = "ChatCore";
        });
```

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Redis": "your-redis-host:6379,password=yourpassword,ssl=true"
  }
}
```

### CORS (Required for Browser Clients)

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("ChatCoreCors", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()!)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();   // Required for SignalR WebSocket upgrade
    });
});

// ...

app.UseCors("ChatCoreCors");   // Must be before UseRouting / MapHub
app.MapChatHub("/hubs/chat");
```

---

## 🔐 Authentication & Authorization

ChatCore.NET does not ship its own authentication stack. It reads the user identity from the ASP.NET Core `ClaimsPrincipal`, so any standard ASP.NET authentication middleware works.

### JWT Bearer

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience  = builder.Configuration["Auth:Audience"];

        // Required for SignalR — token comes in query string, not header
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path        = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
```

```json
// appsettings.json
{
  "Auth": {
    "Authority": "https://your-identity-provider.com",
    "Audience":  "chatcore-api"
  }
}
```

The hub reads user ID from the `sub` claim. Ensure your identity provider issues tokens with `sub` set to a parseable GUID that maps to your user IDs.

### Hub Authorization

To require authentication on all hub methods, add `[Authorize]` to the hub class:

```csharp
[Authorize]
public class ChatHub : Hub
{
    // All methods require an authenticated user
}
```

---

## 🔌 Custom Interceptors

Interceptors implement `IMessageInterceptor` and are executed in registration order. They can inspect or mutate the `MessageContext`, or cancel the send entirely.

```csharp
public class AuditLogInterceptor : IMessageInterceptor
{
    private readonly ILogger<AuditLogInterceptor> _logger;

    public AuditLogInterceptor(ILogger<AuditLogInterceptor> logger)
    {
        _logger = logger;
    }

    public Task OnBeforeSendAsync(MessageContext context, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Sending message {MessageId} in conversation {ConversationId}",
            context.Message.Id, context.Message.ConversationId);
        return Task.CompletedTask;
    }

    public Task OnAfterSendAsync(MessageContext context, CancellationToken ct = default)
    {
        _logger.LogInformation("Message {MessageId} sent successfully", context.Message.Id);
        return Task.CompletedTask;
    }
}
```

```csharp
// Registration
.AddInterceptor<AuditLogInterceptor>()
```

**Cancelling a message from an interceptor:**

```csharp
public Task OnBeforeSendAsync(MessageContext context, CancellationToken ct = default)
{
    if (ContainsProfanity(context.Message.Content))
    {
        context.IsCancelled       = true;
        context.CancellationReason = "Message contains prohibited content";
    }
    return Task.CompletedTask;
}
```

Interceptors are registered as `IMessageInterceptor` scoped services and support constructor injection of any DI-registered service.

---

## 🌍 Environment Configuration Reference

### `appsettings.json` Structure

```json
{
  "ConnectionStrings": {
    "ChatCore": "...",
    "Redis":    "..."
  },
  "Auth": {
    "Authority": "https://your-identity-provider.com",
    "Audience":  "chatcore-api"
  },
  "AllowedOrigins": [
    "https://your-frontend.com"
  ],
  "Logging": {
    "LogLevel": {
      "Default":                  "Information",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Microsoft.AspNetCore.SignalR":  "Information"
    }
  }
}
```

### Environment Variables

For production, prefer environment variables or a secrets manager over file-based secrets:

```bash
# Connection strings
CONNECTIONSTRINGS__CHATCORE="Server=prod-sql;..."
CONNECTIONSTRINGS__REDIS="prod-redis:6379"

# Auth
AUTH__AUTHORITY="https://your-identity-provider.com"
AUTH__AUDIENCE="chatcore-api"
```

---

## 🚢 Deployment

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/YourApp.API/YourApp.API.csproj -c Release -o /app/publish

FROM runtime AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "YourApp.API.dll"]
```

```yaml
# docker-compose.yml
services:
  api:
    build: .
    ports:
      - "8080:8080"
    environment:
      - CONNECTIONSTRINGS__CHATCORE=Server=db;Database=ChatCoreDb;User Id=sa;Password=YourStrong!Passw0rd;
      - CONNECTIONSTRINGS__REDIS=redis:6379
      - AUTH__AUTHORITY=https://your-identity-provider.com
      - AUTH__AUDIENCE=chatcore-api
    depends_on:
      - db
      - redis

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - SA_PASSWORD=YourStrong!Passw0rd
      - ACCEPT_EULA=Y

  redis:
    image: redis:7-alpine
```

### Health Checks

```csharp
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<ChatCoreDbContext>("chatcore-db")
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")!,
        name: "chatcore-redis");

app.MapHealthChecks("/health");
```

### Logging

ChatCore.NET itself does not configure a logging provider; it uses the standard `Microsoft.Extensions.Logging` abstractions. Configure your preferred provider (Serilog, NLog, Application Insights, etc.) independently:

```csharp
// Serilog example
builder.Host.UseSerilog((ctx, config) =>
    config.ReadFrom.Configuration(ctx.Configuration));
```

Useful log category filters:

| Category | Recommended Level |
|----------|------------------|
| `Microsoft.EntityFrameworkCore.Database.Command` | `Warning` (production) / `Information` (dev) |
| `Microsoft.AspNetCore.SignalR` | `Information` |
| `ChatCore.*` | `Information` |

---

## ⚠️ Known Configuration Gaps

| Item | Status | Notes |
|------|--------|-------|
| `_connectionString` field on `ChatCoreBuilder` | Tracked but unused in `Build()` | Harmless; reserved for future validation |
| `_useSignalR` flag on `ChatCoreBuilder` | Tracked but unused in `Build()` | Harmless; reserved for conditional registration |
| Redis backplane | Not wired by default | Must be configured manually as shown above |
| Migration on startup | Not provided | Apply migrations as a deploy step or via the suggested development guard |

---

**See Also:** [05-signalr-hub-reference.md](./05-signalr-hub-reference.md) | [07-database-schema.md](./07-database-schema.md) | [TROUBLESHOOTING.md](../TROUBLESHOOTING.md)
