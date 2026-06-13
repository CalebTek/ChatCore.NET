namespace ChatCore.Tests.Integration.Database;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Persistence.EFCore;
using ChatCore.Persistence.EFCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class MessageRepositoryTests : IAsyncLifetime
{
    private ChatCoreDbContext  _context    = null!;
    private MessageRepository  _repository = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ChatCoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context    = new ChatCoreDbContext(options);
        _repository = new MessageRepository(_context);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    // =========================================================================
    // CreateAsync / GetByIdAsync
    // =========================================================================

    [Fact]
    public async Task CreateAsync_StoresMessage()
    {
        var message = MakeMessage(seq: 1);

        await _repository.CreateAsync(message);

        var retrieved = await _repository.GetByIdAsync(message.Id, message.TenantId);
        Assert.NotNull(retrieved);
        Assert.Equal(message.Id,      retrieved.Id);
        Assert.Equal(message.Content, retrieved.Content);
    }

    [Fact]
    public async Task GetByIdAsync_WrongTenantId_ReturnsNull()
    {
        var message = MakeMessage(seq: 1);
        await _repository.CreateAsync(message);

        var retrieved = await _repository.GetByIdAsync(message.Id, Guid.NewGuid());

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var retrieved = await _repository.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(retrieved);
    }

    // =========================================================================
    // GetByConversationIdAsync — pagination
    // =========================================================================

    [Fact]
    public async Task GetByConversationIdAsync_ReturnsPaginatedResults()
    {
        var (conversationId, tenantId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedMessages(conversationId, tenantId, count: 5);

        var result = await _repository.GetByConversationIdAsync(conversationId, tenantId, long.MaxValue, 3);

        Assert.Equal(3, result.Count);
        Assert.True(result.HasMore);
    }

    [Fact]
    public async Task GetByConversationIdAsync_LastPage_HasMoreIsFalse()
    {
        var (conversationId, tenantId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedMessages(conversationId, tenantId, count: 3);

        var result = await _repository.GetByConversationIdAsync(conversationId, tenantId, long.MaxValue, 10);

        Assert.Equal(3, result.Count);
        Assert.False(result.HasMore);
    }

    [Fact]
    public async Task GetByConversationIdAsync_ReturnsItemsInAscendingSequenceOrder()
    {
        var (conversationId, tenantId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedMessages(conversationId, tenantId, count: 4);

        var result = await _repository.GetByConversationIdAsync(conversationId, tenantId, long.MaxValue, 10);
        var sequences = result.Items.Select(m => m.SequenceNumber).ToList();

        Assert.Equal(sequences.OrderBy(s => s).ToList(), sequences);
    }

    [Fact]
    public async Task GetByConversationIdAsync_SeekPagination_LoadsCorrectPage()
    {
        var (conversationId, tenantId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedMessages(conversationId, tenantId, count: 6);

        // First page: sequences 4, 5, 6
        var page1 = await _repository.GetByConversationIdAsync(conversationId, tenantId, long.MaxValue, 3);
        // Second page: sequences 1, 2, 3
        var page2 = await _repository.GetByConversationIdAsync(conversationId, tenantId, page1.NextCursor!.Value, 3);

        Assert.Equal(3, page1.Count);
        Assert.Equal(3, page2.Count);
        Assert.True(page1.HasMore);
        Assert.False(page2.HasMore);

        // No overlap
        var page1Ids = page1.Items.Select(m => m.SequenceNumber).ToHashSet();
        Assert.All(page2.Items, m => Assert.DoesNotContain(m.SequenceNumber, page1Ids));
    }

    [Fact]
    public async Task GetByConversationIdAsync_EmptyConversation_ReturnsEmpty()
    {
        var result = await _repository.GetByConversationIdAsync(Guid.NewGuid(), Guid.NewGuid(), long.MaxValue, 10);

        Assert.Empty(result.Items);
        Assert.False(result.HasMore);
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task GetByConversationIdAsync_IsolatesByTenant()
    {
        var conversationId = Guid.NewGuid();
        var tenantA        = Guid.NewGuid();
        var tenantB        = Guid.NewGuid();

        await _repository.CreateAsync(MakeMessage(conversationId: conversationId, tenantId: tenantA, seq: 1, content: "A"));
        await _repository.CreateAsync(MakeMessage(conversationId: conversationId, tenantId: tenantB, seq: 2, content: "B"));

        var resultA = await _repository.GetByConversationIdAsync(conversationId, tenantA, long.MaxValue, 10);
        var resultB = await _repository.GetByConversationIdAsync(conversationId, tenantB, long.MaxValue, 10);

        Assert.Single(resultA.Items);
        Assert.Equal("A", resultA.Items.First().Content);
        Assert.Single(resultB.Items);
        Assert.Equal("B", resultB.Items.First().Content);
    }

    // =========================================================================
    // GetByIdempotencyKeyAsync
    // =========================================================================

    [Fact]
    public async Task GetByIdempotencyKeyAsync_ReturnsDuplicateMessage()
    {
        var key     = "idem-key-1";
        var message = MakeMessage(seq: 1, idempotencyKey: key);
        await _repository.CreateAsync(message);

        var retrieved = await _repository.GetByIdempotencyKeyAsync(key, message.TenantId);

        Assert.NotNull(retrieved);
        Assert.Equal(message.Id, retrieved.Id);
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_WrongTenantId_ReturnsNull()
    {
        var key     = "idem-key-2";
        var message = MakeMessage(seq: 1, idempotencyKey: key);
        await _repository.CreateAsync(message);

        var retrieved = await _repository.GetByIdempotencyKeyAsync(key, Guid.NewGuid());

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_UnknownKey_ReturnsNull()
    {
        var retrieved = await _repository.GetByIdempotencyKeyAsync("unknown", Guid.NewGuid());

        Assert.Null(retrieved);
    }

    // =========================================================================
    // UpdateAsync (soft delete)
    // =========================================================================

    [Fact]
    public async Task UpdateAsync_SoftDeleteIsPersisted()
    {
        var message = MakeMessage(seq: 1);
        await _repository.CreateAsync(message);

        message.SoftDelete();
        await _repository.UpdateAsync(message);

        var retrieved = await _repository.GetByIdAsync(message.Id, message.TenantId);
        Assert.NotNull(retrieved);
        Assert.True(retrieved.IsDeleted);
        Assert.Equal("[deleted]", retrieved.Content);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static ChatMessage MakeMessage(
        Guid?   conversationId  = null,
        Guid?   tenantId        = null,
        long    seq             = 1,
        string  content         = "Test message",
        string? idempotencyKey  = null) =>
        new(
            Guid.NewGuid(),
            conversationId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            tenantId       ?? Guid.NewGuid(),
            content,
            seq,
            DateTime.UtcNow,
            idempotencyKey);

    private async Task SeedMessages(Guid conversationId, Guid tenantId, int count)
    {
        for (int i = 1; i <= count; i++)
            await _repository.CreateAsync(MakeMessage(conversationId: conversationId, tenantId: tenantId, seq: i));
    }
}
