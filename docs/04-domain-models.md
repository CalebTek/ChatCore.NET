# Domain Models

## 📋 Overview

This document provides a complete reference for all domain entities in ChatCore.NET. These models represent the core business concepts and are located in the `ChatCore.Domain` project.

---

## 👤 User

### Entity Definition

Represents a user account in the ChatCore.NET system.

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DisplayName { get; set; }
    public string AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation Properties
    public ICollection<Participant> Participants { get; set; }
    public ICollection<Message> Messages { get; set; }
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier for the user |
| `Email` | `string` | User's email address (unique) |
| `Username` | `string` | Username for login (unique) |
| `PasswordHash` | `string` | Hashed password for security |
| `FirstName` | `string` | User's first name |
| `LastName` | `string` | User's last name |
| `DisplayName` | `string` | Name displayed in UI |
| `AvatarUrl` | `string` | URL to user's profile picture |
| `IsActive` | `bool` | Whether the account is active |
| `CreatedAt` | `DateTime` | Timestamp when user registered |
| `UpdatedAt` | `DateTime?` | Last profile update timestamp |
| `LastLoginAt` | `DateTime?` | Last login timestamp |

### Constraints

- Email must be unique and valid format
- Username must be unique and 3-50 characters
- Password must be hashed (never stored in plain text)
- Email and username are case-insensitive for comparison

### Related Entities

- **Participants:** Users participate in conversations
- **Messages:** Users send messages

---

## 💬 Conversation

### Entity Definition

Represents a chat conversation or group chat room.

```csharp
public class Conversation
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Topic { get; set; }
    public Guid CreatedByUserId { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public bool IsArchived { get; set; }
    
    // Navigation Properties
    public User CreatedByUser { get; set; }
    public ICollection<Participant> Participants { get; set; }
    public ICollection<Message> Messages { get; set; }
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier for conversation |
| `Name` | `string` | Conversation name/title |
| `Description` | `string` | Description of the conversation |
| `Topic` | `string` | Current topic being discussed |
| `CreatedByUserId` | `Guid` | ID of user who created conversation |
| `IsPrivate` | `bool` | Whether conversation is private |
| `CreatedAt` | `DateTime` | When conversation was created |
| `UpdatedAt` | `DateTime?` | Last update timestamp |
| `LastMessageAt` | `DateTime?` | Timestamp of last message |
| `IsArchived` | `bool` | Whether conversation is archived |

### Types

**Public Conversations:**
- Visible to all users
- Anyone can join
- Used for team/general discussions

**Private Conversations:**
- Only invited members can join
- Used for direct messages or closed groups
- Requires explicit participant addition

### Constraints

- Name is required and 1-100 characters
- CreatedByUserId must reference valid user
- Private conversations must have explicit participants
- Only creator or admins can modify

### Related Entities

- **CreatedByUser:** References the User who created it
- **Participants:** Users participating in conversation
- **Messages:** Messages within conversation

---

## 💭 Message

### Entity Definition

Represents a single message in a conversation.

```csharp
public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; }
    public MessageType MessageType { get; set; }
    public bool IsEdited { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? RepliedToMessageId { get; set; }
    
    // Attachments
    public ICollection<Attachment> Attachments { get; set; }
    
    // Navigation Properties
    public Conversation Conversation { get; set; }
    public User Sender { get; set; }
    public Message RepliedToMessage { get; set; }
}

public enum MessageType
{
    Text = 0,
    Image = 1,
    File = 2,
    System = 3,
    Notification = 4
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique message identifier |
| `ConversationId` | `Guid` | Parent conversation ID |
| `SenderId` | `Guid` | User who sent the message |
| `Content` | `string` | Message text content |
| `MessageType` | `MessageType` | Type of message (text, image, etc.) |
| `IsEdited` | `bool` | Whether message has been edited |
| `CreatedAt` | `DateTime` | When message was sent |
| `UpdatedAt` | `DateTime?` | When message was last edited |
| `DeletedAt` | `DateTime?` | When message was deleted (soft delete) |
| `RepliedToMessageId` | `Guid?` | ID of replied-to message (for threading) |

### Message Types

- **Text:** Standard text message
- **Image:** Message containing image content
- **File:** Message with file attachment
- **System:** Automated system message (user joined, etc.)
- **Notification:** System notification to participants

### Constraints

- Content must not be empty (except system messages)
- Content limited to 5000 characters per message
- Cannot modify deleted messages
- SenderId must reference valid user
- ConversationId must reference valid conversation

### Related Entities

- **Conversation:** Parent conversation
- **Sender:** User who sent message
- **RepliedToMessage:** Referenced message if this is a reply
- **Attachments:** Associated file attachments

---

## 👥 Participant

### Entity Definition

Represents a user's membership in a conversation.

```csharp
public class Participant
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public ParticipantRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastReadAt { get; set; }
    public bool IsMuted { get; set; }
    public bool IsPinned { get; set; }
    public bool IsBlocked { get; set; }
    
    // Navigation Properties
    public Conversation Conversation { get; set; }
    public User User { get; set; }
}

public enum ParticipantRole
{
    Member = 0,
    Moderator = 1,
    Admin = 2,
    Owner = 3
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique participant record ID |
| `ConversationId` | `Guid` | Conversation ID |
| `UserId` | `Guid` | User ID |
| `Role` | `ParticipantRole` | User's role in conversation |
| `JoinedAt` | `DateTime` | When user joined conversation |
| `LastReadAt` | `DateTime?` | Last time user read messages |
| `IsMuted` | `bool` | Whether notifications are muted |
| `IsPinned` | `bool` | Whether conversation is pinned |
| `IsBlocked` | `bool` | Whether user is blocked from conversation |

### Participant Roles

- **Member:** Standard participant, can read/send messages
- **Moderator:** Can moderate messages, manage users
- **Admin:** Can modify conversation settings
- **Owner:** Full control, typically the creator

### Constraints

- Each user can only be participant once per conversation
- Only Owner/Admin can remove/block participants
- Role can only be elevated/lowered by higher role

### Related Entities

- **Conversation:** Reference to conversation
- **User:** Reference to user

---

## 📎 Attachment

### Entity Definition

Represents a file attachment to a message.

```csharp
public class Attachment
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long FileSize { get; set; }
    public string StorageUrl { get; set; }
    public DateTime UploadedAt { get; set; }
    
    // Navigation Properties
    public Message Message { get; set; }
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique attachment identifier |
| `MessageId` | `Guid` | Parent message ID |
| `FileName` | `string` | Original file name |
| `ContentType` | `string` | MIME type (e.g., "image/jpeg") |
| `FileSize` | `long` | File size in bytes |
| `StorageUrl` | `string` | URL to download file |
| `UploadedAt` | `DateTime` | When file was uploaded |

### Constraints

- FileSize limited based on configuration
- Supported content types validated
- StorageUrl must be accessible

### Related Entities

- **Message:** Parent message

---

## 🔑 Value Objects

Value objects are immutable objects with no identity, used to represent concepts without separate storage.

### UserId

```csharp
public record UserId(Guid Value);
```

### ConversationId

```csharp
public record ConversationId(Guid Value);
```

### MessageId

```csharp
public record MessageId(Guid Value);
```

---

## 🔗 Entity Relationships

### Relationship Diagram

```
User (1) ──────────── (Many) Participant
         ──────────── (Many) Message
         ──────────── (Many) Conversation (created by)

Conversation (1) ──────────── (Many) Participant
                 ──────────── (Many) Message

Message (1) ──────────── (Many) Attachment
            ──────────── (0..1) Message (replied to)

Participant (Many) ──────────── (1) User
            (Many) ──────────── (1) Conversation
```

### Primary Keys

All entities use `Guid` as primary key for distributed traceability and security.

### Foreign Keys

- `Participant.UserId` → `User.Id`
- `Participant.ConversationId` → `Conversation.Id`
- `Message.SenderId` → `User.Id`
- `Message.ConversationId` → `Conversation.Id`
- `Message.RepliedToMessageId` → `Message.Id`
- `Conversation.CreatedByUserId` → `User.Id`
- `Attachment.MessageId` → `Message.Id`

---

## 🏗️ Database Configuration

### EF Core Configuration Files

Configuration for each entity is stored in `ChatCore.Infrastructure/Data/Configurations/`:

- `UserConfiguration.cs` - User entity mapping
- `ConversationConfiguration.cs` - Conversation entity mapping
- `MessageConfiguration.cs` - Message entity mapping
- `ParticipantConfiguration.cs` - Participant entity mapping
- `AttachmentConfiguration.cs` - Attachment entity mapping

Each configuration specifies:
- Column names and types
- Maximum lengths
- Required/optional fields
- Indexes
- Relationships

---

## 📊 DTO Reference

Data Transfer Objects used in API communication:

### UserDto

```csharp
public record UserDto(
    Guid Id,
    string Email,
    string Username,
    string DisplayName,
    string AvatarUrl,
    DateTime CreatedAt
);
```

### ConversationDto

```csharp
public record ConversationDto(
    Guid Id,
    string Name,
    string Description,
    Guid CreatedByUserId,
    bool IsPrivate,
    int ParticipantCount,
    DateTime CreatedAt,
    DateTime? LastMessageAt
);
```

### MessageDto

```csharp
public record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string SenderName,
    string Content,
    string MessageType,
    DateTime CreatedAt,
    bool IsEdited
);
```

### ParticipantDto

```csharp
public record ParticipantDto(
    Guid Id,
    Guid ConversationId,
    Guid UserId,
    string UserName,
    string Role,
    DateTime JoinedAt
);
```

---

## 📚 See Also

- [01-architecture-overview.md](./01-architecture-overview.md) - System architecture
- [02-project-structure.md](./02-project-structure.md) - Project layout
- [03-quick-start-guide.md](./03-quick-start-guide.md) - Setup and API examples

---

**Last Updated:** May 29, 2026
