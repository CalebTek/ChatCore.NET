namespace ChatCore.Persistence.EFCore.Repositories;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core implementation of <see cref="IUserConnectionRepository"/>.
/// </summary>
public class UserConnectionRepository : IUserConnectionRepository
{
    private readonly ChatCoreDbContext _context;

    public UserConnectionRepository(ChatCoreDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UserConnection connection, CancellationToken cancellationToken = default)
    {
        _context.UserConnections.Add(connection);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await _context.UserConnections
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ConnectionId == connectionId, cancellationToken);

        if (connection != null)
        {
            _context.UserConnections.Remove(connection);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<UserConnection>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserConnections
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserConnections
            .AnyAsync(c => c.UserId == userId, cancellationToken);
    }
}