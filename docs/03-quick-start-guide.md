# Quick Start Guide

## 🎯 Overview

This guide walks you from zero to a working chat endpoint in a new ASP.NET Core application. By the end you will have ChatCore.NET wired up, a database ready, real-time messaging active via SignalR, and your first message sent.

**Prerequisites:**
- .NET 8.0 SDK or later
- SQL Server (LocalDB is fine for development)
- An existing ASP.NET Core Web API project, or create one below

---

## 1. Create a New Project (optional)

```bash
dotnet new webapi -n MyChat.API
cd MyChat.API
```

---

## 2. Reference the ChatCore.NET Projects

Until the NuGet packages are published, add project references directly:

```bash
# From your API project directory
dotnet add reference ../ChatCore.NET/src/ChatCore.AspNetCore/ChatCore.AspNetCore.csproj
```

`ChatCore.AspNetCore` references all other ChatCore projects transitively — it is the only reference your app needs.

---

## 3. Configure Services

Open `Program.cs` and register ChatCore with the fluent builder:

```csharp
using ChatCore.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ── ChatCore.NET ──────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("ChatCore")
    ?? throw new InvalidOperationException("Connection string 'ChatCore' not found.");

builder.Services
    .AddChatCore()
    .UseEntityFramework(connectionString)   // EF Core + SQL Server repositories
    .UseSignalR()                           // SignalR hub + presence provider
    .Build();

// ── Standard ASP.NET Core ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddAuthentication(/* your scheme */);
builder.Services.AddAuthorization();

// CORS is required for browser SignalR clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("ChatCoreCors", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()!)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

var app = builder.Build();

app.UseCors("ChatCoreCors");    // must be before MapHub
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapChatHub("/hubs/chat");   // registers ChatHub at this path

app.Run();
```

---

## 4. Add the Connection String

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "ChatCore": "Server=(localdb)\\mssqllocaldb;Database=ChatCoreDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "AllowedOrigins": [ "http://localhost:3000" ]
}
```

---

## 5. Apply Database Migrations

Run the migration command targeting the persistence project:

```bash
# From the repo root
dotnet ef migrations add InitialCreate \
  --project src/ChatCore.Persistence.EFCore \
  --startup-project src/YourApp.API

dotnet ef database update \
  --project src/ChatCore.Persistence.EFCore \
  --startup-project src/YourApp.API
```

For development convenience you can apply migrations automatically on startup:

```csharp
// Program.cs — development only
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ChatCoreDbContext>();
    await db.Database.MigrateAsync();
}
```

> ⚠️ Do not use auto-migration in production. Generate a SQL script and apply it as a deployment step instead.

---

## 6. Expose the Engine via a Controller

Inject `IChatEngine` into any controller to use ChatCore operations over HTTP:

```csharp
using ChatCore.Abstractions.Engine;
using ChatCore.Abstractions.Queries;
using ChatCore.Abstractions.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IChatEngine _engine;

    public ConversationsController(IChatEngine engine) => _engine = engine;

    // GET /api/conversations?page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new GetConversationsQuery
        {
            UserId   = Guid.Parse(User.FindFirst("sub")!.Value),
            TenantId = Guid.Parse(User.FindFirst("tenant_id")!.Value),
            Page     = page,
            PageSize = pageSize
        };

        var result = await _engine.GetConversationsAsync(query, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    // GET /api/conversations/{id}/messages?lastSeenSequence=9223372036854775807&pageSize=50
    [HttpGet("{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] long lastSeenSequence = long.MaxValue,
        [FromQuery] int  pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GetMessagesQuery
        {
            ConversationId   = conversationId,
            UserId           = Guid.Parse(User.FindFirst("sub")!.Value),
            TenantId         = Guid.Parse(User.FindFirst("tenant_id")!.Value),
            LastSeenSequence = lastSeenSequence,
            PageSize         = pageSize
        };

        var result = await _engine.GetMessagesAsync(query, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    // POST /api/conversations/{id}/messages
    [HttpPost("{conversationId}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendMessageBody body,
        CancellationToken ct = default)
    {
        var request = new ChatMessageRequest
        {
            ConversationId = conversationId,
            SenderId       = Guid.Parse(User.FindFirst("sub")!.Value),
            TenantId       = Guid.Parse(User.FindFirst("tenant_id")!.Value),
            Content        = body.Content,
            IdempotencyKey = body.IdempotencyKey
        };

        var result = await _engine.SendAsync(request, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    // DELETE /api/conversations/{convId}/messages/{msgId}
    [HttpDelete("{conversationId}/messages/{messageId}")]
    public async Task<IActionResult> DeleteMessage(
        Guid conversationId,
        Guid messageId,
        CancellationToken ct = default)
    {
        var result = await _engine.DeleteMessageAsync(
            messageId,
            Guid.Parse(User.FindFirst("sub")!.Value),
            Guid.Parse(User.FindFirst("tenant_id")!.Value),
            ct);

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}

public record SendMessageBody(string Content, string? IdempotencyKey);
```

---

## 7. Connect from a JavaScript Client

```typescript
import * as signalR from "@microsoft/signalr";

const token = "your-jwt-token";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/chat", {
        accessTokenFactory: () => token
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

// ── Receive events ────────────────────────────────────────────────────────────
connection.on("MessageReceived", (message) => {
    console.log(`[${message.sequenceNumber}] ${message.content}`);
});

connection.on("MessageRead", (conversationId, userId, lastReadSequence) => {
    console.log(`User ${userId} read up to sequence ${lastReadSequence}`);
});

connection.on("UserTyping",        (userId) => showTypingIndicator(userId));
connection.on("UserStoppedTyping", (userId) => hideTypingIndicator(userId));

connection.on("Error", (message) => {
    console.error("Hub error:", message);
});

// ── Start and join ────────────────────────────────────────────────────────────
await connection.start();
console.log("Connected to ChatCore.NET");

const conversationId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

// Join every conversation the user participates in
await connection.invoke("JoinConversation", conversationId);

// ── Send a message ────────────────────────────────────────────────────────────
const tenantId = "11111111-1111-1111-1111-111111111111";
await connection.invoke("SendMessage", conversationId, "Hello, World!", tenantId);

// ── Mark as read ──────────────────────────────────────────────────────────────
await connection.invoke("MarkAsRead", conversationId, 1, tenantId);

// ── Typing indicators ─────────────────────────────────────────────────────────
inputElement.addEventListener("input", () => {
    connection.invoke("IsTyping", conversationId);
    clearTimeout(typingTimer);
    typingTimer = setTimeout(() => {
        connection.invoke("StoppedTyping", conversationId);
    }, 2000);
});
```

---

## 8. Add a Custom Interceptor (Optional)

Interceptors let you add cross-cutting logic — content filtering, audit logging, rate limiting — without touching the engine.

```csharp
using ChatCore.Abstractions.Interceptors;

public class ProfanityFilterInterceptor : IMessageInterceptor
{
    private static readonly string[] Blocked = { "badword1", "badword2" };

    public Task OnBeforeSendAsync(MessageContext context, CancellationToken ct = default)
    {
        if (Blocked.Any(w => context.Message.Content.Contains(w, StringComparison.OrdinalIgnoreCase)))
        {
            context.IsCancelled       = true;
            context.CancellationReason = "Message contains prohibited content";
        }
        return Task.CompletedTask;
    }

    public Task OnAfterSendAsync(MessageContext context, CancellationToken ct = default)
        => Task.CompletedTask;
}
```

Register it in the builder — interceptors execute in registration order:

```csharp
builder.Services
    .AddChatCore()
    .UseEntityFramework(connectionString)
    .UseSignalR()
    .AddInterceptor<ProfanityFilterInterceptor>()
    .Build();
```

---

## 9. Verify Everything Works

**Run the app:**
```bash
dotnet run --project src/MyChat.API
```

**Quick smoke test with `curl`:**

```bash
# Send a message (replace IDs and token)
curl -X POST https://localhost:5001/api/conversations/{convId}/messages \
  -H "Authorization: Bearer {your-token}" \
  -H "Content-Type: application/json" \
  -d '{"content": "Hello from curl!", "idempotencyKey": "test-001"}'
```

**Expected response:**
```json
{
  "id": "...",
  "conversationId": "...",
  "senderId": "...",
  "content": "Hello from curl!",
  "sequenceNumber": 1,
  "sentAt": "2026-06-13T10:00:00Z",
  "isDeleted": false
}
```

---

## ✅ What You Have Now

| Capability | How it works |
|-----------|-------------|
| Send a message | `POST /api/conversations/{id}/messages` or `SendMessage` hub method |
| Load message history | `GET /api/conversations/{id}/messages` with seek-based cursor |
| Real-time delivery | SignalR hub broadcasts `MessageReceived` to all joined clients |
| Read receipts | `MarkAsRead` hub method or `PUT .../read` REST endpoint |
| Typing indicators | `IsTyping` / `StoppedTyping` hub methods |
| Presence | Automatic on connect/disconnect via `IPresenceProvider` |
| Duplicate prevention | Pass `idempotencyKey` on send |
| Soft delete | `DELETE .../messages/{id}` — content replaced, record retained |
| Custom middleware | Register `IMessageInterceptor` implementations |

---

## 📚 Next Steps

- **[05-signalr-hub-reference.md](./05-signalr-hub-reference.md)** — Full hub method and event reference
- **[06-api-endpoints.md](./06-api-endpoints.md)** — Complete request/response contracts and error codes
- **[09-configuration.md](./09-configuration.md)** — Redis backplane, auth wiring, Docker, health checks
- **[08-testing-strategies.md](./08-testing-strategies.md)** — Writing unit and integration tests

---

**Last Updated:** June 13, 2026
