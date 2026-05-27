namespace ChatCore.Persistence.EFCore.Repositories;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core implementation of <see cref="IConversationRepository"/>.
/// </summary>
public class ConversationRepository : IConversationRepository
{
    private readonly ChatCoreDbContext _context;

    public ConversationRepository(ChatCoreDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, cancellationToken);
    }

    public async Task<IEnumerable<Conversation>> GetByUserIdAsync(Guid userId, Guid tenantId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Where(c => c.TenantId == tenantId)
            .Join(
                _context.Participants,
                c => c.Id,
                p => p.ConversationId,
                (c, p) => new { Conversation = c, p.UserId })
            .Where(x => x.UserId == userId)
            .Select(x => x.Conversation)
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsUserParticipantAsync(Guid conversationId, Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Participants
            .AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId, cancellationToken);
    }

    public async Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        _context.Conversations.Update(conversation);
        await _context.SaveChangesAsync(cancellationToken);
    }
}