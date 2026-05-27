namespace ChatCore.Tests.Integration.Database;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Domain.Enums;
using ChatCore.Persistence.EFCore;
using ChatCore.Persistence.EFCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class MessageRepositoryTests : IAsyncLifetime
{
    private ChatCoreDbContext _context = null!;
    private MessageRepository _repository = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ChatCoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChatCoreDbContext(options);
        _repository = new MessageRepository(_context);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_StoresMessage()
    {
        // Arrange
        var message = new ChatMessage(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test message",
            1,
            DateTime.UtcNow);

        // Act
        await _repository.CreateAsync(message);

        // Assert
        var retrieved = await _repository.GetByIdAsync(message.Id, message.TenantId);
        Assert.NotNull(retrieved);
        Assert.Equal(message.Id, retrieved.Id);
    }

    [Fact]
    public async Task GetByConversationIdAsync_ReturnsPaginatedResults()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var messages = new List<ChatMessage>();
        for (int i = 1; i <= 5; i++)
        {
            messages.Add(new ChatMessage(
                Guid.NewGuid(),
                conversationId,
                Guid.NewGuid(),
                tenantId,
                $"Message {i}",
                i,
                DateTime.UtcNow.AddSeconds(i)));
        }

        foreach (var msg in messages)
        {
            await _repository.CreateAsync(msg);
        }

        // Act
        var result = await _repository.GetByConversationIdAsync(conversationId, tenantId, long.MaxValue, 3);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasMore);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_ReturnsDuplicateMessage()
    {
        // Arrange
        var idempotencyKey = "test-idempotency-key";
        var message = new ChatMessage(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test message",
            1,
            DateTime.UtcNow,
            idempotencyKey);

        await _repository.CreateAsync(message);

        // Act
        var retrieved = await _repository.GetByIdempotencyKeyAsync(idempotencyKey, message.TenantId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(message.Id, retrieved.Id);
    }
}
