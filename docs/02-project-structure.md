# Project Structure

## 📁 Repository Layout

```
ChatCore.NET/
├── ChatCore.NET/                         # Solution root
│   ├── ChatCore.NET.slnx                 # Solution file
│   └── src/
│       ├── ChatCore.Abstractions/        # Contracts, domain models, interfaces
│       ├── ChatCore.Core/                # Engine & business logic
│       ├── ChatCore.Persistence.EFCore/  # EF Core storage layer
│       ├── ChatCore.RealTime.SignalR/    # SignalR transport
│       └── ChatCore.AspNetCore/          # ASP.NET Core integration & DI
├── tests/
│   ├── ChatCore.Tests.Unit/              # Unit tests (xUnit + Moq)
│   └── ChatCore.Tests.Integration/       # Integration tests (EF InMemory)
├── docs/                                 # All documentation
├── .gitignore
├── CODE_OF_CONDUCT.md
├── README.md
└── TROUBLESHOOTING.md
```

---

## 📦 Projects

### `ChatCore.Abstractions`
**Target:** `net8.0` | **Role:** Shared contracts — no business logic, no framework dependencies.

Everything every other project depends on lives here: domain entities, enums, interfaces, request/response models, and DTOs.

```
ChatCore.Abstractions/
├── Domain/
│   ├── Entities/
│   │   ├── ChatMessage.cs          # Core message entity with sequence ordering
│   │   ├── Conversation.cs         # Conversation with atomic sequence counter
│   │   ├── MessageRead.cs          # Read receipt (high-water mark per user)
│   │   ├── Participant.cs          # User ↔ Conversation membership
│   │   └── UserConnection.cs       # Active SignalR connection (presence)
│   └── Enums/
│       ├── ConversationType.cs     # Direct = 0, Group = 1
│       └── MessageStatus.cs        # Pending, Sent, Delivered, Read
├── DTOs/
│   ├── ChatMessageDto.cs           # API-facing message shape
│   └── ConversationDto.cs          # API-facing conversation shape
├── Engine/
│   └── IChatEngine.cs              # Primary entry point for all chat operations
├── Interceptors/
│   ├── IMessageInterceptor.cs      # Before/after send hooks
│   ├── IInterceptorPipeline.cs     # Pipeline runner
│   └── MessageContext.cs           # Mutable context passed through interceptors
├── Presence/
│   └── IPresenceProvider.cs        # Online/offline tracking abstraction
├── Queries/
│   ├── GetConversationsQuery.cs
│   └── GetMessagesQuery.cs
├── Repositories/
│   ├── IConversationRepository.cs
│   ├── IMessageRepository.cs
│   ├── IReadReceiptRepository.cs
│   └── IUserConnectionRepository.cs
├── Requests/
│   ├── ChatMessageRequest.cs
│   └── MarkAsReadRequest.cs
├── Results/
│   ├── ChatResult.cs               # Success/failure wrapper for all engine ops
│   └── PaginatedResult.cs          # Seek-based pagination envelope
├── Services/
│   └── IClock.cs                   # Testable clock abstraction
└── Transport/
    └── ITransportDispatcher.cs     # Abstraction for broadcasting messages
```

**Key design rule:** Nothing in `ChatCore.Abstractions` may reference any other ChatCore project. It is the dependency root.

---

### `ChatCore.Core`
**Target:** `net8.0` | **Role:** Business logic and orchestration. References only `ChatCore.Abstractions`.

```
ChatCore.Core/
├── Engine/
│   └── ChatEngine.cs               # IChatEngine implementation — all chat operations
├── Interceptors/
│   └── InterceptorPipeline.cs      # Runs IMessageInterceptor list in order
└── Services/
    └── SystemClock.cs              # IClock → DateTime.UtcNow
```

`ChatEngine` is the heart of the library. It coordinates repositories, the interceptor pipeline, and the transport dispatcher for every operation: send, read, paginate, delete.

**Key design rule:** `ChatCore.Core` has zero knowledge of EF Core, SignalR, or ASP.NET Core. All infrastructure concerns are injected via the abstractions.

---

### `ChatCore.Persistence.EFCore`
**Target:** `net8.0` | **Role:** SQL Server persistence via Entity Framework Core 8.

```
ChatCore.Persistence.EFCore/
├── ChatCoreDbContext.cs            # EF Core DbContext — registers all DbSets
├── Configurations/
│   ├── ChatMessageConfiguration.cs     # Composite PK (ConversationId, SequenceNumber)
│   ├── ConversationConfiguration.cs    # Composite PK (Id, TenantId) + RowVersion
│   ├── MessageReadConfiguration.cs     # Composite PK (ConversationId, UserId)
│   ├── ParticipantConfiguration.cs     # Composite PK (ConversationId, UserId)
│   └── UserConnectionConfiguration.cs  # Composite PK (UserId, ConnectionId)
└── Repositories/
    ├── ConversationRepository.cs
    ├── MessageRepository.cs            # Seek-based pagination implementation
    ├── ReadReceiptRepository.cs        # Upsert pattern for read receipts
    └── UserConnectionRepository.cs
```

**NuGet dependencies:**
- `Microsoft.EntityFrameworkCore` 8.0.0
- `Microsoft.EntityFrameworkCore.SqlServer` 8.0.0
- `Microsoft.EntityFrameworkCore.Tools` 8.0.0

**Key design rule:** All repository classes implement interfaces from `ChatCore.Abstractions`. The EF Core `DbContext` is an internal implementation detail — callers never reference it directly.

---

### `ChatCore.RealTime.SignalR`
**Target:** `net8.0` | **Role:** Real-time WebSocket transport via ASP.NET Core SignalR.

```
ChatCore.RealTime.SignalR/
├── Hubs/
│   └── ChatHub.cs                  # SignalR hub — client connections, send, read, typing
├── Presence/
│   └── DatabasePresenceProvider.cs # IPresenceProvider backed by UserConnectionRepository
└── Transport/
    └── SignalRTransportDispatcher.cs  # ITransportDispatcher — broadcasts via IHubContext
```

**NuGet dependencies:**
- `Microsoft.AspNetCore.SignalR` 1.1.0

**Key design rule:** The hub depends on `IChatEngine` and `IPresenceProvider` only — never on EF Core directly. The `SignalRTransportDispatcher` uses `IHubContext<ChatHub>` so it can broadcast from outside the hub lifecycle.

---

### `ChatCore.AspNetCore`
**Target:** `net8.0` | **Role:** ASP.NET Core integration — DI registration and endpoint mapping.

```
ChatCore.AspNetCore/
└── Extensions/
    ├── ServiceCollectionExtensions.cs    # AddChatCore() entry point
    ├── ChatCoreBuilder.cs                # Fluent builder: UseEntityFramework, UseSignalR, AddInterceptor, Build
    └── EndpointRouteBuilderExtensions.cs # MapChatHub(pattern) helper
```

**NuGet dependencies:**
- `Microsoft.AspNetCore.App` 8.0.0

**References:** All four other src projects.

This is the only project a consuming application needs to reference directly. It pulls in the full dependency graph.

---

## 🧪 Test Projects

### `ChatCore.Tests.Unit`
**Target:** `net8.0` | **References:** `ChatCore.Abstractions`, `ChatCore.Core`

```
ChatCore.Tests.Unit/
├── Domain/
│   └── DomainModelTests.cs         # Entity behaviour tests (no mocks needed)
└── Engine/
    ├── ChatEngineTests.cs           # Full ChatEngine coverage with mocked repos
    └── InterceptorPipelineTests.cs  # Pipeline ordering and cancellation
```

**NuGet dependencies:** `xunit`, `xunit.runner.visualstudio`, `Moq`

---

### `ChatCore.Tests.Integration`
**Target:** `net8.0` | **References:** `ChatCore.Abstractions`, `ChatCore.Core`, `ChatCore.Persistence.EFCore`

```
ChatCore.Tests.Integration/
└── Database/
    ├── ConversationRepositoryTests.cs
    ├── MessageRepositoryTests.cs
    └── ReadReceiptRepositoryTests.cs
```

**NuGet dependencies:** `xunit`, `xunit.runner.visualstudio`, `Microsoft.EntityFrameworkCore.InMemory`

Each test class spins up a fresh in-memory database (unique name per class) via `IAsyncLifetime` to guarantee full isolation.

---

## 🔗 Dependency Graph

```
                    ┌─────────────────────────┐
                    │   ChatCore.Abstractions  │  ← No ChatCore dependencies
                    └────────────┬────────────┘
                                 │  referenced by all
          ┌──────────────────────┼────────────────────────┐
          │                      │                         │
          ▼                      ▼                         ▼
┌─────────────────┐  ┌──────────────────────┐  ┌──────────────────────────┐
│  ChatCore.Core  │  │ ChatCore.Persistence  │  │  ChatCore.RealTime       │
│                 │  │ .EFCore               │  │  .SignalR                │
└────────┬────────┘  └──────────┬───────────┘  └──────────┬───────────────┘
         │                      │                          │
         └──────────────────────┴──────────────────────────┘
                                 │  all wired together by
                                 ▼
                    ┌─────────────────────────┐
                    │  ChatCore.AspNetCore     │  ← Only project app needs
                    └─────────────────────────┘
```

---

## 📐 File & Namespace Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Namespaces | Match folder path | `ChatCore.Persistence.EFCore.Repositories` |
| Classes | PascalCase | `ConversationRepository` |
| Interfaces | `I` prefix + PascalCase | `IConversationRepository` |
| Private fields | `_` prefix + camelCase | `_conversationRepository` |
| Async methods | `Async` suffix | `GetByIdAsync` |
| Test classes | `{Subject}Tests` | `ChatEngineTests` |
| Test methods | `Method_Scenario_Expected` | `SendAsync_WithEmptyContent_ReturnsFail` |
| Config classes | `{Entity}Configuration` | `ChatMessageConfiguration` |

---

## 🗂️ Solution File

The solution uses the newer `.slnx` (XML) format organised into two virtual folders:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/ChatCore.Abstractions/..." />
    <Project Path="src/ChatCore.AspNetCore/..." />
    <Project Path="src/ChatCore.Core/..." />
    <Project Path="src/ChatCore.Persistence.EFCore/..." />
    <Project Path="src/ChatCore.RealTime.SignalR/..." />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/ChatCore.Tests.Integration/..." />
    <Project Path="tests/ChatCore.Tests.Unit/..." />
  </Folder>
</Solution>
```

Open with Visual Studio 2022 17.8+ or the `dotnet` CLI — earlier versions do not support `.slnx`.

---

## 📚 See Also

- [01-architecture-overview.md](./01-architecture-overview.md) — Layered architecture and data flow
- [03-quick-start-guide.md](./03-quick-start-guide.md) — Get it running in your app
- [04-domain-models.md](./04-domain-models.md) — Entity and DTO reference
- [07-database-schema.md](./07-database-schema.md) — Table definitions and indexes
- [08-testing-strategies.md](./08-testing-strategies.md) — Test patterns and coverage guide

---

**Last Updated:** June 13, 2026
