# Changelog

All notable changes to ChatCore.NET are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
This project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] - 2026-06-13

### Added
- `ChatCore.Abstractions` — domain entities (`Conversation`, `ChatMessage`, `MessageRead`, `Participant`, `UserConnection`), all repository interfaces, `IChatEngine`, `IMessageInterceptor`, `IPresenceProvider`, `ITransportDispatcher`, `IClock`, result types (`ChatResult<T>`, `PaginatedResult<T>`), DTOs, request/query models
- `ChatCore.Core` — `ChatEngine` implementation with full send/read/paginate/delete orchestration, `InterceptorPipeline`, `SystemClock`
- `ChatCore.Persistence.EFCore` — SQL Server persistence via EF Core 8: `ChatCoreDbContext`, all entity configurations, `ConversationRepository`, `MessageRepository`, `ReadReceiptRepository`, `UserConnectionRepository` with seek-based pagination and `GetDistinctOnlineUserIdsAsync`
- `ChatCore.RealTime.SignalR` — `ChatHub` (send, read, join/leave, typing indicators), `SignalRTransportDispatcher` with proper `GroupExcept` sender exclusion, `DatabasePresenceProvider`
- `ChatCore.AspNetCore` — `AddChatCore()` fluent builder, `UseEntityFramework()`, `UseSignalR()`, `AddInterceptor<T>()`, `MapChatHub()`, `UseChatCoreExceptionHandler()` global exception middleware
- `Directory.Build.props` — centralised version, authors, license, Source Link, and package metadata for all projects
- EF Core migration `AddTenantIdToMessageRead` — adds `TenantId` column to `MessageReads` table and fixes composite FK for tenant isolation
- CI workflow — build and test on every push and pull request
- Publish workflow — pack and push all packages to NuGet.org on version tag

### Security
- `MessageRead.TenantId` added and FK to `Conversation` updated to composite `(ConversationId, TenantId)` — prevents cross-tenant read receipt references
- `ChatCoreBuilder.Build()` now throws `InvalidOperationException` with a clear message if `UseEntityFramework` or `UseSignalR` was not called, preventing silent DI failures at runtime

### Fixed
- `SignalRTransportDispatcher` — `excludeUserId` parameter now actually excludes the sender using `GroupExcept`; previously both branches sent to all clients
- `GetOnlineUsersAsync` — was always returning empty; now delegates to `GetDistinctOnlineUserIdsAsync` which queries distinct user IDs
- `GetConversationsAsync` — `ConversationDto.ParticipantIds` now populated via a single batch query; previously always empty
- `ChatEngine.MarkAsReadAsync` — `TenantId` now passed through to `MessageRead` constructor
- `Debug.WriteLine` in `SignalRTransportDispatcher` replaced with `ILogger`

[1.0.0]: https://github.com/CalebTek/ChatCore.NET/releases/tag/v1.0.0
