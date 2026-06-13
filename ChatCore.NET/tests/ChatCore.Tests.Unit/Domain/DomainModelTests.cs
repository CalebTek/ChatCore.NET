namespace ChatCore.Tests.Unit.Domain;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Domain.Enums;
using Xunit;

public class DomainModelTests
{
    // -------------------------------------------------------------------------
    // Conversation
    // -------------------------------------------------------------------------

    [Fact]
    public void Conversation_NextSequenceNumber_IncrementsProperly()
    {
        var conversation = new Conversation(Guid.NewGuid(), ConversationType.Direct, Guid.NewGuid(), DateTime.UtcNow);

        var seq1 = conversation.NextSequenceNumber();
        var seq2 = conversation.NextSequenceNumber();
        var seq3 = conversation.NextSequenceNumber();

        Assert.Equal(1, seq1);
        Assert.Equal(2, seq2);
        Assert.Equal(3, seq3);
    }

    [Fact]
    public void Conversation_NextSequenceNumber_StartsAtOne()
    {
        var conversation = new Conversation(Guid.NewGuid(), ConversationType.Group, Guid.NewGuid(), DateTime.UtcNow);

        var first = conversation.NextSequenceNumber();

        Assert.Equal(1, first);
    }

    [Fact]
    public void Conversation_Properties_AreSetCorrectly()
    {
        var id       = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var now      = DateTime.UtcNow;

        var conversation = new Conversation(id, ConversationType.Group, tenantId, now);

        Assert.Equal(id,                     conversation.Id);
        Assert.Equal(ConversationType.Group,  conversation.Type);
        Assert.Equal(tenantId,               conversation.TenantId);
        Assert.Equal(now,                    conversation.CreatedAt);
        Assert.Equal(0,                      conversation.LastSequenceNumber);
    }

    // -------------------------------------------------------------------------
    // ChatMessage
    // -------------------------------------------------------------------------

    [Fact]
    public void ChatMessage_SoftDelete_SetsDeletedAndPlaceholder()
    {
        var message = new ChatMessage(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Original content", 1, DateTime.UtcNow);

        message.SoftDelete();

        Assert.True(message.IsDeleted);
        Assert.Equal("[deleted]", message.Content);
    }

    [Fact]
    public void ChatMessage_IsDeleted_IsFalseOnCreation()
    {
        var message = new ChatMessage(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Hello", 1, DateTime.UtcNow);

        Assert.False(message.IsDeleted);
    }

    [Fact]
    public void ChatMessage_Properties_AreSetCorrectly()
    {
        var id             = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var senderId       = Guid.NewGuid();
        var tenantId       = Guid.NewGuid();
        var now            = DateTime.UtcNow;
        var key            = "idem-key-1";

        var message = new ChatMessage(id, conversationId, senderId, tenantId, "Hello", 5, now, key);

        Assert.Equal(id,             message.Id);
        Assert.Equal(conversationId, message.ConversationId);
        Assert.Equal(senderId,       message.SenderId);
        Assert.Equal(tenantId,       message.TenantId);
        Assert.Equal("Hello",        message.Content);
        Assert.Equal(5,              message.SequenceNumber);
        Assert.Equal(now,            message.SentAt);
        Assert.Equal(key,            message.IdempotencyKey);
    }

    [Fact]
    public void ChatMessage_IdempotencyKey_IsNullByDefault()
    {
        var message = new ChatMessage(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Hello", 1, DateTime.UtcNow);

        Assert.Null(message.IdempotencyKey);
    }

    // -------------------------------------------------------------------------
    // MessageRead
    // -------------------------------------------------------------------------

    [Fact]
    public void MessageRead_Properties_AreSetCorrectly()
    {
        var conversationId = Guid.NewGuid();
        var userId         = Guid.NewGuid();
        var tenantId       = Guid.NewGuid();
        var now            = DateTime.UtcNow;

        var read = new MessageRead(conversationId, userId, tenantId, 10, now);

        Assert.Equal(conversationId, read.ConversationId);
        Assert.Equal(userId,         read.UserId);
        Assert.Equal(tenantId,       read.TenantId);
        Assert.Equal(10,             read.LastReadSequence);
        Assert.Equal(now,            read.ReadAt);
    }

    [Fact]
    public void MessageRead_Update_ChangesSequenceAndTimestamp()
    {
        var read    = new MessageRead(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5, DateTime.UtcNow);
        var newTime = DateTime.UtcNow.AddSeconds(10);

        read.Update(20, newTime);

        Assert.Equal(20,      read.LastReadSequence);
        Assert.Equal(newTime, read.ReadAt);
    }

    [Fact]
    public void MessageRead_Update_DoesNotChangeTenantId()
    {
        var tenantId = Guid.NewGuid();
        var read     = new MessageRead(Guid.NewGuid(), Guid.NewGuid(), tenantId, 1, DateTime.UtcNow);

        read.Update(99, DateTime.UtcNow.AddMinutes(1));

        Assert.Equal(tenantId, read.TenantId);
    }

    // -------------------------------------------------------------------------
    // Participant
    // -------------------------------------------------------------------------

    [Fact]
    public void Participant_Properties_AreSetCorrectly()
    {
        var conversationId = Guid.NewGuid();
        var userId         = Guid.NewGuid();
        var now            = DateTime.UtcNow;

        var participant = new Participant(conversationId, userId, now);

        Assert.Equal(conversationId, participant.ConversationId);
        Assert.Equal(userId,         participant.UserId);
        Assert.Equal(now,            participant.JoinedAt);
    }

    // -------------------------------------------------------------------------
    // UserConnection
    // -------------------------------------------------------------------------

    [Fact]
    public void UserConnection_Properties_AreSetCorrectly()
    {
        var userId       = Guid.NewGuid();
        var connectionId = "conn-abc-123";
        var now          = DateTime.UtcNow;

        var connection = new UserConnection(userId, connectionId, now);

        Assert.Equal(userId,       connection.UserId);
        Assert.Equal(connectionId, connection.ConnectionId);
        Assert.Equal(now,          connection.ConnectedAt);
    }
}
