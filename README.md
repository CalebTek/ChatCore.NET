# ChatCore.NET

> A modular, scalable, SignalR-powered chat framework for .NET

[![NuGet](https://img.shields.io/nuget/v/ChatCore.AspNetCore.svg)](https://www.nuget.org/packages/ChatCore.AspNetCore)
[![CI](https://github.com/CalebTek/ChatCore.NET/actions/workflows/ci.yml/badge.svg)](https://github.com/CalebTek/ChatCore.NET/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## 🎯 Overview

ChatCore.NET is an enterprise-grade chat engine for .NET 8 applications. It provides a clean, pluggable abstraction over real-time communication with support for 1:1 and group conversations, read receipts, presence tracking, and horizontal scaling.

## ✨ Features

- **Modular Architecture** — clean separation of concerns with pluggable storage and transport layers
- **Real-Time Delivery** — built-in SignalR integration with automatic sender exclusion
- **Multi-Tenancy Ready** — tenant isolation enforced at every layer including composite foreign keys
- **Message Ordering Guarantee** — atomic sequence-number assignment per conversation
- **Read Receipts** — high-water-mark tracking per user per conversation
- **Presence Tracking** — online/offline status with multi-connection support
- **Soft Deletes** — message content replaced with `[deleted]`, record retained for audit
- **Idempotency** — prevent duplicate sends with a client-supplied idempotency key
- **Interceptor Pipeline** — middleware-style hooks before and after every send
- **Seek-based Pagination** — cursor pagination on sequence numbers — no `OFFSET` scans
- **Global Exception Handling** — structured JSON error responses from a single middleware

## 📦 Packages

| Package | Description |
|---------|-------------|
| [`ChatCore.Abstractions`](https://www.nuget.org/packages/ChatCore.Abstractions) | Domain models, interfaces, contracts — no infrastructure dependencies |
| [`ChatCore.Core`](https://www.nuget.org/packages/ChatCore.Core) | `ChatEngine` implementation and interceptor pipeline |
| [`ChatCore.Persistence.EFCore`](https://www.nuget.org/packages/ChatCore.Persistence.EFCore) | SQL Server repositories via EF Core 8 |
| [`ChatCore.RealTime.SignalR`](https://www.nuget.org/packages/ChatCore.RealTime.SignalR) | SignalR hub, transport dispatcher, presence provider |
| [`ChatCore.AspNetCore`](https://www.nuget.org/packages/ChatCore.AspNetCore) | ASP.NET Core integration — the only package most apps need |

## 🚀 Quick Start

### 1. Install

```bash
dotnet add package ChatCore.AspNetCore
```

### 2. Register services

```csharp
// Program.cs
builder.Services
    .AddChatCore()
    .UseEntityFramework(connectionString)
    .UseSignalR()
    .Build();
```

### 3. Map the hub and middleware

```csharp
app.UseChatCoreExceptionHandler();  // global exception handler — register first
app.UseCors("ChatCoreCors");        // required for browser SignalR clients
app.MapChatHub("/hubs/chat");
```

### 4. Send a message

```csharp
public class MessagesController : ControllerBase
{
    private readonly IChatEngine _engine;
    public MessagesController(IChatEngine engine) => _engine = engine;

    [HttpPost("{conversationId}/messages")]
    public async Task<IActionResult> Send(Guid conversationId, [FromBody] SendBody body)
    {
        var result = await _engine.SendAsync(new ChatMessageRequest
        {
            ConversationId = conversationId,
            SenderId       = User.GetUserId(),
            TenantId       = User.GetTenantId(),
            Content        = body.Content,
            IdempotencyKey = body.IdempotencyKey
        });
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }
}
```

### 5. Connect from JavaScript

```typescript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/chat", { accessTokenFactory: () => yourJwtToken })
    .withAutomaticReconnect()
    .build();

connection.on("MessageReceived", (message) => console.log(message));

await connection.start();
await connection.invoke("JoinConversation", conversationId);
await connection.invoke("SendMessage", conversationId, "Hello!", tenantId);
```

## 📚 Documentation

Full documentation is in the [`docs/`](docs/) folder:

| Doc | Description |
|-----|-------------|
| [Architecture Overview](docs/01-architecture-overview.md) | Layers, data flow, design decisions |
| [Project Structure](docs/02-project-structure.md) | Directory layout, dependency graph |
| [Quick Start Guide](docs/03-quick-start-guide.md) | Step-by-step setup |
| [Domain Models](docs/04-domain-models.md) | Entity and DTO reference |
| [SignalR Hub Reference](docs/05-signalr-hub-reference.md) | All hub methods and events |
| [API Endpoints](docs/06-api-endpoints.md) | Request/response contracts, error codes |
| [Database Schema](docs/07-database-schema.md) | Tables, indexes, migrations |
| [Testing Strategies](docs/08-testing-strategies.md) | Unit and integration test guide |
| [Configuration](docs/09-configuration.md) | Redis, auth, Docker, health checks |
| [Contributing](docs/10-contributing-guidelines.md) | Branch strategy, PR process, standards |

## 🔐 Design Principles

1. **Storage Agnostic** — swap storage behind an interface without touching the engine
2. **Transport Agnostic** — SignalR today, anything tomorrow
3. **Multi-Tenant by Default** — every entity carries `TenantId`, enforced at the database FK level
4. **Ordering Guarantee** — atomic sequence numbers per conversation
5. **Scalability First** — seek pagination, stateless design, Redis backplane support
6. **Extensible** — interceptor pipeline for content filtering, audit logging, rate limiting
7. **Test Friendly** — interface-driven, 70+ tests across unit and integration suites

## 📝 Roadmap

- [x] Core abstractions and domain models
- [x] ChatEngine with full send / read / paginate / delete
- [x] Entity Framework Core persistence layer
- [x] SignalR transport with correct sender exclusion
- [x] ASP.NET Core integration and DI builder
- [x] Unit and integration tests (70+ tests)
- [x] Global exception middleware
- [x] NuGet package release
- [ ] Redis backplane support
- [ ] Message encryption layer
- [ ] Webhook support for external integrations
- [ ] Multi-tenant admin dashboard

## 🤝 Contributing

Contributions are welcome! See [docs/10-contributing-guidelines.md](docs/10-contributing-guidelines.md) for the full guide.

## 📄 License

MIT — see [LICENSE](LICENSE) for details.

## 👨‍💻 Author

CalebTek — [GitHub](https://github.com/CalebTek)
