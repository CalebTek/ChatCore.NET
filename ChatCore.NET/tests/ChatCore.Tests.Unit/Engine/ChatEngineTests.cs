namespace ChatCore.Tests.Unit.Engine;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Domain.Enums;
using ChatCore.Abstractions.Engine;
using ChatCore.Abstractions.Interceptors;
using ChatCore.Abstractions.Queries;
using ChatCore.Abstractions.Repositories;
using ChatCore.Abstractions.Requests;
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
    private readonly Mock<IMessageRepository> _mockMessages;
    private readonly Mock<IReadReceiptRepository> _mockReads;
    private readonly Mock<IUserConnectionRepository> _mockConnections;
    private readonly Mock<ITransportDispatcher> _mockDispatcher;
    private readonly IClock _clock;
    private readonly IChatEngine _engine;

    public ChatEngineTests()
    {
        _mockConversations = new Mock<IConversationRepository>();
        _mockMessages = new Mock<IMessageRepository>();
        _mockReads = new Mock<IReadReceiptRepository>();
        _mockConnections = new Mock<IUserConnectionRepository>();
        _mockDispatcher = new Mock<ITransportDispatcher>();
        _clock = new SystemClock();

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

    [Fact]
    public async Task SendAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var request = new ChatMessageRequest
        {
            ConversationId = conversationId,
            SenderId = userId,
            TenantId = tenantId,
            Content = "Hello, World!"
        };

        var conversation = new Conversation(conversationId, ConversationType.Direct, tenantId, _clock.UtcNow);

        _mockConversations
            .Setup(x => x.IsUserParticipantAsync(conversationId, userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockConversations
            .Setup(x => x.GetByIdAsync(conversationId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _mockMessages
            .Setup(x => x.CreateAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockConversations
            .Setup(x => x.UpdateAsync(conversation, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _engine.SendAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(userId, result.Data.SenderId);
    }

    [Fact]
    public async Task SendAsync_WithEmptyContent_ReturnsFail()
    {
        // Arrange
        var request = new ChatMessageRequest
        {
            ConversationId = Guid.NewGuid(),
            SenderId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Content = ""
        };

        // Act
        var result = await _engine.SendAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("EMPTY_CONTENT", result.ErrorCode);
    }

    [Fact]
    public async Task SendAsync_UserNotParticipant_ReturnsFail()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var request = new ChatMessageRequest
        {
            ConversationId = conversationId,
            SenderId = userId,
            TenantId = tenantId,
            Content = "Hello"
        };

        _mockConversations
            .Setup(x => x.IsUserParticipantAsync(conversationId, userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _engine.SendAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_PARTICIPANT", result.ErrorCode);
    }

    [Fact]
    public async Task SendAsync_WithIdempotencyKey_DeduplicatesMessages()
    {
        // Arrange
        var idempotencyKey = "test-key";
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var existingMessage = new ChatMessage(
            Guid.NewGuid(),
            conversationId,
            userId,
            tenantId,
            "Hello",
            1,
            _clock.UtcNow,
            idempotencyKey);

        var request = new ChatMessageRequest
        {
            ConversationId = conversationId,
            SenderId = userId,
            TenantId = tenantId,
            Content = "Hello",
            IdempotencyKey = idempotencyKey
        };

        _mockMessages
            .Setup(x => x.GetByIdempotencyKeyAsync(idempotencyKey, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMessage);

        // Act
        var result = await _engine.SendAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(existingMessage.Id, result.Data!.Id);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new MarkAsReadRequest
        {
            ConversationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            LastReadSequence = 5,
            TenantId = Guid.NewGuid()
        };

        _mockReads
            .Setup(x => x.CreateOrUpdateAsync(It.IsAny<MessageRead>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _engine.MarkAsReadAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
    }

    [Fact]
    public async Task DeleteMessageAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var message = new ChatMessage(
            messageId,
            Guid.NewGuid(),
            userId,
            tenantId,
            "Test",
            1,
            _clock.UtcNow);

        _mockMessages
            .Setup(x => x.GetByIdAsync(messageId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        _mockMessages
            .Setup(x => x.UpdateAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _engine.DeleteMessageAsync(messageId, userId, tenantId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
    }

    [Fact]
    public async Task DeleteMessageAsync_NotSender_ReturnsFail()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var message = new ChatMessage(
            messageId,
            Guid.NewGuid(),
            senderId,
            tenantId,
            "Test",
            1,
            _clock.UtcNow);

        _mockMessages
            .Setup(x => x.GetByIdAsync(messageId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        // Act
        var result = await _engine.DeleteMessageAsync(messageId, userId, tenantId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.ErrorCode);
    }
}
