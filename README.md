# ChatCore.NET

> A modular, scalable, SignalR-powered chat framework for .NET

## 🎯 Overview

ChatCore.NET is an enterprise-grade, production-ready chat engine designed for .NET applications. It provides a clean, pluggable abstraction over real-time communication with support for 1:1 and group conversations, read receipts, presence tracking, and horizontal scaling.

## ✨ Features

- **Modular Architecture** - Clean separation of concerns with pluggable storage and transport layers
- **Real-Time Delivery** - Built-in SignalR integration with fallback support
- **Multi-Tenancy Ready** - Tenant isolation from the ground up
- **Message Ordering Guarantee** - Sequence-based ordering for consistency
- **Read Receipts** - Track message read status per user
- **Presence Tracking** - Online/offline status with multi-connection support
- **Soft Deletes** - Preserve message history while allowing deletion
- **Idempotency** - Prevent duplicate messages with idempotency keys
- **Extensibility** - Middleware-style interceptor pipeline for custom logic
- **Scalable** - Seek-based pagination and Redis backplane support

## 🏗️ Architecture

### Packages

- **ChatCore.Abstractions** - Core domain models, interfaces, and contracts
- **ChatCore.Core** - Engine implementation and business logic
- **ChatCore.Persistence.EFCore** - Entity Framework Core storage layer (coming soon)
- **ChatCore.RealTime.SignalR** - SignalR transport implementation (coming soon)
- **ChatCore.AspNetCore** - ASP.NET Core integration and DI setup (coming soon)

### Core Concepts

#### Conversations
- Direct (1:1) conversations
- Group conversations with unlimited participants
- Composite key: `(ConversationId, TenantId)`

#### Messages
- Atomic sequence numbering for ordering guarantee
- Soft delete support for compliance
- Idempotency key for duplicate prevention
- Seekable pagination for infinite scalability

#### Presence
- Multi-connection per user
- Online/offline tracking
- Redis-backed optional

#### Read Receipts
- Track last read sequence per user/conversation
- Efficient single-value update model

## 🚀 Quick Start

*(Coming soon - package will be available on NuGet)*

```csharp
// 1. Register services
services
    .AddChatCore()
    .UseEntityFramework<AppDbContext>()
    .UseSignalR();

// 2. Map hub endpoint
app.MapChatHub("/chat");

// 3. Inject and use
var engine = serviceProvider.GetRequiredService<IChatEngine>();
var result = await engine.SendAsync(new ChatMessageRequest
{
    ConversationId = conversationId,
    SenderId = userId,
    TenantId = tenantId,
    Content = "Hello, World!"
});
```

## 🏛️ Project Structure

```
src/
├── ChatCore.Abstractions/      # Domain models & interfaces
├── ChatCore.Core/              # Engine & implementations
├── ChatCore.Persistence.EFCore # Database layer (planned)
├── ChatCore.RealTime.SignalR   # Real-time transport (planned)
└── ChatCore.AspNetCore         # Integration layer (planned)

test/
├── ChatCore.Tests.Unit         # Unit tests
└── ChatCore.Tests.Integration  # Integration tests
```

## 🔐 Design Principles

1. **Storage Agnostic** - Swap storage implementations without changing the engine
2. **Transport Agnostic** - Support multiple real-time transport options
3. **Multi-Tenant by Default** - Every entity includes tenant isolation
4. **Ordering Guarantee** - Sequence numbers ensure message ordering
5. **Scalability First** - Seek-based pagination and stateless design
6. **Extensible** - Interceptor pipeline for cross-cutting concerns
7. **Test Friendly** - Interface-based design for dependency injection

## 📊 Database Schema (Planned)

```sql
-- Conversations
CREATE TABLE Conversations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Type INT NOT NULL,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    LastSequenceNumber BIGINT NOT NULL,
    UNIQUE (Id, TenantId)
);

-- Messages (clustered by Conversation + Sequence)
CREATE TABLE Messages (
    Id UNIQUEIDENTIFIER,
    ConversationId UNIQUEIDENTIFIER NOT NULL,
    SenderId UNIQUEIDENTIFIER NOT NULL,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    SequenceNumber BIGINT NOT NULL,
    SentAt DATETIME2 NOT NULL,
    IsDeleted BIT NOT NULL,
    IdempotencyKey NVARCHAR(100),
    PRIMARY KEY (ConversationId, SequenceNumber)
);

-- Read Receipts
CREATE TABLE MessageReads (
    ConversationId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    LastReadSequence BIGINT NOT NULL,
    ReadAt DATETIME2 NOT NULL,
    PRIMARY KEY (ConversationId, UserId)
);
```

## 📝 Roadmap

- [x] Core abstractions and domain models
- [x] ChatEngine implementation
- [ ] Entity Framework Core persistence layer
- [ ] SignalR transport implementation
- [ ] ASP.NET Core integration
- [ ] Unit and integration tests
- [ ] NuGet package release
- [ ] Redis backplane support
- [ ] Multi-tenant admin dashboard
- [ ] Message encryption layer
- [ ] Webhook support for external systems

## 🤝 Contributing

Contributions are welcome! Please follow the established architecture patterns.

## 📄 License

MIT License - see LICENSE file for details

## 👨‍💻 Author

CalebTek - [GitHub](https://github.com/CalebTek)
