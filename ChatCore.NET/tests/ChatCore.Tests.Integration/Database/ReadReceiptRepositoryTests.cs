namespace ChatCore.Tests.Integration.Database;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Persistence.EFCore;
using ChatCore.Persistence.EFCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ReadReceiptRepositoryTests : IAsyncLifetime
{
    private ChatCoreDbContext      _context    = null!;
    private ReadReceiptRepository  _repository = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ChatCoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context    = new ChatCoreDbContext(options);
        _repository = new ReadReceiptRepository(_context);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    // =========================================================================
    // CreateOrUpdateAsync
    // =========================================================================

    [Fact]
    public async Task CreateOrUpdateAsync_CreatesNewReadReceipt()
    {
        var read = MakeRead(sequence: 5);

        await _repository.CreateOrUpdateAsync(read);

        var retrieved = await _repository.GetByConversationAndUserAsync(read.ConversationId, read.UserId);
        Assert.NotNull(retrieved);
        Assert.Equal(5,              retrieved.LastReadSequence);
        Assert.Equal(read.TenantId,  retrieved.TenantId);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_UpdatesExistingReadReceipt()
    {
        var conversationId = Guid.NewGuid();
        var userId         = Guid.NewGuid();
        var tenantId       = Guid.NewGuid();

        var read    = new MessageRead(conversationId, userId, tenantId, 5,  DateTime.UtcNow);
        var updated = new MessageRead(conversationId, userId, tenantId, 10, DateTime.UtcNow.AddSeconds(1));

        await _repository.CreateOrUpdateAsync(read);
        await _repository.CreateOrUpdateAsync(updated);

        var retrieved = await _repository.GetByConversationAndUserAsync(conversationId, userId);
        Assert.NotNull(retrieved);
        Assert.Equal(10, retrieved.LastReadSequence);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_DifferentUsers_CreatesSeparateReceipts()
    {
        var conversationId = Guid.NewGuid();
        var tenantId       = Guid.NewGuid();
        var userId1        = Guid.NewGuid();
        var userId2        = Guid.NewGuid();

        await _repository.CreateOrUpdateAsync(new MessageRead(conversationId, userId1, tenantId, 3, DateTime.UtcNow));
        await _repository.CreateOrUpdateAsync(new MessageRead(conversationId, userId2, tenantId, 7, DateTime.UtcNow));

        var r1 = await _repository.GetByConversationAndUserAsync(conversationId, userId1);
        var r2 = await _repository.GetByConversationAndUserAsync(conversationId, userId2);

        Assert.Equal(3, r1!.LastReadSequence);
        Assert.Equal(7, r2!.LastReadSequence);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_UpdateDoesNotChangeTenantId()
    {
        var conversationId = Guid.NewGuid();
        var userId         = Guid.NewGuid();
        var tenantId       = Guid.NewGuid();

        await _repository.CreateOrUpdateAsync(new MessageRead(conversationId, userId, tenantId, 1, DateTime.UtcNow));
        // A second write with the same tenantId — TenantId must stay the same
        await _repository.CreateOrUpdateAsync(new MessageRead(conversationId, userId, tenantId, 5, DateTime.UtcNow));

        var retrieved = await _repository.GetByConversationAndUserAsync(conversationId, userId);
        Assert.Equal(tenantId, retrieved!.TenantId);
    }

    // =========================================================================
    // GetByConversationAndUserAsync
    // =========================================================================

    [Fact]
    public async Task GetByConversationAndUserAsync_NonExistent_ReturnsNull()
    {
        var retrieved = await _repository.GetByConversationAndUserAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(retrieved);
    }

    // =========================================================================
    // GetByConversationIdAsync
    // =========================================================================

    [Fact]
    public async Task GetByConversationIdAsync_ReturnsAllReceiptsForConversation()
    {
        var conversationId = Guid.NewGuid();
        var tenantId       = Guid.NewGuid();

        await _repository.CreateOrUpdateAsync(new MessageRead(conversationId, Guid.NewGuid(), tenantId, 1, DateTime.UtcNow));
        await _repository.CreateOrUpdateAsync(new MessageRead(conversationId, Guid.NewGuid(), tenantId, 2, DateTime.UtcNow));
        await _repository.CreateOrUpdateAsync(new MessageRead(conversationId, Guid.NewGuid(), tenantId, 3, DateTime.UtcNow));
        // Different conversation — should not appear
        await _repository.CreateOrUpdateAsync(new MessageRead(Guid.NewGuid(), Guid.NewGuid(), tenantId, 9, DateTime.UtcNow));

        var results = (await _repository.GetByConversationIdAsync(conversationId)).ToList();

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal(conversationId, r.ConversationId));
    }

    [Fact]
    public async Task GetByConversationIdAsync_EmptyConversation_ReturnsEmpty()
    {
        var results = await _repository.GetByConversationIdAsync(Guid.NewGuid());

        Assert.Empty(results);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static MessageRead MakeRead(long sequence = 1) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), sequence, DateTime.UtcNow);
}
