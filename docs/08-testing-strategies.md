# Testing Strategies

## 🧪 Overview

ChatCore.NET uses a two-project test structure that mirrors the two distinct testing concerns in the codebase:

| Project | What it tests | Technology |
|---------|--------------|------------|
| `ChatCore.Tests.Unit` | Domain logic and the `ChatEngine` in isolation | xUnit + Moq |
| `ChatCore.Tests.Integration` | EF Core repositories against an in-memory database | xUnit + EF InMemory |

---

## 🏃 Running Tests

```bash
# All tests
dotnet test

# One project
dotnet test tests/ChatCore.Tests.Unit
dotnet test tests/ChatCore.Tests.Integration

# Single test by name
dotnet test --filter "FullyQualifiedName~SendAsync_WithValidRequest"

# With code coverage (requires coverlet)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

---

## 🔬 Unit Tests

Unit tests live in `ChatCore.Tests.Unit` and test two areas: domain model behaviour and the `ChatEngine` orchestration layer.

### Domain Model Tests (`DomainModelTests`)

Tests that domain rules are enforced by the entity classes themselves — no database or services involved.

```
tests/ChatCore.Tests.Unit/Domain/DomainModelTests.cs
```

**Covered behaviours:**

| Test | Verifies |
|------|---------|
| `Conversation_NextSequenceNumber_IncrementsProperly` | Sequence numbers start at 1 and increment atomically |
| `ChatMessage_SoftDelete_SetsDeletedAndPlaceholder` | `SoftDelete()` sets `IsDeleted = true` and replaces content with `"[deleted]"` |

**Writing domain tests — pattern:**

```csharp
[Fact]
public void Entity_Method_ExpectedBehaviour()
{
    // Arrange: construct entity using its public constructor
    var conversation = new Conversation(
        Guid.NewGuid(), ConversationType.Group, Guid.NewGuid(), DateTime.UtcNow);

    // Act: invoke domain method
    var seq = conversation.NextSequenceNumber();

    // Assert: verify state change
    Assert.Equal(1, seq);
}
```

---

### Chat Engine Tests (`ChatEngineTests`)

Tests the `ChatEngine` orchestration logic. All dependencies (repositories, dispatcher, clock) are mocked with **Moq** so tests are fast, deterministic, and focused on the engine's own logic.

```
tests/ChatCore.Tests.Unit/Engine/ChatEngineTests.cs
```

**Test fixture setup:**

```csharp
public ChatEngineTests()
{
    _mockConversations = new Mock<IConversationRepository>();
    _mockMessages      = new Mock<IMessageRepository>();
    _mockReads         = new Mock<IReadReceiptRepository>();
    _mockConnections   = new Mock<IUserConnectionRepository>();
    _mockDispatcher    = new Mock<ITransportDispatcher>();
    _clock             = new SystemClock();

    var pipeline = new InterceptorPipeline(Enumerable.Empty<IMessageInterceptor>());
    _engine = new ChatEngine(
        _mockConversations.Object, _mockMessages.Object, _mockReads.Object,
        _mockConnections.Object,   _mockDispatcher.Object, pipeline, _clock);
}
```

**Covered scenarios:**

| Test | Verifies |
|------|---------|
| `SendAsync_WithValidRequest_ReturnsSuccess` | Happy path — message is created and returned |
| `SendAsync_WithEmptyContent_ReturnsFail` | Guard clause for blank content (`EMPTY_CONTENT`) |
| `SendAsync_UserNotParticipant_ReturnsFail` | Authorization check (`NOT_PARTICIPANT`) |
| `SendAsync_WithIdempotencyKey_DeduplicatesMessages` | Duplicate send returns existing message |
| `MarkAsReadAsync_WithValidRequest_ReturnsSuccess` | Read receipt is created |
| `DeleteMessageAsync_WithValidRequest_ReturnsSuccess` | Message is soft-deleted |
| `DeleteMessageAsync_NotSender_ReturnsFail` | Only sender can delete (`UNAUTHORIZED`) |

**Pattern — happy path:**

```csharp
[Fact]
public async Task SendAsync_WithValidRequest_ReturnsSuccess()
{
    // Arrange: wire up mocks for the success path
    _mockConversations
        .Setup(x => x.IsUserParticipantAsync(conversationId, userId, tenantId, default))
        .ReturnsAsync(true);

    _mockConversations
        .Setup(x => x.GetByIdAsync(conversationId, tenantId, default))
        .ReturnsAsync(conversation);

    // Act
    var result = await _engine.SendAsync(request);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
}
```

**Pattern — guard clause / failure path:**

```csharp
[Fact]
public async Task SendAsync_WithEmptyContent_ReturnsFail()
{
    var request = new ChatMessageRequest { Content = "" /* other fields */ };

    var result = await _engine.SendAsync(request);

    Assert.False(result.IsSuccess);
    Assert.Equal("EMPTY_CONTENT", result.ErrorCode);
    // Repositories should never be called for an invalid request
    _mockConversations.VerifyNoOtherCalls();
}
```

---

### Interceptor Pipeline Tests (`InterceptorPipelineTests`)

Tests the middleware execution order and cancellation behaviour of `InterceptorPipeline`.

```
tests/ChatCore.Tests.Unit/Engine/InterceptorPipelineTests.cs
```

| Test | Verifies |
|------|---------|
| `ExecuteBeforeAsync_RunsInterceptorsInOrder` | Interceptors execute in registration order |
| `ExecuteBeforeAsync_StopsWhenCancelled` | Pipeline halts after an interceptor sets `IsCancelled = true` |

**Testing a custom interceptor:**

```csharp
[Fact]
public async Task MyInterceptor_BlocksProfanity_CancelsMessage()
{
    var interceptor = new ProfanityFilterInterceptor();
    var pipeline    = new InterceptorPipeline(new[] { interceptor });

    var context = new MessageContext
    {
        Message = new ChatMessage(/* ... "badword" ... */)
    };

    await pipeline.ExecuteBeforeAsync(context);

    Assert.True(context.IsCancelled);
    Assert.Equal("Message_CANCELLED", context.CancellationReason);
}
```

---

## 🔗 Integration Tests

Integration tests live in `ChatCore.Tests.Integration` and verify that the EF Core repository implementations correctly persist and retrieve data. They use **EF InMemory** provider with a unique database name per test class to guarantee isolation.

### Test Lifecycle

All integration test classes implement `IAsyncLifetime`:

```csharp
public async Task InitializeAsync()
{
    var options = new DbContextOptionsBuilder<ChatCoreDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())  // Unique DB per test class
        .Options;

    _context    = new ChatCoreDbContext(options);
    _repository = new ConversationRepository(_context);
    await _context.Database.EnsureCreatedAsync();
}

public async Task DisposeAsync()
{
    await _context.DisposeAsync();
}
```

### Conversation Repository Tests

```
tests/ChatCore.Tests.Integration/Database/ConversationRepositoryTests.cs
```

| Test | Verifies |
|------|---------|
| `CreateAsync_StoresConversation` | Created conversation can be retrieved by ID + tenantId |
| `IsUserParticipantAsync_ReturnsTrueWhenParticipant` | Returns `true` when participant record exists |
| `IsUserParticipantAsync_ReturnsFalseWhenNotParticipant` | Returns `false` for a user not in the conversation |

### Message Repository Tests

```
tests/ChatCore.Tests.Integration/Database/MessageRepositoryTests.cs
```

| Test | Verifies |
|------|---------|
| `CreateAsync_StoresMessage` | Message can be retrieved after creation |
| `GetByConversationIdAsync_ReturnsPaginatedResults` | Seek pagination returns correct page and sets `HasMore` |
| `GetByIdempotencyKeyAsync_ReturnsDuplicateMessage` | Idempotency key lookup returns the existing message |

### Read Receipt Repository Tests

```
tests/ChatCore.Tests.Integration/Database/ReadReceiptRepositoryTests.cs
```

| Test | Verifies |
|------|---------|
| `CreateOrUpdateAsync_CreatesNewReadReceipt` | New read receipt is stored |
| `CreateOrUpdateAsync_UpdatesExistingReadReceipt` | Subsequent call updates `LastReadSequence` rather than creating a duplicate |

---

## ✅ Test Conventions

### Naming

All test methods follow the pattern `Method_Scenario_ExpectedResult`:

```
SendAsync_WithValidRequest_ReturnsSuccess
SendAsync_WithEmptyContent_ReturnsFail
IsUserParticipantAsync_ReturnsTrueWhenParticipant
```

### AAA Structure

Every test follows **Arrange → Act → Assert**:

```csharp
[Fact]
public async Task MyTest()
{
    // Arrange
    // ...

    // Act
    var result = await _subject.DoSomethingAsync();

    // Assert
    Assert.True(result.IsSuccess);
}
```

### One Assertion Concept Per Test

Prefer one logical assertion per test. Use multiple `Assert.*` calls only when they test the same concept:

```csharp
// Acceptable — two assertions, one concept (failure shape)
Assert.False(result.IsSuccess);
Assert.Equal("NOT_PARTICIPANT", result.ErrorCode);
```

### Mock Verification

When testing that a method should **not** call a dependency, verify it explicitly:

```csharp
_mockMessages.Verify(
    x => x.CreateAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()),
    Times.Never);
```

---

## 📋 What Is Not Yet Tested

The following areas have no test coverage and are candidates for future additions:

| Area | Suggested Approach |
|------|--------------------|
| `GetConversationsAsync` in `ChatEngine` | Unit test with mocked repository |
| `GetMessagesAsync` in `ChatEngine` | Unit test with mocked repository |
| `SignalRTransportDispatcher` | Integration test with `HubContextMock` or in-memory host |
| `DatabasePresenceProvider` | Integration test with in-memory EF |
| `ChatHub` methods | Integration test using `TestServer` + SignalR test client |
| Read receipt repository `GetByConversationIdAsync` | Integration test |
| User connection repository (all methods) | Integration test |
| Interceptor pipeline `ExecuteAfterAsync` | Unit test |
| Multi-tenant isolation | Integration test — verify tenant A cannot read tenant B's data |

---

## 🛠️ Adding Tests

### Unit Test for a New Engine Method

1. Add the method to `IChatEngine` and implement in `ChatEngine`.
2. Add a test class or test method in `ChatCore.Tests.Unit/Engine/ChatEngineTests.cs`.
3. Mock all repository dependencies via the existing `_mock*` fields.
4. Follow the AAA pattern and the naming convention.

### Integration Test for a New Repository

1. Create a new `[RepoName]RepositoryTests.cs` in `ChatCore.Tests.Integration/Database/`.
2. Implement `IAsyncLifetime` with a unique in-memory database.
3. Test each public method with at least a happy path and one edge case.

### Custom Interceptor Test

1. Create the interceptor in `ChatCore.Core` (or a consuming project).
2. Add a test in the unit test project that instantiates the interceptor directly.
3. Construct a `MessageContext` with appropriate data and call `OnBeforeSendAsync` / `OnAfterSendAsync`.
4. Assert the expected mutations to `context`.

---

**See Also:** [10-contributing-guidelines.md](./10-contributing-guidelines.md) | [01-architecture-overview.md](./01-architecture-overview.md)
