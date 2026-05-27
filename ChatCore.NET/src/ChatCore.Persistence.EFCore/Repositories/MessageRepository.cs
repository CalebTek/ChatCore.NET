namespace ChatCore.Persistence.EFCore.Repositories;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Repositories;
using ChatCore.Abstractions.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core implementation of <see cref="IMessageRepository"/>.
/// </summary>
public class MessageRepository : IMessageRepository
{
    private readonly ChatCoreDbContext _context;

    public MessageRepository(ChatCoreDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ChatMessage?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == id && m.TenantId == tenantId, cancellationToken);
    }

    public async Task<PaginatedResult<ChatMessage>> GetByConversationIdAsync(
        Guid conversationId,
        Guid tenantId,
        long lastSeenSequence,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Seek-based pagination: fetch pageSize + 1 to determine if more exist
        var messages = await _context.Messages
            .Where(m => m.ConversationId == conversationId && m.TenantId == tenantId && m.SequenceNumber < lastSeenSequence)
            .OrderByDescending(m => m.SequenceNumber)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = messages.Count > pageSize;
        var items = hasMore ? messages.Take(pageSize).ToList() : messages;

        // Reverse to return in ascending order
        items.Reverse();

        var nextCursor = items.FirstOrDefault()?.SequenceNumber;

        return new PaginatedResult<ChatMessage>(items, hasMore, nextCursor);
    }

    public async Task<ChatMessage?> GetByIdempotencyKeyAsync(string idempotencyKey, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .FirstOrDefaultAsync(
                m => m.IdempotencyKey == idempotencyKey && m.TenantId == tenantId,
                cancellationToken);
    }

    public async Task UpdateAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        _context.Messages.Update(message);
        await _context.SaveChangesAsync(cancellationToken);
    }
}