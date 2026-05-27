namespace ChatCore.Tests.Integration.Database;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Domain.Enums;
using ChatCore.Persistence.EFCore;
using ChatCore.Persistence.EFCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ConversationRepositoryTests : IAsyncLifetime
{
    private ChatCoreDbContext _context = null!;
    private ConversationRepository _repository = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ChatCoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChatCoreDbContext(options);
        _repository = new ConversationRepository(_context);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_StoresConversation()
    {
        // Arrange
        var conversation = new Conversation(
            Guid.NewGuid(),
            ConversationType.Direct,
            Guid.NewGuid(),
            DateTime.UtcNow);

        // Act
        await _repository.CreateAsync(conversation);

        // Assert
        var retrieved = await _repository.GetByIdAsync(conversation.Id, conversation.TenantId);
        Assert.NotNull(retrieved);
        Assert.Equal(conversation.Id, retrieved.Id);
    }

    [Fact]
    public async Task IsUserParticipantAsync_ReturnsTrueWhenParticipant()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var conversation = new Conversation(conversationId, ConversationType.Direct, tenantId, DateTime.UtcNow);
        await _repository.CreateAsync(conversation);

        var participant = new Participant(conversationId, userId, DateTime.UtcNow);
        _context.Participants.Add(participant);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsUserParticipantAsync(conversationId, userId, tenantId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsUserParticipantAsync_ReturnsFalseWhenNotParticipant()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var conversation = new Conversation(conversationId, ConversationType.Direct, tenantId, DateTime.UtcNow);
        await _repository.CreateAsync(conversation);

        var participant = new Participant(conversationId, otherUserId, DateTime.UtcNow);
        _context.Participants.Add(participant);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsUserParticipantAsync(conversationId, userId, tenantId);

        // Assert
        Assert.False(result);
    }
}
