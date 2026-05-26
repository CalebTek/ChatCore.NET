namespace ChatCore.Abstractions.Domain.Entities;

using ChatCore.Abstractions.Domain.Enums;

/// <summary>
/// Represents a conversation (1:1 or group).
/// </summary>
public class Conversation
{
    /// <summary>
    /// Gets the conversation identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the type of conversation (Direct or Group).
    /// </summary>
    public ConversationType Type { get; private set; }

    /// <summary>
    /// Gets the tenant identifier for multi-tenancy support.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the last sequence number for atomic ordering.
    /// </summary>
    public long LastSequenceNumber { get; private set; }

    /// <summary>
    /// Gets the row version for concurrency control.
    /// </summary>
    public byte[]? RowVersion { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Conversation"/> class.
    /// </summary>
    /// <param name="id">The conversation identifier.</param>
    /// <param name="type">The type of conversation.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="createdAt">The creation timestamp.</param>
    public Conversation(Guid id, ConversationType type, Guid tenantId, DateTime createdAt)
    {
        Id = id;
        Type = type;
        TenantId = tenantId;
        CreatedAt = createdAt;
        LastSequenceNumber = 0;
    }

    /// <summary>
    /// Increments the sequence number atomically.
    /// </summary>
    /// <returns>The next sequence number.</returns>
    public long NextSequenceNumber()
    {
        return ++LastSequenceNumber;
    }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
#pragma warning disable CS8618
    protected Conversation()
    {
    }
#pragma warning restore CS8618
}