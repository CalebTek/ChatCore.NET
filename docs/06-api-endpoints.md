# REST API Endpoints

## 📋 Overview

ChatCore.NET exposes its chat functionality primarily through the SignalR hub for real-time operations. This document covers the REST-style API surface that is available via the `IChatEngine` interface, how to wire up HTTP controllers on top of it, and the full request/response contracts for each operation.

---

## 🧱 Core Engine Interface

All operations go through `IChatEngine`. Whether you expose them via minimal API, MVC controllers, or gRPC, the engine is the single entry point.

```csharp
public interface IChatEngine
{
    Task<ChatResult<ChatMessageDto>>                    SendAsync(ChatMessageRequest request, CancellationToken ct = default);
    Task<ChatResult<bool>>                              MarkAsReadAsync(MarkAsReadRequest request, CancellationToken ct = default);
    Task<ChatResult<PaginatedResult<ChatMessageDto>>>   GetMessagesAsync(GetMessagesQuery query, CancellationToken ct = default);
    Task<ChatResult<IEnumerable<ConversationDto>>>      GetConversationsAsync(GetConversationsQuery query, CancellationToken ct = default);
    Task<ChatResult<bool>>                              DeleteMessageAsync(Guid messageId, Guid userId, Guid tenantId, CancellationToken ct = default);
}
```

---

## 📦 Shared Response Envelope

Every engine call returns `ChatResult<T>`:

```json
// Success
{
  "isSuccess": true,
  "data": { ... },
  "error": null,
  "errorCode": null
}

// Failure
{
  "isSuccess": false,
  "data": null,
  "error": "Sender is not a participant in this conversation",
  "errorCode": "NOT_PARTICIPANT"
}
```

### Error Code Reference

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| `INVALID_REQUEST` | 400 | Request object is null |
| `EMPTY_CONTENT` | 400 | Message content is blank |
| `INVALID_CONVERSATION_ID` | 400 | ConversationId is Guid.Empty |
| `INVALID_SENDER_ID` | 400 | SenderId is Guid.Empty |
| `INVALID_USER_ID` | 400 | UserId is Guid.Empty |
| `INVALID_MESSAGE_ID` | 400 | MessageId is Guid.Empty |
| `NOT_PARTICIPANT` | 403 | Sender not in conversation |
| `UNAUTHORIZED` | 403 | Operation not permitted (e.g. deleting another user's message) |
| `CONVERSATION_NOT_FOUND` | 404 | Conversation not found for tenant |
| `MESSAGE_NOT_FOUND` | 404 | Message not found for tenant |
| `MESSAGE_CANCELLED` | 422 | Pre-send interceptor cancelled send |
| `SEND_FAILED` | 500 | Unexpected persistence failure |
| `MARK_READ_FAILED` | 500 | Unexpected read-receipt failure |
| `GET_MESSAGES_FAILED` | 500 | Unexpected message retrieval failure |
| `GET_CONVERSATIONS_FAILED` | 500 | Unexpected conversation retrieval failure |
| `DELETE_FAILED` | 500 | Unexpected delete failure |

---

## 📬 Operations

---

### Send Message

**Engine method:** `SendAsync`

**Suggested HTTP:** `POST /api/conversations/{conversationId}/messages`

#### Request

```csharp
public class ChatMessageRequest
{
    public Guid   ConversationId  { get; set; }
    public Guid   SenderId        { get; set; }
    public string Content         { get; set; }
    public string? IdempotencyKey { get; set; }  // Optional — prevents duplicate sends
    public Guid   TenantId        { get; set; }
}
```

```json
{
  "conversationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "senderId":       "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "content":        "Hello, World!",
  "idempotencyKey": "client-generated-uuid-or-hash",
  "tenantId":       "11111111-1111-1111-1111-111111111111"
}
```

#### Response

```csharp
public class ChatMessageDto
{
    public Guid          Id             { get; set; }
    public Guid          ConversationId { get; set; }
    public Guid          SenderId       { get; set; }
    public string        Content        { get; set; }
    public long          SequenceNumber { get; set; }
    public DateTime      SentAt         { get; set; }
    public bool          IsDeleted      { get; set; }
    public MessageStatus Status         { get; set; }
}
```

```json
{
  "isSuccess": true,
  "data": {
    "id":             "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    "conversationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "senderId":       "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "content":        "Hello, World!",
    "sequenceNumber": 1,
    "sentAt":         "2026-06-13T10:00:00Z",
    "isDeleted":      false,
    "status":         0
  }
}
```

#### Idempotency

If `IdempotencyKey` is provided and a message with that key already exists for the tenant, the existing message is returned as-is with no side effects. Use a client-generated UUID or hash to safely retry on network failure without creating duplicates.

#### Controller Example

```csharp
[HttpPost("{conversationId}/messages")]
public async Task<IActionResult> SendMessage(
    Guid conversationId,
    [FromBody] SendMessageBody body,
    CancellationToken ct)
{
    var request = new ChatMessageRequest
    {
        ConversationId = conversationId,
        SenderId       = User.GetUserId(),
        TenantId       = User.GetTenantId(),
        Content        = body.Content,
        IdempotencyKey = body.IdempotencyKey
    };

    var result = await _engine.SendAsync(request, ct);
    return result.IsSuccess
        ? Ok(result)
        : MapError(result);
}
```

---

### Get Messages

**Engine method:** `GetMessagesAsync`

**Suggested HTTP:** `GET /api/conversations/{conversationId}/messages`

Uses **seek-based (cursor) pagination** — pass the sequence number of the oldest message you have to load the page before it, enabling infinite scroll without skip/offset performance degradation.

#### Query Parameters

```csharp
public class GetMessagesQuery
{
    public Guid ConversationId  { get; set; }
    public Guid UserId          { get; set; }
    public Guid TenantId        { get; set; }
    public long LastSeenSequence { get; set; } = long.MaxValue;  // Start from the newest
    public int  PageSize        { get; set; } = 50;
}
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `lastSeenSequence` | `long.MaxValue` | Load messages with sequence < this value |
| `pageSize` | `50` | Max messages per page (recommend ≤ 100) |

#### Response

```csharp
public class PaginatedResult<T>
{
    public IEnumerable<T> Items      { get; }   // Messages, ascending order
    public bool           HasMore    { get; }   // More pages available
    public long?          NextCursor { get; }   // Pass as lastSeenSequence for next page
    public int            Count      { get; }
}
```

```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": "...",
        "sequenceNumber": 1,
        "content": "Hello",
        "sentAt": "2026-06-13T09:58:00Z",
        "isDeleted": false
      },
      {
        "id": "...",
        "sequenceNumber": 2,
        "content": "World",
        "sentAt": "2026-06-13T09:59:00Z",
        "isDeleted": false
      }
    ],
    "hasMore": true,
    "nextCursor": 1,
    "count": 2
  }
}
```

#### Pagination Walkthrough

```
First load:  lastSeenSequence = long.MaxValue   → returns messages 50..100 (newest 50)
Next page:   lastSeenSequence = nextCursor (50) → returns messages 1..49
No more:     hasMore = false
```

#### Controller Example

```csharp
[HttpGet("{conversationId}/messages")]
public async Task<IActionResult> GetMessages(
    Guid conversationId,
    [FromQuery] long lastSeenSequence = long.MaxValue,
    [FromQuery] int  pageSize         = 50,
    CancellationToken ct              = default)
{
    var query = new GetMessagesQuery
    {
        ConversationId  = conversationId,
        UserId          = User.GetUserId(),
        TenantId        = User.GetTenantId(),
        LastSeenSequence = lastSeenSequence,
        PageSize        = pageSize
    };

    var result = await _engine.GetMessagesAsync(query, ct);
    return result.IsSuccess ? Ok(result) : MapError(result);
}
```

---

### Get Conversations

**Engine method:** `GetConversationsAsync`

**Suggested HTTP:** `GET /api/conversations`

Returns a page of conversations the authenticated user participates in, ordered newest-first by creation date.

#### Query Parameters

```csharp
public class GetConversationsQuery
{
    public Guid UserId   { get; set; }
    public Guid TenantId { get; set; }
    public int  Page     { get; set; } = 1;
    public int  PageSize { get; set; } = 20;
}
```

#### Response

```csharp
public class ConversationDto
{
    public Guid                 Id             { get; set; }
    public ConversationType     Type           { get; set; }  // 0 = Direct, 1 = Group
    public DateTime             CreatedAt      { get; set; }
    public IEnumerable<Guid>    ParticipantIds { get; set; }
}
```

```json
{
  "isSuccess": true,
  "data": [
    {
      "id":             "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "type":           0,
      "createdAt":      "2026-06-01T08:00:00Z",
      "participantIds": []
    }
  ]
}
```

> **Note:** `participantIds` is currently not populated by the engine — this is a known enhancement item. See the enhancement notes at the bottom of this document.

---

### Mark As Read

**Engine method:** `MarkAsReadAsync`

**Suggested HTTP:** `PUT /api/conversations/{conversationId}/read`

Records the user's read position. Only a single "high-water mark" per user/conversation is stored — there is no per-message read flag.

#### Request

```csharp
public class MarkAsReadRequest
{
    public Guid ConversationId  { get; set; }
    public Guid UserId          { get; set; }
    public long LastReadSequence { get; set; }
    public Guid TenantId        { get; set; }
}
```

```json
{
  "lastReadSequence": 42
}
```

#### Response

```json
{
  "isSuccess": true,
  "data": true
}
```

---

### Delete Message

**Engine method:** `DeleteMessageAsync`

**Suggested HTTP:** `DELETE /api/conversations/{conversationId}/messages/{messageId}`

Soft-deletes a message. The message is retained in the database but its content is replaced with `"[deleted]"` and `isDeleted` is set to `true`. Only the original sender can delete their own message.

#### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `messageId` | `Guid` | The message to delete |
| `userId` | `Guid` | Must match the original `SenderId` |
| `tenantId` | `Guid` | Tenant scope |

#### Response

```json
{
  "isSuccess": true,
  "data": true
}
```

#### Controller Example

```csharp
[HttpDelete("{conversationId}/messages/{messageId}")]
public async Task<IActionResult> DeleteMessage(
    Guid conversationId,
    Guid messageId,
    CancellationToken ct)
{
    var result = await _engine.DeleteMessageAsync(
        messageId,
        User.GetUserId(),
        User.GetTenantId(),
        ct);

    return result.IsSuccess ? NoContent() : MapError(result);
}
```

---

## 🔧 Error Mapping Helper

A utility for mapping `ChatResult` errors to HTTP status codes:

```csharp
private IActionResult MapError<T>(ChatResult<T> result)
{
    return result.ErrorCode switch
    {
        "INVALID_REQUEST"
        or "EMPTY_CONTENT"
        or "INVALID_CONVERSATION_ID"
        or "INVALID_SENDER_ID"
        or "INVALID_USER_ID"
        or "INVALID_MESSAGE_ID"    => BadRequest(result),

        "NOT_PARTICIPANT"
        or "UNAUTHORIZED"          => Forbid(),

        "CONVERSATION_NOT_FOUND"
        or "MESSAGE_NOT_FOUND"     => NotFound(result),

        "MESSAGE_CANCELLED"        => UnprocessableEntity(result),

        _                          => StatusCode(500, result)
    };
}
```

---

## 📝 Planned Enhancements

| Operation | Enhancement |
|-----------|-------------|
| `GetConversations` | Populate `ParticipantIds` on `ConversationDto` |
| `GetConversations` | Switch to seek-based pagination (currently offset-based) |
| `SendMessage` | Add `MessageStatus` update to `Sent` after persistence |
| All writes | Wrap message + conversation update in a database transaction |
| `DeleteMessage` | Broadcast deletion event via SignalR transport |

---

**See Also:** [05-signalr-hub-reference.md](./05-signalr-hub-reference.md) | [04-domain-models.md](./04-domain-models.md)
