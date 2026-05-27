namespace ChatCore.Tests.Integration.Database;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Persistence.EFCore;
using ChatCore.Persistence.EFCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ReadReceiptRepositoryTests : IAsyncLifetime
{
    private ChatCoreDbContext _context = null!;
    private ReadReceiptRepository _repository = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ChatCoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChatCoreDbContext(options);
        _repository = new ReadReceiptRepository(_context);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task CreateOrUpdateAsync_CreatesNewReadReceipt()
    {
        // Arrange
        var read = new MessageRead(
            Guid.NewGuid(),
            Guid.NewGuid(),
            5,
            DateTime.UtcNow);

        // Act
        await _repository.CreateOrUpdateAsync(read);

        // Assert
        var retrieved = await _repository.GetByConversationAndUserAsync(read.ConversationId, read.UserId);
        Assert.NotNull(retrieved);
        Assert.Equal(5, retrieved.LastReadSequence);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_UpdatesExistingReadReceipt()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var read = new MessageRead(conversationId, userId, 5, DateTime.UtcNow);
        await _repository.CreateOrUpdateAsync(read);

        var updated = new MessageRead(conversationId, userId, 10, DateTime.UtcNow.AddSeconds(1));

        // Act
        await _repository.CreateOrUpdateAsync(updated);

        // Assert
        var retrieved = await _repository.GetByConversationAndUserAsync(conversationId, userId);
        Assert.NotNull(retrieved);
        Assert.Equal(10, retrieved.LastReadSequence);
    }
}
