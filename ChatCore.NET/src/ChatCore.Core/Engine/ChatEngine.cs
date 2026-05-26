namespace ChatCore.Core.Engine;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.DTOs;
using ChatCore.Abstractions.Engine;
using ChatCore.Abstractions.Interceptors;
using ChatCore.Abstractions.Queries;
using ChatCore.Abstractions.Repositories;
using ChatCore.Abstractions.Requests;
using ChatCore.Abstractions.Results;
using ChatCore.Abstractions.Services;
using ChatCore.Abstractions.Transport;

/// <summary>
/// Core chat engine implementation.
/// </summary>
public class ChatEngine : IChatEngine
{
    private readonly IConversationRepository _conversations;
    private readonly IMessageRepository _messages;
    private readonly IReadReceiptRepository _reads;
    private readonly IUserConnectionRepository _connections;
    private readonly ITransportDispatcher _dispatcher;
    private readonly IInterceptorPipeline _pipeline;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatEngine"/> class.
    /// </summary>
    public ChatEngine(
        IConversationRepository conversations,
        IMessageRepository messages,
        IReadReceiptRepository reads,
        IUserConnectionRepository connections,
        ITransportDispatcher dispatcher,
        IInterceptorPipeline pipeline,
        IClock clock)
    {
        _conversations = conversations;
        _messages = messages;
        _reads = reads;
        _connections = connections;
        _dispatcher = dispatcher;
        _pipeline = pipeline;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<ChatResult<ChatMessageDto>> SendAsync(ChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        // Validate input
        if (request == null)
            return ChatResult<ChatMessageDto>.Failure("Request cannot be null", "INVALID_REQUEST");

        if (string.IsNullOrWhiteSpace(request.Content))
            return ChatResult<ChatMessageDto>.Failure("Message content cannot be empty", "EMPTY_CONTENT");

        if (request.ConversationId == Guid.Empty)
            return ChatResult<ChatMessageDto>.Failure("Conversation ID is required", "INVALID_CONVERSATION_ID");

        if (request.SenderId == Guid.Empty)
            return ChatResult<ChatMessageDto>.Failure("Sender ID is required", "INVALID_SENDER_ID");

        // Check for deduplication
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingMessage = await _messages.GetByIdempotencyKeyAsync(request.IdempotencyKey, request.TenantId, cancellationToken);
            if (existingMessage != null)
            {
                return ChatResult<ChatMessageDto>.Success(MapToDto(existingMessage));
            }
        }

        // Verify sender is a participant
        var isParticipant = await _conversations.IsUserParticipantAsync(
            request.ConversationId,
            request.SenderId,
            request.TenantId,
            cancellationToken);

        if (!isParticipant)
            return ChatResult<ChatMessageDto>.Failure("Sender is not a participant in this conversation", "NOT_PARTICIPANT");

        // Get conversation
        var conversation = await _conversations.GetByIdAsync(request.ConversationId, request.TenantId, cancellationToken);
        if (conversation == null)
            return ChatResult<ChatMessageDto>.Failure("Conversation not found", "CONVERSATION_NOT_FOUND");

        // Create message entity
        var now = _clock.UtcNow;
        var sequenceNumber = conversation.NextSequenceNumber();
        var message = new ChatMessage(
            Guid.NewGuid(),
            request.ConversationId,
            request.SenderId,
            request.TenantId,
            request.Content,
            sequenceNumber,
            now,
            request.IdempotencyKey);

        // Execute pre-send interceptors
        var context = new MessageContext { Message = message };
        await _pipeline.ExecuteBeforeAsync(context, cancellationToken);

        if (context.IsCancelled)
            return ChatResult<ChatMessageDto>.Failure(
                context.CancellationReason ?? "Message send cancelled",
                "MESSAGE_CANCELLED");

        try
        {
            // Persist message
            await _messages.CreateAsync(message, cancellationToken);
            await _conversations.UpdateAsync(conversation, cancellationToken);

            // Execute post-send interceptors
            await _pipeline.ExecuteAfterAsync(context, cancellationToken);

            // Dispatch to transport (outside transaction)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _dispatcher.DispatchAsync(
                        message,
                        request.ConversationId,
                        request.TenantId,
                        request.SenderId,
                        cancellationToken);
                }
                catch
                {
                    // Log dispatch errors but don't fail the send
                }
            }, cancellationToken);

            return ChatResult<ChatMessageDto>.Success(MapToDto(message));
        }
        catch (Exception ex)
        {
            return ChatResult<ChatMessageDto>.Failure(
                $"Failed to send message: {ex.Message}",
                "SEND_FAILED");
        }
    }

    /// <inheritdoc />
    public async Task<ChatResult<bool>> MarkAsReadAsync(MarkAsReadRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            return ChatResult<bool>.Failure("Request cannot be null", "INVALID_REQUEST");

        if (request.ConversationId == Guid.Empty)
            return ChatResult<bool>.Failure("Conversation ID is required", "INVALID_CONVERSATION_ID");

        if (request.UserId == Guid.Empty)
            return ChatResult<bool>.Failure("User ID is required", "INVALID_USER_ID");

        try
        {
            var now = _clock.UtcNow;
            var read = new MessageRead(
                request.ConversationId,
                request.UserId,
                request.LastReadSequence,
                now);

            await _reads.CreateOrUpdateAsync(read, cancellationToken);
            return ChatResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ChatResult<bool>.Failure(
                $"Failed to mark messages as read: {ex.Message}",
                "MARK_READ_FAILED");
        }
    }

    /// <inheritdoc />
    public async Task<ChatResult<PaginatedResult<ChatMessageDto>>> GetMessagesAsync(GetMessagesQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            return ChatResult<PaginatedResult<ChatMessageDto>>.Failure("Query cannot be null", "INVALID_REQUEST");

        if (query.ConversationId == Guid.Empty)
            return ChatResult<PaginatedResult<ChatMessageDto>>.Failure("Conversation ID is required", "INVALID_CONVERSATION_ID");

        try
        {
            var result = await _messages.GetByConversationIdAsync(
                query.ConversationId,
                query.TenantId,
                query.LastSeenSequence,
                query.PageSize,
                cancellationToken);

            var dtos = result.Items.Select(MapToDto);
            var paginatedDto = new PaginatedResult<ChatMessageDto>(dtos, result.HasMore, result.NextCursor);

            return ChatResult<PaginatedResult<ChatMessageDto>>.Success(paginatedDto);
        }
        catch (Exception ex)
        {
            return ChatResult<PaginatedResult<ChatMessageDto>>.Failure(
                $"Failed to get messages: {ex.Message}",
                "GET_MESSAGES_FAILED");
        }
    }

    /// <inheritdoc />
    public async Task<ChatResult<IEnumerable<ConversationDto>>> GetConversationsAsync(GetConversationsQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            return ChatResult<IEnumerable<ConversationDto>>.Failure("Query cannot be null", "INVALID_REQUEST");

        if (query.UserId == Guid.Empty)
            return ChatResult<IEnumerable<ConversationDto>>.Failure("User ID is required", "INVALID_USER_ID");

        try
        {
            var skip = (query.Page - 1) * query.PageSize;
            var conversations = await _conversations.GetByUserIdAsync(
                query.UserId,
                query.TenantId,
                skip,
                query.PageSize,
                cancellationToken);

            var dtos = conversations.Select(c => new ConversationDto
            {
                Id = c.Id,
                Type = c.Type,
                CreatedAt = c.CreatedAt
            });

            return ChatResult<IEnumerable<ConversationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return ChatResult<IEnumerable<ConversationDto>>.Failure(
                $"Failed to get conversations: {ex.Message}",
                "GET_CONVERSATIONS_FAILED");
        }
    }

    /// <inheritdoc />
    public async Task<ChatResult<bool>> DeleteMessageAsync(Guid messageId, Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            return ChatResult<bool>.Failure("Message ID is required", "INVALID_MESSAGE_ID");

        if (userId == Guid.Empty)
            return ChatResult<bool>.Failure("User ID is required", "INVALID_USER_ID");

        try
        {
            var message = await _messages.GetByIdAsync(messageId, tenantId, cancellationToken);
            if (message == null)
                return ChatResult<bool>.Failure("Message not found", "MESSAGE_NOT_FOUND");

            if (message.SenderId != userId)
                return ChatResult<bool>.Failure("Only the sender can delete a message", "UNAUTHORIZED");

            message.SoftDelete();
            await _messages.UpdateAsync(message, cancellationToken);

            return ChatResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ChatResult<bool>.Failure(
                $"Failed to delete message: {ex.Message}",
                "DELETE_FAILED");
        }
    }

    private static ChatMessageDto MapToDto(ChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            Content = message.Content,
            SequenceNumber = message.SequenceNumber,
            SentAt = message.SentAt,
            IsDeleted = message.IsDeleted
        };
    }
}