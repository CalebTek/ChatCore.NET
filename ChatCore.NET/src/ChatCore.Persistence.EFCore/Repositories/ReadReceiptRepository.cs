namespace ChatCore.Persistence.EFCore.Repositories;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core implementation of <see cref="IReadReceiptRepository"/>.
/// </summary>
public class ReadReceiptRepository : IReadReceiptRepository
{
    private readonly ChatCoreDbContext _context;

    public ReadReceiptRepository(ChatCoreDbContext context)
    {
        _context = context;
    }

    public async Task CreateOrUpdateAsync(MessageRead read, CancellationToken cancellationToken = default)
    {
        var existing = await _context.MessageReads
            .FirstOrDefaultAsync(
                r => r.ConversationId == read.ConversationId && r.UserId == read.UserId,
                cancellationToken);

        if (existing == null)
        {
            _context.MessageReads.Add(read);
        }
        else
        {
            existing.Update(read.LastReadSequence, read.ReadAt);
            _context.MessageReads.Update(existing);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<MessageRead?> GetByConversationAndUserAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MessageReads
            .FirstOrDefaultAsync(
                r => r.ConversationId == conversationId && r.UserId == userId,
                cancellationToken);
    }

    public async Task<IEnumerable<MessageRead>> GetByConversationIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MessageReads
            .Where(r => r.ConversationId == conversationId)
            .ToListAsync(cancellationToken);
    }
}