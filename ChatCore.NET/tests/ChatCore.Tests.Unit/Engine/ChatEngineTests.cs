namespace ChatCore.Tests.Unit.Engine;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Domain.Enums;
using ChatCore.Abstractions.DTOs;
using ChatCore.Abstractions.Engine;
using ChatCore.Abstractions.Interceptors;
using ChatCore.Abstractions.Queries;
using ChatCore.Abstractions.Repositories;
using ChatCore.Abstractions.Requests;
using ChatCore.Abstractions.Results;
using ChatCore.Abstractions.Services;
using ChatCore.Abstractions.Transport;
using ChatCore.Core.Engine;
using ChatCore.Core.Interceptors;
using ChatCore.Core.Services;
using Moq;
using Xunit;

public class ChatEngineTests
{
    private readonly Mock<IConversationRepository> _mockConversations;
    private readonly Mock<IMessageRepository>      _mockMessages;
    private readonly Mock<IReadReceiptRepository>  _mockReads;
    private readonly Mock<IUserConnectionRepository> _mockConnections;
    private readonly Mock<ITransportDispatcher>    _mockDispatcher;
    private readonly IClock                        _clock;
    private readonly IChatEngine                   _engine;

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
            _mockConversations.Object,
            _mockMessages.Object,
            _mockReads.Object,
            _mockConnections.Object,
            _mockDispatcher.Object,
            pipeline,
            _clock);
    }

    // =========================================================================
    // SendAsync
    // =========================================================================

    [Fact]
    public async Task SendAsync_WithValidRequest_ReturnsSuccess()
    {
        var (conversationId, userId, tenantId) = NewIds();
        var conversation = new Conversation(conversationId, ConversationType.Direct, tenantId, _clock.UtcNow);
        var request = BuildSendRequest(conversationId, userId, tenantId, "Hello, World!");

        SetupParticipant(conversationId, userId, tenantId, true);
        SetupConversation(conversationId, tenantId, conversation);
        _mockMessages.Setup(x => x.CreateAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        _mockConversations.Setup(x => x.UpdateAsync(conversation, It.IsAny<CancellationToken>()))
                          .Returns(Task.CompletedTask);

        var result = await _engine.SendAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(userId,         result.Data.SenderId);
        Assert.Equal(conversationId, result.Data.ConversationId);
        Assert.Equal("Hello, World!", result.Data.Content);
        Assert.Equal(1,              result.Data.SequenceNumber);
        Assert.False(result.Data.IsDeleted);
    }

    [Fact]
    public async Task SendAsync_AssignsSequenceNumberFromConversation()
    {
        var (conversationId, userId, tenantId) = NewIds();
        var conversation = new Conversation(conversationId, ConversationType.Direct, tenantId, _clock.UtcNow);
        // Advance the sequence so we can assert the value
        conversation.NextSequenceNumber(); // now at 1
        var request = BuildSendRequest(conversationId, userId, tenantId, "msg");

        SetupParticipant(conversationId, userId, tenantId, true);
        SetupConversation(conversationId, tenantId, conversation);
        _mockMessages.Setup(x => x.CreateAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        _mockConversations.Setup(x => x.UpdateAsync(conversation, It.IsAny<CancellationToken>()))
                          .Returns(Task.CompletedTask);

        var result = await _engine.SendAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.SequenceNumber); // was 1, engine bumps it to 2
    }

    [Fact]
    public async Task SendAsync_NullRequest_ReturnsInvalidRequest()
    {
        var result = await _engine.SendAsync(null!);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_REQUEST", result.ErrorCode);
    }

    [Fact]
    public async Task SendAsync_WithEmptyContent_ReturnsEmptyContent()
    {
        var request = BuildSendRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "");

        var result = await _engine.SendAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("EMPTY_CONTENT", result.ErrorCode);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task SendAsync_WithWhitespaceContent_ReturnsEmptyContent(string whitespace)
    {
        var request = BuildSendRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), whitespace);

        var result = await _engine.SendAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("EMPTY_CONTENT", result.ErrorCode);
    }

    [Fact]
    public async Task SendAsync_EmptyConversationId_ReturnsInvalidConversationId()
    {
        var request = BuildSendRequest(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "hello");

        var result = await _engine.SendAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CONVERSATION_ID", result.ErrorCode);
    }

    [Fact]
    public async Task SendAsync_EmptySenderId_ReturnsInvalidSenderId()
    {
        var request = BuildSendRequest(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "hello");

        var result = await _engine.SendAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_SENDER_ID", result.ErrorCode);
    }

    [Fact]
    public async Task SendAsync_UserNotParticipant_ReturnsNotParticipant()
    {
        var (conversationId, userId, tenantId) = NewIds();
        SetupParticipant(conversationId, userId, tenantId, false);

        var result = await _engine.SendAsync(BuildSendRequest(conversationId, userId, tenantId, "hello"));

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_PARTICIPANT", result.ErrorCode);
    }

    [Fact]
    public async Task SendAsync_ConversationNotFound_ReturnsConversationNotFound()
    {
        var (conversationId, userId, tenantId) = NewIds();
        SetupParticipant(conversationId, userId, tenantId, true);
        _mockConversations.Setup(x => x.GetByIdAsync(conversationId, tenantId, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((Conversation?)null);

        var result = await _engine.SendAsync(BuildSendRequest(conversationId, userId, tenantId, "hello"));

        Assert.False(result.IsSuccess);
        Assert.Equal("CONVERSATION_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task SendAsync_WithIdempotencyKey_DeduplicatesAndReturnsExisting()
    {
        var (conversationId, userId, tenantId) = NewIds();
        var key = "idem-key-xyz";
        var existing = new ChatMessage(Guid.NewGuid(), conversationId, userId, tenantId, "Hello", 1, _clock.UtcNow, key);

        _mockMessages.Setup(x => x.GetByIdempotencyKeyAsync(key, tenantId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(existing);

        var request = BuildSendRequest(conversationId, userId, tenantId, "Hello");
        request.IdempotencyKey = key;

        var result = await _engine.SendAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(existing.Id, result.Data!.Id);
        // Repos must NOT have been called beyond the idempotency lookup
        _mockConversations.Verify(x => x.IsUserParticipantAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_InterceptorCancels_ReturnsMessageCancelled()
    {
        var (conversationId, userId, tenantId) = NewIds();
        var conversation = new Conversation(conversationId, ConversationType.Direct, tenantId, _clock.UtcNow);

        SetupParticipant(conversationId, userId, tenantId, true);
        SetupConversation(conversationId, tenantId, conversation);

        var cancellingInterceptor = new Mock<IMessageInterceptor>();
        cancellingInterceptor
            .Setup(x => x.OnBeforeSendAsync(It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
            .Callback<MessageContext, CancellationToken>((ctx, _) =>
            {
                ctx.IsCancelled        = true;
                ctx.CancellationReason = "Blocked by policy";
            })
            .Returns(Task.CompletedTask);
        cancellingInterceptor
            .Setup(x => x.OnAfterSendAsync(It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pipeline = new InterceptorPipeline(new[] { cancellingInterceptor.Object });
        var engine   = BuildEngine(pipeline);

        var result = await engine.SendAsync(BuildSendRequest(conversationId, userId, tenantId, "hello"));

        Assert.False(result.IsSuccess);
        Assert.Equal("MESSAGE_CANCELLED", result.ErrorCode);
        Assert.Equal("Blocked by policy", result.Error);
        // Message must never be persisted
        _mockMessages.Verify(x => x.CreateAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_PersistenceThrows_ReturnsSendFailed()
    {
        var (conversationId, userId, tenantId) = NewIds();
        var conversation = new Conversation(conversationId, ConversationType.Direct, tenantId, _clock.UtcNow);

        SetupParticipant(conversationId, userId, tenantId, true);
        SetupConversation(conversationId, tenantId, conversation);
        _mockMessages.Setup(x => x.CreateAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new InvalidOperationException("DB unavailable"));

        var result = await _engine.SendAsync(BuildSendRequest(conversationId, userId, tenantId, "hello"));

        Assert.False(result.IsSuccess);
        Assert.Equal("SEND_FAILED", result.ErrorCode);
    }

    // =========================================================================
    // MarkAsReadAsync
    // =========================================================================

    [Fact]
    public async Task MarkAsReadAsync_WithValidRequest_ReturnsSuccess()
    {
        var request = new MarkAsReadRequest
        {
            ConversationId   = Guid.NewGuid(),
            UserId           = Guid.NewGuid(),
            TenantId         = Guid.NewGuid(),
            LastReadSequence = 5
        };

        _mockReads.Setup(x => x.CreateOrUpdateAsync(It.IsAny<MessageRead>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        var result = await _engine.MarkAsReadAsync(request);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
    }

    [Fact]
    public async Task MarkAsReadAsync_PassesTenantIdToReadReceipt()
    {
        var tenantId = Guid.NewGuid();
        MessageRead? captured = null;

        _mockReads.Setup(x => x.CreateOrUpdateAsync(It.IsAny<MessageRead>(), It.IsAny<CancellationToken>()))
                  .Callback<MessageRead, CancellationToken>((r, _) => captured = r)
                  .Returns(Task.CompletedTask);

        var request = new MarkAsReadRequest
        {
            ConversationId   = Guid.NewGuid(),
            UserId           = Guid.NewGuid(),
            TenantId         = tenantId,
            LastReadSequence = 3
        };

        await _engine.MarkAsReadAsync(request);

        Assert.NotNull(captured);
        Assert.Equal(tenantId, captured!.TenantId);
    }

    [Fact]
    public async Task MarkAsReadAsync_NullRequest_ReturnsInvalidRequest()
    {
        var result = await _engine.MarkAsReadAsync(null!);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_REQUEST", result.ErrorCode);
    }

    [Fact]
    public async Task MarkAsReadAsync_EmptyConversationId_ReturnsInvalidConversationId()
    {
        var result = await _engine.MarkAsReadAsync(new MarkAsReadRequest
        {
            ConversationId = Guid.Empty,
            UserId         = Guid.NewGuid(),
            TenantId       = Guid.NewGuid()
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CONVERSATION_ID", result.ErrorCode);
    }

    [Fact]
    public async Task MarkAsReadAsync_EmptyUserId_ReturnsInvalidUserId()
    {
        var result = await _engine.MarkAsReadAsync(new MarkAsReadRequest
        {
            ConversationId = Guid.NewGuid(),
            UserId         = Guid.Empty,
            TenantId       = Guid.NewGuid()
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_USER_ID", result.ErrorCode);
    }

    [Fact]
    public async Task MarkAsReadAsync_RepositoryThrows_ReturnsMarkReadFailed()
    {
        _mockReads.Setup(x => x.CreateOrUpdateAsync(It.IsAny<MessageRead>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new Exception("DB error"));

        var result = await _engine.MarkAsReadAsync(new MarkAsReadRequest
        {
            ConversationId   = Guid.NewGuid(),
            UserId           = Guid.NewGuid(),
            TenantId         = Guid.NewGuid(),
            LastReadSequence = 1
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("MARK_READ_FAILED", result.ErrorCode);
    }

    // =========================================================================
    // GetMessagesAsync
    // =========================================================================

    [Fact]
    public async Task GetMessagesAsync_WithValidQuery_ReturnsMessages()
    {
        var (conversationId, _, tenantId) = NewIds();
        var messages = new List<ChatMessage>
        {
            new(Guid.NewGuid(), conversationId, Guid.NewGuid(), tenantId, "msg 1", 1, DateTime.UtcNow),
            new(Guid.NewGuid(), conversationId, Guid.NewGuid(), tenantId, "msg 2", 2, DateTime.UtcNow)
        };
        var paginated = new PaginatedResult<ChatMessage>(messages, false, null);

        _mockMessages.Setup(x => x.GetByConversationIdAsync(
                conversationId, tenantId, long.MaxValue, 50, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(paginated);

        var result = await _engine.GetMessagesAsync(new GetMessagesQuery
        {
            ConversationId   = conversationId,
            TenantId         = tenantId,
            LastSeenSequence = long.MaxValue,
            PageSize         = 50
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
        Assert.False(result.Data.HasMore);
    }

    [Fact]
    public async Task GetMessagesAsync_ReturnsHasMoreWhenMoreExist()
    {
        var (conversationId, _, tenantId) = NewIds();
        var messages = Enumerable.Range(1, 3).Select(i =>
            new ChatMessage(Guid.NewGuid(), conversationId, Guid.NewGuid(), tenantId, $"msg {i}", i, DateTime.UtcNow))
            .ToList();
        var paginated = new PaginatedResult<ChatMessage>(messages, true, 1);

        _mockMessages.Setup(x => x.GetByConversationIdAsync(
                conversationId, tenantId, It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(paginated);

        var result = await _engine.GetMessagesAsync(new GetMessagesQuery
        {
            ConversationId = conversationId,
            TenantId       = tenantId
        });

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.HasMore);
        Assert.Equal(1, result.Data.NextCursor);
    }

    [Fact]
    public async Task GetMessagesAsync_NullQuery_ReturnsInvalidRequest()
    {
        var result = await _engine.GetMessagesAsync(null!);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_REQUEST", result.ErrorCode);
    }

    [Fact]
    public async Task GetMessagesAsync_EmptyConversationId_ReturnsInvalidConversationId()
    {
        var result = await _engine.GetMessagesAsync(new GetMessagesQuery { ConversationId = Guid.Empty });

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CONVERSATION_ID", result.ErrorCode);
    }

    [Fact]
    public async Task GetMessagesAsync_RepositoryThrows_ReturnsGetMessagesFailed()
    {
        var (conversationId, _, tenantId) = NewIds();

        _mockMessages.Setup(x => x.GetByConversationIdAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("DB error"));

        var result = await _engine.GetMessagesAsync(new GetMessagesQuery
        {
            ConversationId = conversationId,
            TenantId       = tenantId
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("GET_MESSAGES_FAILED", result.ErrorCode);
    }

    // =========================================================================
    // GetConversationsAsync
    // =========================================================================

    [Fact]
    public async Task GetConversationsAsync_WithValidQuery_ReturnsConversations()
    {
        var (_, userId, tenantId) = NewIds();
        var convId1 = Guid.NewGuid();
        var convId2 = Guid.NewGuid();
        var p1      = Guid.NewGuid();
        var p2      = Guid.NewGuid();

        var conversations = new List<Conversation>
        {
            new(convId1, ConversationType.Direct, tenantId, DateTime.UtcNow),
            new(convId2, ConversationType.Group,  tenantId, DateTime.UtcNow)
        };

        var participantMap = new Dictionary<Guid, List<Guid>>
        {
            { convId1, new List<Guid> { userId, p1 } },
            { convId2, new List<Guid> { userId, p2 } }
        };

        _mockConversations.Setup(x => x.GetByUserIdAsync(userId, tenantId, 0, 20, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(conversations);
        _mockConversations.Setup(x => x.GetParticipantIdsByConversationIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(participantMap);

        var result = await _engine.GetConversationsAsync(new GetConversationsQuery
        {
            UserId   = userId,
            TenantId = tenantId,
            Page     = 1,
            PageSize = 20
        });

        Assert.True(result.IsSuccess);
        var dtos = result.Data!.ToList();
        Assert.Equal(2, dtos.Count);

        var dto1 = dtos.Single(d => d.Id == convId1);
        Assert.Contains(userId, dto1.ParticipantIds);
        Assert.Contains(p1,     dto1.ParticipantIds);

        var dto2 = dtos.Single(d => d.Id == convId2);
        Assert.Contains(userId, dto2.ParticipantIds);
        Assert.Contains(p2,     dto2.ParticipantIds);
    }

    [Fact]
    public async Task GetConversationsAsync_ConversationWithNoParticipants_ReturnsEmptyParticipantIds()
    {
        var (_, userId, tenantId) = NewIds();
        var convId = Guid.NewGuid();

        _mockConversations.Setup(x => x.GetByUserIdAsync(userId, tenantId, 0, 20, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new List<Conversation> { new(convId, ConversationType.Direct, tenantId, DateTime.UtcNow) });
        // Participant map omits this conversation entirely
        _mockConversations.Setup(x => x.GetParticipantIdsByConversationIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new Dictionary<Guid, List<Guid>>());

        var result = await _engine.GetConversationsAsync(new GetConversationsQuery
        {
            UserId = userId, TenantId = tenantId, Page = 1, PageSize = 20
        });

        Assert.True(result.IsSuccess);
        var dto = result.Data!.Single();
        Assert.Empty(dto.ParticipantIds);
    }

    [Fact]
    public async Task GetConversationsAsync_NullQuery_ReturnsInvalidRequest()
    {
        var result = await _engine.GetConversationsAsync(null!);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_REQUEST", result.ErrorCode);
    }

    [Fact]
    public async Task GetConversationsAsync_EmptyUserId_ReturnsInvalidUserId()
    {
        var result = await _engine.GetConversationsAsync(new GetConversationsQuery { UserId = Guid.Empty });

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_USER_ID", result.ErrorCode);
    }

    [Fact]
    public async Task GetConversationsAsync_RepositoryThrows_ReturnsGetConversationsFailed()
    {
        _mockConversations.Setup(x => x.GetByUserIdAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                          .ThrowsAsync(new Exception("DB error"));

        var result = await _engine.GetConversationsAsync(new GetConversationsQuery
        {
            UserId = Guid.NewGuid(), TenantId = Guid.NewGuid()
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("GET_CONVERSATIONS_FAILED", result.ErrorCode);
    }

    [Fact]
    public async Task GetConversationsAsync_UsesCorrectSkipForPage2()
    {
        var (_, userId, tenantId) = NewIds();
        int capturedSkip = -1, capturedTake = -1;

        _mockConversations.Setup(x => x.GetByUserIdAsync(
                userId, tenantId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                          .Callback<Guid, Guid, int, int, CancellationToken>((_, _, s, t, _) =>
                          {
                              capturedSkip = s;
                              capturedTake = t;
                          })
                          .ReturnsAsync(new List<Conversation>());
        _mockConversations.Setup(x => x.GetParticipantIdsByConversationIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new Dictionary<Guid, List<Guid>>());

        await _engine.GetConversationsAsync(new GetConversationsQuery
        {
            UserId = userId, TenantId = tenantId, Page = 2, PageSize = 10
        });

        Assert.Equal(10, capturedSkip); // (page-1) * pageSize = 1*10
        Assert.Equal(10, capturedTake);
    }

    // =========================================================================
    // DeleteMessageAsync
    // =========================================================================

    [Fact]
    public async Task DeleteMessageAsync_WithValidRequest_ReturnsSuccess()
    {
        var (_, userId, tenantId) = NewIds();
        var messageId = Guid.NewGuid();
        var message = new ChatMessage(messageId, Guid.NewGuid(), userId, tenantId, "Test", 1, _clock.UtcNow);

        _mockMessages.Setup(x => x.GetByIdAsync(messageId, tenantId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(message);
        _mockMessages.Setup(x => x.UpdateAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

        var result = await _engine.DeleteMessageAsync(messageId, userId, tenantId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
    }

    [Fact]
    public async Task DeleteMessageAsync_MessageIsSoftDeleted_AfterCall()
    {
        var (_, userId, tenantId) = NewIds();
        var messageId = Guid.NewGuid();
        var message = new ChatMessage(messageId, Guid.NewGuid(), userId, tenantId, "Original", 1, _clock.UtcNow);

        _mockMessages.Setup(x => x.GetByIdAsync(messageId, tenantId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(message);
        _mockMessages.Setup(x => x.UpdateAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

        await _engine.DeleteMessageAsync(messageId, userId, tenantId);

        Assert.True(message.IsDeleted);
        Assert.Equal("[deleted]", message.Content);
    }

    [Fact]
    public async Task DeleteMessageAsync_EmptyMessageId_ReturnsInvalidMessageId()
    {
        var result = await _engine.DeleteMessageAsync(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_MESSAGE_ID", result.ErrorCode);
    }

    [Fact]
    public async Task DeleteMessageAsync_EmptyUserId_ReturnsInvalidUserId()
    {
        var result = await _engine.DeleteMessageAsync(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_USER_ID", result.ErrorCode);
    }

    [Fact]
    public async Task DeleteMessageAsync_MessageNotFound_ReturnsMessageNotFound()
    {
        var messageId = Guid.NewGuid();
        var tenantId  = Guid.NewGuid();
        _mockMessages.Setup(x => x.GetByIdAsync(messageId, tenantId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((ChatMessage?)null);

        var result = await _engine.DeleteMessageAsync(messageId, Guid.NewGuid(), tenantId);

        Assert.False(result.IsSuccess);
        Assert.Equal("MESSAGE_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task DeleteMessageAsync_NotSender_ReturnsUnauthorized()
    {
        var (_, senderId, tenantId) = NewIds();
        var messageId = Guid.NewGuid();
        var message = new ChatMessage(messageId, Guid.NewGuid(), senderId, tenantId, "Test", 1, _clock.UtcNow);

        _mockMessages.Setup(x => x.GetByIdAsync(messageId, tenantId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(message);

        // A different user tries to delete
        var result = await _engine.DeleteMessageAsync(messageId, Guid.NewGuid(), tenantId);

        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.ErrorCode);
    }

    [Fact]
    public async Task DeleteMessageAsync_RepositoryThrows_ReturnsDeleteFailed()
    {
        var (_, userId, tenantId) = NewIds();
        var messageId = Guid.NewGuid();
        var message = new ChatMessage(messageId, Guid.NewGuid(), userId, tenantId, "Test", 1, _clock.UtcNow);

        _mockMessages.Setup(x => x.GetByIdAsync(messageId, tenantId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(message);
        _mockMessages.Setup(x => x.UpdateAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("DB error"));

        var result = await _engine.DeleteMessageAsync(messageId, userId, tenantId);

        Assert.False(result.IsSuccess);
        Assert.Equal("DELETE_FAILED", result.ErrorCode);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static (Guid conversationId, Guid userId, Guid tenantId) NewIds() =>
        (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    private static ChatMessageRequest BuildSendRequest(
        Guid conversationId, Guid userId, Guid tenantId, string content) => new()
    {
        ConversationId = conversationId,
        SenderId       = userId,
        TenantId       = tenantId,
        Content        = content
    };

    private void SetupParticipant(Guid conversationId, Guid userId, Guid tenantId, bool result) =>
        _mockConversations.Setup(x => x.IsUserParticipantAsync(
                conversationId, userId, tenantId, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(result);

    private void SetupConversation(Guid conversationId, Guid tenantId, Conversation conversation) =>
        _mockConversations.Setup(x => x.GetByIdAsync(conversationId, tenantId, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(conversation);

    private IChatEngine BuildEngine(IInterceptorPipeline pipeline) => new ChatEngine(
        _mockConversations.Object,
        _mockMessages.Object,
        _mockReads.Object,
        _mockConnections.Object,
        _mockDispatcher.Object,
        pipeline,
        _clock);
}
