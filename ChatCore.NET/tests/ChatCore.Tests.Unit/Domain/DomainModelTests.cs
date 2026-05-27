namespace ChatCore.Tests.Unit.Domain;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Domain.Enums;
using Xunit;

public class DomainModelTests
{
    [Fact]
    public void Conversation_NextSequenceNumber_IncrementsProperly()
    {
        // Arrange
        var conversation = new Conversation(Guid.NewGuid(), ConversationType.Direct, Guid.NewGuid(), DateTime.UtcNow);

        // Act
        var seq1 = conversation.NextSequenceNumber();
        var seq2 = conversation.NextSequenceNumber();
        var seq3 = conversation.NextSequenceNumber();

        // Assert
        Assert.Equal(1, seq1);
        Assert.Equal(2, seq2);
        Assert.Equal(3, seq3);
    }

    [Fact]
    public void ChatMessage_SoftDelete_SetsDeletedAndPlaceholder()
    {
        // Arrange
        var message = new ChatMessage(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Original content",
            1,
            DateTime.UtcNow);

        // Act
        message.SoftDelete();

        // Assert
        Assert.True(message.IsDeleted);
        Assert.Equal("[deleted]", message.Content);
    }
}
