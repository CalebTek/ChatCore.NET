# Database Schema and Migrations

## 📊 Overview

This document describes the ChatCore.NET database schema, including table definitions, relationships, indexes, and migration strategies.

---

## 🗄️ Database Design

### Database Type

- **Primary:** SQL Server 2019+
- **ORM:** Entity Framework Core 8.0+
- **Migrations:** EF Core Code-First Migrations

### Naming Conventions

- **Tables:** PascalCase (User, Conversation, Message)
- **Columns:** PascalCase (FirstName, LastName)
- **Foreign Keys:** `FK_{PrimaryTable}_{ForeignTable}`
- **Indexes:** `IX_{Table}_{Column}`
- **Unique Constraints:** `UQ_{Table}_{Column}`

---

## 📋 Table Definitions

### Users Table

```sql
CREATE TABLE [Users] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [Email] NVARCHAR(256) NOT NULL UNIQUE,
    [Username] NVARCHAR(50) NOT NULL UNIQUE,
    [PasswordHash] NVARCHAR(MAX) NOT NULL,
    [FirstName] NVARCHAR(100),
    [LastName] NVARCHAR(100),
    [DisplayName] NVARCHAR(200),
    [AvatarUrl] NVARCHAR(MAX),
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsLocked] BIT NOT NULL DEFAULT 0,
    [FailedLoginAttempts] INT NOT NULL DEFAULT 0,
    [LockedUntil] DATETIME2,
    [CreatedAt] DATETIME2 NOT NULL,
    [UpdatedAt] DATETIME2,
    [LastLoginAt] DATETIME2
);

CREATE NONCLUSTERED INDEX [IX_Users_Email] ON [Users]([Email]);
CREATE NONCLUSTERED INDEX [IX_Users_Username] ON [Users]([Username]);
CREATE NONCLUSTERED INDEX [IX_Users_IsActive] ON [Users]([IsActive]);
```

### Conversations Table

```sql
CREATE TABLE [Conversations] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500),
    [Topic] NVARCHAR(200),
    [CreatedByUserId] UNIQUEIDENTIFIER NOT NULL,
    [IsPrivate] BIT NOT NULL DEFAULT 0,
    [IsArchived] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL,
    [UpdatedAt] DATETIME2,
    [LastMessageAt] DATETIME2,
    
    CONSTRAINT [FK_Conversations_Users] 
        FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
);

CREATE NONCLUSTERED INDEX [IX_Conversations_CreatedByUserId] ON [Conversations]([CreatedByUserId]);
CREATE NONCLUSTERED INDEX [IX_Conversations_IsPrivate] ON [Conversations]([IsPrivate]);
CREATE NONCLUSTERED INDEX [IX_Conversations_IsArchived] ON [Conversations]([IsArchived]);
CREATE NONCLUSTERED INDEX [IX_Conversations_CreatedAt] ON [Conversations]([CreatedAt]);
```

### Messages Table

```sql
CREATE TABLE [Messages] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [ConversationId] UNIQUEIDENTIFIER NOT NULL,
    [SenderId] UNIQUEIDENTIFIER NOT NULL,
    [Content] NVARCHAR(MAX) NOT NULL,
    [MessageType] INT NOT NULL DEFAULT 0,
    [IsEdited] BIT NOT NULL DEFAULT 0,
    [RepliedToMessageId] UNIQUEIDENTIFIER,
    [CreatedAt] DATETIME2 NOT NULL,
    [UpdatedAt] DATETIME2,
    [DeletedAt] DATETIME2,
    
    CONSTRAINT [FK_Messages_Conversations] 
        FOREIGN KEY ([ConversationId]) REFERENCES [Conversations]([Id]) ON DELETE CASCADE,
    
    CONSTRAINT [FK_Messages_Users_Sender]
        FOREIGN KEY ([SenderId]) REFERENCES [Users]([Id]) ON DELETE RESTRICT,
    
    CONSTRAINT [FK_Messages_Messages_RepliedTo]
        FOREIGN KEY ([RepliedToMessageId]) REFERENCES [Messages]([Id]) ON DELETE SET NULL
);

CREATE NONCLUSTERED INDEX [IX_Messages_ConversationId] ON [Messages]([ConversationId]);
CREATE NONCLUSTERED INDEX [IX_Messages_SenderId] ON [Messages]([SenderId]);
CREATE NONCLUSTERED INDEX [IX_Messages_CreatedAt] ON [Messages]([CreatedAt]);
CREATE NONCLUSTERED INDEX [IX_Messages_ConversationId_CreatedAt] 
    ON [Messages]([ConversationId], [CreatedAt]) INCLUDE ([Id], [SenderId], [Content]);
CREATE NONCLUSTERED INDEX [IX_Messages_DeletedAt] ON [Messages]([DeletedAt]) 
    WHERE [DeletedAt] IS NULL; -- Filtered index for active messages
```

### Participants Table

```sql
CREATE TABLE [Participants] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [ConversationId] UNIQUEIDENTIFIER NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [Role] INT NOT NULL DEFAULT 0,
    [JoinedAt] DATETIME2 NOT NULL,
    [LastReadAt] DATETIME2,
    [IsMuted] BIT NOT NULL DEFAULT 0,
    [IsPinned] BIT NOT NULL DEFAULT 0,
    [IsBlocked] BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT [FK_Participants_Conversations]
        FOREIGN KEY ([ConversationId]) REFERENCES [Conversations]([Id]) ON DELETE CASCADE,
    
    CONSTRAINT [FK_Participants_Users]
        FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE,
    
    CONSTRAINT [UQ_Participants_ConversationUser]
        UNIQUE ([ConversationId], [UserId])
);

CREATE NONCLUSTERED INDEX [IX_Participants_UserId] ON [Participants]([UserId]);
CREATE NONCLUSTERED INDEX [IX_Participants_ConversationId_Role] ON [Participants]([ConversationId], [Role]);
```

### Attachments Table

```sql
CREATE TABLE [Attachments] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [MessageId] UNIQUEIDENTIFIER NOT NULL,
    [FileName] NVARCHAR(255) NOT NULL,
    [ContentType] NVARCHAR(100) NOT NULL,
    [FileSize] BIGINT NOT NULL,
    [StorageUrl] NVARCHAR(MAX) NOT NULL,
    [UploadedAt] DATETIME2 NOT NULL,
    
    CONSTRAINT [FK_Attachments_Messages]
        FOREIGN KEY ([MessageId]) REFERENCES [Messages]([Id]) ON DELETE CASCADE
);

CREATE NONCLUSTERED INDEX [IX_Attachments_MessageId] ON [Attachments]([MessageId]);
```

---

## 🔗 Relationships Diagram

```
┌──────────────┐
│    Users     │
├──────────────┤
│ Id (PK)      │
│ Email        │
│ Username     │
│ PasswordHash │
│ ...          │
└──────┬───────┘
       │
       ├─────────────────┬────────────────────┬──────────────────┐
       │                 │                    │                  │
       │ (1:Many)        │ (1:Many)           │ (1:Many)         │
       │                 │                    │                  │
       ▼                 ▼                    ▼                  ▼
┌────────────────┐ ┌──────────────┐  ┌──────────────┐  ┌────────────────┐
│ Conversations  │ │  Participants│  │   Messages   │  │ (CreatedBy)    │
├────────────────┤ ├──────────────┤  ├──────────────┤  └────────────────┘
│ Id (PK)        │ │ Id (PK)      │  │ Id (PK)      │
│ CreatedByUserId│ │ UserId (FK)  │  │ SenderId(FK) │
│ Name           │ │ ConversationId│ │ ConversationId
│ ...            │ │ Role         │  │ Content      │
└────────┬───────┘ │ ...          │  │ ...          │
         │         └──────────────┘  └────┬─────────┘
         │ (1:Many)                       │ (1:Many)
         │                                │
         └────────────────────────────────┘

     Messages (1:Many) ──────► Attachments
```

---

## 🔑 Indexes

### Index Strategy

Indexes are created to optimize query performance:

| Table | Index | Columns | Purpose |
|-------|-------|---------|---------|
| Users | PK | Id | Primary key |
| Users | IX_Email | Email | Fast user lookup by email |
| Users | IX_Username | Username | Fast user lookup by username |
| Users | IX_IsActive | IsActive | Filter active users |
| Conversations | PK | Id | Primary key |
| Conversations | IX_CreatedBy | CreatedByUserId | Find conversations by creator |
| Conversations | IX_IsArchived | IsArchived | Filter archived conversations |
| Messages | PK | Id | Primary key |
| Messages | IX_ConvId_CreatedAt | ConversationId, CreatedAt | Pagination queries |
| Messages | IX_DeletedAt | DeletedAt (filtered) | Soft delete queries |
| Participants | PK | Id | Primary key |
| Participants | UQ_Unique | ConversationId, UserId | Prevent duplicates |
| Attachments | PK | Id | Primary key |
| Attachments | IX_MessageId | MessageId | Find attachments by message |

---

## 📝 EF Core Configuration

### DbContext

```csharp
public class ChatCoreDbContext : DbContext
{
    public ChatCoreDbContext(DbContextOptions<ChatCoreDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Participant> Participants { get; set; }
    public DbSet<Attachment> Attachments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatCoreDbContext).Assembly);
        
        // Global configurations
        modelBuilder.Entity<User>().HasQueryFilter(u => u.IsActive);
    }
}
```

### Entity Configuration Example

```csharp
public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(m => m.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(m => m.MessageType)
            .HasConversion<int>();

        // Relationships
        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany(u => u.Messages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => m.CreatedAt);
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt })
            .IncludeProperties(m => new { m.Id, m.SenderId, m.Content });
    }
}
```

---

## 🔄 Migrations

### Creating Migrations

```bash
# Create initial migration
dotnet ef migrations add InitialCreate --project src/ChatCore.Infrastructure --startup-project src/ChatCore.API

# Create migration for new feature
dotnet ef migrations add AddNotificationTable

# Create migration with description
dotnet ef migrations add AddIndexOnMessages -o Data/Migrations
```

### Migration Structure

```csharp
public partial class AddIndexOnMessages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Messages_ConversationId_CreatedAt",
            table: "Messages",
            columns: new[] { "ConversationId", "CreatedAt" },
            unique: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Messages_ConversationId_CreatedAt",
            table: "Messages");
    }
}
```

### Applying Migrations

```bash
# Update database to latest migration
dotnet ef database update --project src/ChatCore.Infrastructure --startup-project src/ChatCore.API

# Update to specific migration
dotnet ef database update AddIndexOnMessages

# Generate SQL script (no execution)
dotnet ef migrations script --project src/ChatCore.Infrastructure --startup-project src/ChatCore.API -o migrations.sql
```

### Rolling Back Migrations

```bash
# Remove last migration (not applied)
dotnet ef migrations remove

# Revert to previous migration
dotnet ef database update PreviousMigration
```

---

## 🧹 Data Cleanup & Maintenance

### Soft Deletes

Messages use soft deletes (not physically removed):

```csharp
// Query excludes deleted messages by default
var activeMessages = await _context.Messages
    .Where(m => m.DeletedAt == null)
    .ToListAsync();

// Query includes deleted messages
var allMessages = await _context.Messages
    .IgnoreQueryFilters()
    .ToListAsync();
```

### Archive Old Conversations

```csharp
public async Task ArchiveOldConversationsAsync(int daysOld)
{
    var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
    
    var oldConversations = await _context.Conversations
        .Where(c => c.LastMessageAt < cutoffDate && !c.IsArchived)
        .ToListAsync();
    
    foreach (var conversation in oldConversations)
    {
        conversation.IsArchived = true;
    }
    
    await _context.SaveChangesAsync();
}
```

### Clean Up Old Messages

```csharp
public async Task DeleteOldDeletedMessagesAsync(int daysOld)
{
    var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
    
    // Permanently delete messages marked as deleted before cutoff
    var messagestoDelete = await _context.Messages
        .Where(m => m.DeletedAt != null && m.DeletedAt < cutoffDate)
        .ToListAsync();
    
    _context.Messages.RemoveRange(messagestoDelete);
    await _context.SaveChangesAsync();
}
```

---

## 📊 Query Performance

### Optimized Queries

**Get conversation with recent messages:**
```csharp
var conversation = await _context.Conversations
    .AsNoTracking()
    .Include(c => c.Participants.Where(p => !p.IsBlocked))
    .ThenInclude(p => p.User)
    .Include(c => c.Messages
        .Where(m => m.DeletedAt == null)
        .OrderByDescending(m => m.CreatedAt)
        .Take(50))
    .ThenInclude(m => m.Sender)
    .FirstOrDefaultAsync(c => c.Id == id);
```

**Get unread message count:**
```csharp
var unreadCount = await _context.Messages
    .Where(m => m.ConversationId == conversationId 
        && m.CreatedAt > participant.LastReadAt
        && m.DeletedAt == null)
    .CountAsync();
```

### Pagination

```csharp
public async Task<PagedResult<MessageDto>> GetConversationMessagesAsync(
    Guid conversationId, 
    int pageNumber = 1, 
    int pageSize = 20)
{
    var query = _context.Messages
        .AsNoTracking()
        .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
        .OrderByDescending(m => m.CreatedAt);

    var totalCount = await query.CountAsync();
    
    var messages = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .Include(m => m.Sender)
        .ToListAsync();

    return new PagedResult<MessageDto>
    {
        Items = messages.Select(m => new MessageDto(m)),
        TotalCount = totalCount,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
}
```

---

## 🛡️ Data Security

### Encryption

```csharp
modelBuilder.Entity<User>()
    .Property(u => u.PasswordHash)
    .IsEncrypted();

modelBuilder.Entity<Message>()
    .Property(m => m.Content)
    .IsEncrypted();
```

### Backup Strategy

- Daily automated backups
- Test restore procedures weekly
- Maintain 30-day backup retention
- Store backups in secure, separate location

---

## 📚 See Also

- [01-architecture-overview.md](./01-architecture-overview.md)
- [02-project-structure.md](./02-project-structure.md)
- [04-domain-models.md](./04-domain-models.md)
- [EF Core Documentation](https://learn.microsoft.com/ef/core/)

---

**Last Updated:** May 29, 2026
