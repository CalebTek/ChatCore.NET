# SignalR Hub Reference

## 📡 Overview

ChatCore.NET uses **SignalR** for all real-time communication. The `ChatHub` is the single hub that clients connect to for sending messages, receiving broadcasts, tracking presence, and managing read receipts.

**Default endpoint:** `/hubs/chat` (configurable via `MapChatHub`)

---

## 🔌 Connection

### Client Connection Setup

```typescript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/chat", {
        accessTokenFactory: () => yourJwtToken
    })
    .withAutomaticReconnect()
    .build();

await connection.start();
```

```csharp
// .NET client
var connection = new HubConnectionBuilder()
    .WithUrl("https://yourapp.com/hubs/chat", options =>
    {
        options.AccessTokenProvider = () => Task.FromResult(yourJwtToken);
    })
    .WithAutomaticReconnect()
    .Build();

await connection.StartAsync();
```

### Authentication

The hub reads the authenticated user's ID from the `sub` claim of the JWT token:

```csharp
var userId = Context.User?.FindFirst("sub")?.Value;
```

Clients **must** send a valid Bearer token. Unauthenticated connections will receive `"Error"` events for any operation that requires a user identity.

### Lifecycle Events

| Event | Server Behaviour |
|-------|-----------------|
| `OnConnectedAsync` | Records the user as online via `IPresenceProvider.MarkOnlineAsync` |
| `OnDisconnectedAsync` | Removes the user's connection via `IPresenceProvider.MarkOfflineAsync` |

A single user may have multiple simultaneous connections (e.g., browser + mobile). Each connection is tracked independently.

---

## 📤 Client → Server Methods

These are methods the client **invokes** on the hub.

---

### `SendMessage`

Sends a message to a conversation. The engine validates the sender is a participant, assigns a sequence number, persists the message, and broadcasts it to the conversation group.

**Signature:**
```
SendMessage(conversationId: Guid, content: string, tenantId: Guid)
```

**Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `conversationId` | `Guid` | Target conversation |
| `content` | `string` | Message text (must not be empty) |
| `tenantId` | `Guid` | Tenant the conversation belongs to |

**JavaScript:**
```typescript
await connection.invoke("SendMessage",
    "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "Hello, world!",
    "11111111-1111-1111-1111-111111111111"
);
```

**On success:** All clients in `conversation_{conversationId}` receive a `MessageReceived` event.

**On failure:** The caller receives an `Error` event with the error message.

**Possible error codes:**

| Error Code | Meaning |
|------------|---------|
| `EMPTY_CONTENT` | Message content was blank |
| `INVALID_CONVERSATION_ID` | ConversationId is `Guid.Empty` |
| `NOT_PARTICIPANT` | Sender is not in the conversation |
| `CONVERSATION_NOT_FOUND` | Conversation does not exist for the tenant |
| `MESSAGE_CANCELLED` | A pre-send interceptor cancelled the message |
| `SEND_FAILED` | Unexpected persistence error |

---

### `MarkAsRead`

Records that the authenticated user has read up to a given sequence number in a conversation.

**Signature:**
```
MarkAsRead(conversationId: Guid, lastReadSequence: long, tenantId: Guid)
```

**Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `conversationId` | `Guid` | The conversation |
| `lastReadSequence` | `long` | The highest sequence number the user has seen |
| `tenantId` | `Guid` | Tenant identifier |

**JavaScript:**
```typescript
await connection.invoke("MarkAsRead",
    "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    42,
    "11111111-1111-1111-1111-111111111111"
);
```

**On success:** All clients in the conversation group receive a `MessageRead` event.

**On failure:** The caller receives an `Error` event.

---

### `JoinConversation`

Adds the current connection to a SignalR group so it receives real-time events for that conversation. **Call this after connecting** for every conversation the user is participating in.

**Signature:**
```
JoinConversation(conversationId: Guid)
```

**JavaScript:**
```typescript
await connection.invoke("JoinConversation",
    "3fa85f64-5717-4562-b3fc-2c963f66afa6"
);
```

**On success:** All clients in the group receive a `UserJoined` event containing the connection ID.

> ⚠️ **Important:** Group membership is per-connection, not per-user. If a user has multiple connections, each must independently call `JoinConversation`.

---

### `LeaveConversation`

Removes the current connection from a conversation group.

**Signature:**
```
LeaveConversation(conversationId: Guid)
```

**JavaScript:**
```typescript
await connection.invoke("LeaveConversation",
    "3fa85f64-5717-4562-b3fc-2c963f66afa6"
);
```

**On success:** All clients in the group receive a `UserLeft` event.

---

### `IsTyping`

Broadcasts a typing indicator to all participants in a conversation.

**Signature:**
```
IsTyping(conversationId: Guid)
```

**JavaScript:**
```typescript
await connection.invoke("IsTyping",
    "3fa85f64-5717-4562-b3fc-2c963f66afa6"
);
```

**On success:** All clients in the group receive a `UserTyping` event with the sender's user ID (or connection ID if unauthenticated).

> 💡 **Best practice:** Debounce this call on the client — fire it when the user starts typing, then stop sending once they pause for ~2–3 seconds.

---

### `StoppedTyping`

Cancels a typing indicator previously broadcast via `IsTyping`.

**Signature:**
```
StoppedTyping(conversationId: Guid)
```

**JavaScript:**
```typescript
await connection.invoke("StoppedTyping",
    "3fa85f64-5717-4562-b3fc-2c963f66afa6"
);
```

**On success:** All clients in the group receive a `UserStoppedTyping` event.

---

## 📥 Server → Client Events

These are events the server **pushes** to connected clients.

---

### `MessageReceived`

Fired when a new message is sent to a conversation the client has joined.

**Payload:**
```typescript
interface MessageReceivedPayload {
    id: string;              // Guid
    conversationId: string;  // Guid
    senderId: string;        // Guid
    content: string;
    sequenceNumber: number;
    sentAt: string;          // ISO 8601 UTC
    isDeleted: boolean;
}
```

**Listening:**
```typescript
connection.on("MessageReceived", (message) => {
    console.log(`[${message.sequenceNumber}] ${message.content}`);
});
```

---

### `MessageRead`

Fired when a user marks messages as read.

**Payload:**
```typescript
// conversationId: string (Guid)
// userId: string (Guid)
// lastReadSequence: number
```

**Listening:**
```typescript
connection.on("MessageRead", (conversationId, userId, lastReadSequence) => {
    updateReadReceipt(conversationId, userId, lastReadSequence);
});
```

---

### `UserJoined`

Fired when a connection joins a conversation group.

**Payload:** `connectionId: string`

---

### `UserLeft`

Fired when a connection leaves a conversation group.

**Payload:** `connectionId: string`

---

### `UserTyping`

Fired when a user starts typing.

**Payload:** `userId: string` (the user's ID or connection ID)

---

### `UserStoppedTyping`

Fired when a user stops typing.

**Payload:** `userId: string`

---

### `Error`

Fired on the **calling client only** when a hub method fails.

**Payload:** `message: string`

```typescript
connection.on("Error", (message) => {
    console.error("Hub error:", message);
});
```

---

## 🗺️ Hub Registration

Register the hub in `Program.cs` using the provided extension methods:

```csharp
// Registration
builder.Services
    .AddChatCore()
    .UseEntityFramework(connectionString)
    .UseSignalR()
    .Build();

// Mapping
app.MapChatHub("/hubs/chat");  // Default pattern
// or custom path:
app.MapChatHub("/api/realtime/chat");
```

---

## 🔄 Recommended Client Workflow

```
1. Authenticate → obtain JWT token
2. Connect to hub with token
3. For each conversation the user is in:
       → JoinConversation(conversationId)
4. Listen for: MessageReceived, MessageRead, UserTyping, UserStoppedTyping
5. On message send: invoke SendMessage(...)
6. On reading messages: invoke MarkAsRead(...)
7. On disconnect: hub automatically marks user offline
```

---

## ⚠️ Known Limitations & Planned Improvements

| Area | Current State | Planned Fix |
|------|--------------|-------------|
| Sender exclusion | `excludeUserId` parameter in dispatcher is accepted but not applied — sender receives their own message back | Filter sender from group broadcast |
| Online user listing | `GetOnlineUsersAsync` always returns empty | Query distinct users from `UserConnections` table |
| Group membership | No server-side check that the user is a participant before adding to SignalR group | Validate participant status in `JoinConversation` |
| Redis backplane | Not yet configured — horizontal scaling is limited | Add `AddStackExchangeRedis` to SignalR config |

---

**See Also:** [06-api-endpoints.md](./06-api-endpoints.md) | [09-configuration.md](./09-configuration.md)
