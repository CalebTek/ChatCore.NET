namespace ChatCore.Tests.Integration.Database;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Domain.Enums;
using ChatCore.Persistence.EFCore;
using ChatCore.Persistence.EFCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ConversationRepositoryTests : IAsyncLifetime
{
    private ChatCoreDbContext    _context    = null!;
    private ConversationRepository _repository = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ChatCoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context    = new ChatCoreDbContext(options);
        _repository = new ConversationRepository(_context);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    // =========================================================================
    // CreateAsync / GetByIdAsync
    // =========================================================================

    [Fact]
    public async Task CreateAsync_StoresConversation()
    {
        var conversation = new Conversation(Guid.NewGuid(), ConversationType.Direct, Guid.NewGuid(), DateTime.UtcNow);

        await _repository.CreateAsync(conversation);

        var retrieved = await _repository.GetByIdAsync(conversation.Id, conversation.TenantId);
        Assert.NotNull(retrieved);
        Assert.Equal(conversation.Id,       retrieved.Id);
        Assert.Equal(conversation.TenantId, retrieved.TenantId);
        Assert.Equal(conversation.Type,     retrieved.Type);
    }

    [Fact]
    public async Task GetByIdAsync_WrongTenantId_ReturnsNull()
    {
        var conversation = new Conversation(Guid.NewGuid(), ConversationType.Direct, Guid.NewGuid(), DateTime.UtcNow);
        await _repository.CreateAsync(conversation);

        var retrieved = await _repository.GetByIdAsync(conversation.Id, Guid.NewGuid());

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var retrieved = await _repository.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(retrieved);
    }

    // =========================================================================
    // IsUserParticipantAsync
    // =========================================================================

    [Fact]
    public async Task IsUserParticipantAsync_ReturnsTrueWhenParticipant()
    {
        var tenantId       = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var userId         = Guid.NewGuid();

        await _repository.CreateAsync(new Conversation(conversationId, ConversationType.Direct, tenantId, DateTime.UtcNow));
        _context.Participants.Add(new Participant(conversationId, userId, DateTime.UtcNow));
        await _context.SaveChangesAsync();

        var result = await _repository.IsUserParticipantAsync(conversationId, userId, tenantId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsUserParticipantAsync_ReturnsFalseWhenNotParticipant()
    {
        var tenantId       = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        await _repository.CreateAsync(new Conversation(conversationId, ConversationType.Direct, tenantId, DateTime.UtcNow));

        var result = await _repository.IsUserParticipantAsync(conversationId, Guid.NewGuid(), tenantId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsUserParticipantAsync_ReturnsFalseForOtherUsersParticipant()
    {
        var tenantId       = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var otherUserId    = Guid.NewGuid();

        await _repository.CreateAsync(new Conversation(conversationId, ConversationType.Direct, tenantId, DateTime.UtcNow));
        _context.Participants.Add(new Participant(conversationId, otherUserId, DateTime.UtcNow));
        await _context.SaveChangesAsync();

        var result = await _repository.IsUserParticipantAsync(conversationId, Guid.NewGuid(), tenantId);

        Assert.False(result);
    }

    // =========================================================================
    // GetByUserIdAsync
    // =========================================================================

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyUserConversations()
    {
        var tenantId = Guid.NewGuid();
        var userId   = Guid.NewGuid();

        var myConvId    = Guid.NewGuid();
        var otherConvId = Guid.NewGuid();

        await _repository.CreateAsync(new Conversation(myConvId,    ConversationType.Direct, tenantId, DateTime.UtcNow));
        await _repository.CreateAsync(new Conversation(otherConvId, ConversationType.Group,  tenantId, DateTime.UtcNow));

        _context.Participants.Add(new Participant(myConvId, userId, DateTime.UtcNow));
        await _context.SaveChangesAsync();

        var result = (await _repository.GetByUserIdAsync(userId, tenantId, 0, 20)).ToList();

        Assert.Single(result);
        Assert.Equal(myConvId, result[0].Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_RespectsSkipAndTake()
    {
        var tenantId = Guid.NewGuid();
        var userId   = Guid.NewGuid();

        for (int i = 0; i < 5; i++)
        {
            var convId = Guid.NewGuid();
            await _repository.CreateAsync(new Conversation(convId, ConversationType.Group, tenantId,
                DateTime.UtcNow.AddMinutes(i)));
            _context.Participants.Add(new Participant(convId, userId, DateTime.UtcNow));
        }
        await _context.SaveChangesAsync();

        var page1 = (await _repository.GetByUserIdAsync(userId, tenantId, 0, 2)).ToList();
        var page2 = (await _repository.GetByUserIdAsync(userId, tenantId, 2, 2)).ToList();

        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.DoesNotContain(page2, c => page1.Select(p => p.Id).Contains(c.Id));
    }

    [Fact]
    public async Task GetByUserIdAsync_IsolatesByTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userId  = Guid.NewGuid();

        var convA = Guid.NewGuid();
        var convB = Guid.NewGuid();
        await _repository.CreateAsync(new Conversation(convA, ConversationType.Direct, tenantA, DateTime.UtcNow));
        await _repository.CreateAsync(new Conversation(convB, ConversationType.Direct, tenantB, DateTime.UtcNow));
        _context.Participants.AddRange(
            new Participant(convA, userId, DateTime.UtcNow),
            new Participant(convB, userId, DateTime.UtcNow));
        await _context.SaveChangesAsync();

        var resultA = (await _repository.GetByUserIdAsync(userId, tenantA, 0, 20)).ToList();
        var resultB = (await _repository.GetByUserIdAsync(userId, tenantB, 0, 20)).ToList();

        Assert.Single(resultA);
        Assert.Equal(convA, resultA[0].Id);
        Assert.Single(resultB);
        Assert.Equal(convB, resultB[0].Id);
    }

    // =========================================================================
    // GetParticipantIdsByConversationIdsAsync
    // =========================================================================

    [Fact]
    public async Task GetParticipantIdsByConversationIdsAsync_ReturnsMappedParticipants()
    {
        var tenantId = Guid.NewGuid();
        var convId1  = Guid.NewGuid();
        var convId2  = Guid.NewGuid();
        var user1    = Guid.NewGuid();
        var user2    = Guid.NewGuid();
        var user3    = Guid.NewGuid();

        await _repository.CreateAsync(new Conversation(convId1, ConversationType.Direct, tenantId, DateTime.UtcNow));
        await _repository.CreateAsync(new Conversation(convId2, ConversationType.Group,  tenantId, DateTime.UtcNow));
        _context.Participants.AddRange(
            new Participant(convId1, user1, DateTime.UtcNow),
            new Participant(convId1, user2, DateTime.UtcNow),
            new Participant(convId2, user3, DateTime.UtcNow));
        await _context.SaveChangesAsync();

        var map = await _repository.GetParticipantIdsByConversationIdsAsync(
            new[] { convId1, convId2 });

        Assert.True(map.ContainsKey(convId1));
        Assert.Contains(user1, map[convId1]);
        Assert.Contains(user2, map[convId1]);

        Assert.True(map.ContainsKey(convId2));
        Assert.Contains(user3, map[convId2]);
    }

    [Fact]
    public async Task GetParticipantIdsByConversationIdsAsync_EmptyInput_ReturnsEmptyDictionary()
    {
        var map = await _repository.GetParticipantIdsByConversationIdsAsync(Array.Empty<Guid>());

        Assert.Empty(map);
    }

    [Fact]
    public async Task GetParticipantIdsByConversationIdsAsync_UnknownIds_AreOmitted()
    {
        var map = await _repository.GetParticipantIdsByConversationIdsAsync(
            new[] { Guid.NewGuid(), Guid.NewGuid() });

        Assert.Empty(map);
    }

    // =========================================================================
    // UpdateAsync
    // =========================================================================

    [Fact]
    public async Task UpdateAsync_SequenceNumberIsPersisted()
    {
        var conversation = new Conversation(Guid.NewGuid(), ConversationType.Direct, Guid.NewGuid(), DateTime.UtcNow);
        await _repository.CreateAsync(conversation);

        conversation.NextSequenceNumber();
        conversation.NextSequenceNumber();
        await _repository.UpdateAsync(conversation);

        var retrieved = await _repository.GetByIdAsync(conversation.Id, conversation.TenantId);
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved.LastSequenceNumber);
    }
}
