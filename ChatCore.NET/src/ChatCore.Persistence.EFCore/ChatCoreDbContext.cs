namespace ChatCore.Persistence.EFCore;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Persistence.EFCore.Configurations;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core database context for ChatCore.
/// </summary>
public class ChatCoreDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatCoreDbContext"/> class.
    /// </summary>
    /// <param name="options">The DbContext options.</param>
    public ChatCoreDbContext(DbContextOptions<ChatCoreDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the conversations DbSet.
    /// </summary>
    public DbSet<Conversation> Conversations => Set<Conversation>();

    /// <summary>
    /// Gets the participants DbSet.
    /// </summary>
    public DbSet<Participant> Participants => Set<Participant>();

    /// <summary>
    /// Gets the messages DbSet.
    /// </summary>
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    /// <summary>
    /// Gets the read receipts DbSet.
    /// </summary>
    public DbSet<MessageRead> MessageReads => Set<MessageRead>();

    /// <summary>
    /// Gets the user connections DbSet.
    /// </summary>
    public DbSet<UserConnection> UserConnections => Set<UserConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new ConversationConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipantConfiguration());
        modelBuilder.ApplyConfiguration(new ChatMessageConfiguration());
        modelBuilder.ApplyConfiguration(new MessageReadConfiguration());
        modelBuilder.ApplyConfiguration(new UserConnectionConfiguration());
    }
}