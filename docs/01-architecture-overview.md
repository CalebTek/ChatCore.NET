# Architecture Overview

## 🏗️ System Layers

ChatCore.NET is built with a **layered architecture** designed for modularity, scalability, and maintainability. The system is organized into the following layers:

### 1. **Presentation Layer**
- **Purpose:** Handles HTTP requests and WebSocket communication via SignalR
- **Components:**
  - ASP.NET Core controllers for REST API endpoints
  - SignalR hubs for real-time chat communication
  - Request/response handling and validation
  - Output formatting (JSON, etc.)

### 2. **Application Layer**
- **Purpose:** Business logic and orchestration
- **Components:**
  - Application services (use cases)
  - DTOs (Data Transfer Objects) for API contracts
  - Mapping between domain and presentation layers
  - Command and query handlers
  - Cross-cutting concerns (logging, validation, exception handling)

### 3. **Domain Layer**
- **Purpose:** Core business logic and domain models
- **Components:**
  - Domain entities (Conversation, Message, Participant, User, etc.)
  - Value objects
  - Domain services
  - Business rules and constraints
  - **Note:** Domain layer is independent of any framework or external dependency

### 4. **Infrastructure Layer**
- **Purpose:** Technical implementation and external integrations
- **Components:**
  - Database context and repositories
  - Entity Framework Core mappings
  - External service integrations
  - Authentication/Authorization implementations
  - Caching mechanisms
  - File storage services

## 📊 Data Flow

### Typical Request Flow

```
Client Request
    ↓
[Presentation Layer - Controller/Hub]
    ↓
[Application Layer - Service]
    ↓
[Domain Layer - Domain Logic]
    ↓
[Infrastructure Layer - Repository/Database]
    ↓
Database/External Services
    ↓
[Response Back Through Layers]
    ↓
Client Response
```

### Real-Time Chat Flow (SignalR)

```
Client → SignalR Hub
    ↓
[Hub receives message]
    ↓
[Application Service processes]
    ↓
[Domain logic validates]
    ↓
[Repository persists]
    ↓
[Hub broadcasts to connected clients]
    ↓
All Connected Clients Receive Update
```

## 🎯 Key Design Decisions

### 1. **Separation of Concerns**
- Each layer has a specific responsibility
- Domain layer remains framework-agnostic
- Infrastructure is abstracted behind interfaces

### 2. **Dependency Injection**
- All services use constructor injection
- Loose coupling between components
- Testability through mock implementations

### 3. **SOLID Principles**
- **S**ingle Responsibility: Each class has one reason to change
- **O**pen/Closed: Open for extension, closed for modification
- **L**iskov Substitution: Derived classes can substitute base classes
- **I**nterface Segregation: Clients depend on specific interfaces
- **D**ependency Inversion: Depend on abstractions, not concretions

### 4. **Repository Pattern**
- Abstracts data access logic
- Allows easy switching between data sources
- Facilitates unit testing with in-memory implementations

### 5. **SignalR for Real-Time Communication**
- Automatic fallback to long-polling if WebSocket unavailable
- Built-in reconnection handling
- Scalable hub-based communication pattern

### 6. **Asynchronous Operations**
- Heavy use of async/await for non-blocking I/O
- Improved scalability and responsiveness
- Better resource utilization

## 🔌 Core Components

### Domain Models
- **Conversation:** Represents a chat conversation
- **Message:** Individual chat message within a conversation
- **Participant:** User participating in a conversation
- **User:** System user with authentication credentials

### Services
- **ConversationService:** Manages conversation lifecycle
- **MessageService:** Handles message creation and retrieval
- **ParticipantService:** Manages conversation participants
- **AuthenticationService:** User authentication and authorization

### Data Access
- **DbContext:** Entity Framework Core context
- **Repositories:** Generic and specific repository patterns
- **Unit of Work:** Transaction coordination (optional implementation)

## 🔄 Scalability Considerations

1. **Horizontal Scaling:** SignalR can work with sticky sessions or a backplane (Redis, Service Bus)
2. **Database Optimization:** Indexed queries, connection pooling, read replicas
3. **Caching Strategies:** Redis for frequently accessed data
4. **Message Queuing:** Async operations using background jobs
5. **Load Balancing:** Distribute traffic across multiple instances

## 🛡️ Security Layers

1. **Authentication:** JWT or OAuth2 tokens
2. **Authorization:** Role-based access control (RBAC)
3. **Input Validation:** Server-side validation on all inputs
4. **Data Protection:** Encryption at rest and in transit
5. **Hub Security:** Authorization checks on SignalR methods

---

**See Also:** [02-project-structure.md](./02-project-structure.md) for directory layout and module organization.
