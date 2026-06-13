namespace ChatCore.Tests.Integration.Database;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Persistence.EFCore;
using ChatCore.Persistence.EFCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class UserConnectionRepositoryTests : IAsyncLifetime
{
    private ChatCoreDbContext          _context    = null!;
    private UserConnectionRepository   _repository = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ChatCoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context    = new ChatCoreDbContext(options);
        _repository = new UserConnectionRepository(_context);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    // =========================================================================
    // AddAsync / GetByUserIdAsync
    // =========================================================================

    [Fact]
    public async Task AddAsync_StoresConnection()
    {
        var userId = Guid.NewGuid();
        var conn   = new UserConnection(userId, "conn-1", DateTime.UtcNow);

        await _repository.AddAsync(conn);

        var connections = (await _repository.GetByUserIdAsync(userId)).ToList();
        Assert.Single(connections);
        Assert.Equal("conn-1", connections[0].ConnectionId);
    }

    [Fact]
    public async Task AddAsync_MultipleConnectionsForSameUser_AllStored()
    {
        var userId = Guid.NewGuid();
        await _repository.AddAsync(new UserConnection(userId, "conn-a", DateTime.UtcNow));
        await _repository.AddAsync(new UserConnection(userId, "conn-b", DateTime.UtcNow));

        var connections = (await _repository.GetByUserIdAsync(userId)).ToList();

        Assert.Equal(2, connections.Count);
        Assert.Contains(connections, c => c.ConnectionId == "conn-a");
        Assert.Contains(connections, c => c.ConnectionId == "conn-b");
    }

    [Fact]
    public async Task GetByUserIdAsync_UnknownUser_ReturnsEmpty()
    {
        var connections = await _repository.GetByUserIdAsync(Guid.NewGuid());

        Assert.Empty(connections);
    }

    // =========================================================================
    // RemoveAsync
    // =========================================================================

    [Fact]
    public async Task RemoveAsync_RemovesSpecificConnection()
    {
        var userId = Guid.NewGuid();
        await _repository.AddAsync(new UserConnection(userId, "conn-1", DateTime.UtcNow));
        await _repository.AddAsync(new UserConnection(userId, "conn-2", DateTime.UtcNow));

        await _repository.RemoveAsync(userId, "conn-1");

        var connections = (await _repository.GetByUserIdAsync(userId)).ToList();
        Assert.Single(connections);
        Assert.Equal("conn-2", connections[0].ConnectionId);
    }

    [Fact]
    public async Task RemoveAsync_NonExistentConnection_DoesNotThrow()
    {
        var ex = await Record.ExceptionAsync(
            () => _repository.RemoveAsync(Guid.NewGuid(), "ghost-conn"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task RemoveAsync_DoesNotAffectOtherUsers()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await _repository.AddAsync(new UserConnection(userA, "conn-a", DateTime.UtcNow));
        await _repository.AddAsync(new UserConnection(userB, "conn-b", DateTime.UtcNow));

        await _repository.RemoveAsync(userA, "conn-a");

        var connectionsB = (await _repository.GetByUserIdAsync(userB)).ToList();
        Assert.Single(connectionsB);
    }

    // =========================================================================
    // IsUserOnlineAsync
    // =========================================================================

    [Fact]
    public async Task IsUserOnlineAsync_ReturnsTrueWhenConnected()
    {
        var userId = Guid.NewGuid();
        await _repository.AddAsync(new UserConnection(userId, "conn-1", DateTime.UtcNow));

        var result = await _repository.IsUserOnlineAsync(userId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsUserOnlineAsync_ReturnsFalseWhenNoConnections()
    {
        var result = await _repository.IsUserOnlineAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task IsUserOnlineAsync_ReturnsFalseAfterAllConnectionsRemoved()
    {
        var userId = Guid.NewGuid();
        await _repository.AddAsync(new UserConnection(userId, "conn-1", DateTime.UtcNow));
        await _repository.RemoveAsync(userId, "conn-1");

        var result = await _repository.IsUserOnlineAsync(userId);

        Assert.False(result);
    }

    // =========================================================================
    // GetDistinctOnlineUserIdsAsync
    // =========================================================================

    [Fact]
    public async Task GetDistinctOnlineUserIdsAsync_ReturnsDistinctUserIds()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        // userA has 2 connections — should appear only once
        await _repository.AddAsync(new UserConnection(userA, "conn-a1", DateTime.UtcNow));
        await _repository.AddAsync(new UserConnection(userA, "conn-a2", DateTime.UtcNow));
        await _repository.AddAsync(new UserConnection(userB, "conn-b1", DateTime.UtcNow));

        var onlineUsers = (await _repository.GetDistinctOnlineUserIdsAsync()).ToList();

        Assert.Equal(2, onlineUsers.Count);
        Assert.Contains(userA, onlineUsers);
        Assert.Contains(userB, onlineUsers);
    }

    [Fact]
    public async Task GetDistinctOnlineUserIdsAsync_NoConnections_ReturnsEmpty()
    {
        var onlineUsers = await _repository.GetDistinctOnlineUserIdsAsync();

        Assert.Empty(onlineUsers);
    }

    [Fact]
    public async Task GetDistinctOnlineUserIdsAsync_ExcludesDisconnectedUsers()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        await _repository.AddAsync(new UserConnection(userA, "conn-a", DateTime.UtcNow));
        await _repository.AddAsync(new UserConnection(userB, "conn-b", DateTime.UtcNow));

        // userB disconnects
        await _repository.RemoveAsync(userB, "conn-b");

        var onlineUsers = (await _repository.GetDistinctOnlineUserIdsAsync()).ToList();

        Assert.Single(onlineUsers);
        Assert.Equal(userA, onlineUsers[0]);
    }
}
