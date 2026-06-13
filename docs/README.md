# ChatCore.NET Documentation

Welcome to the complete documentation for **ChatCore.NET** — a modular, scalable, SignalR-powered chat framework for .NET 8.

---

## 📚 Documentation Index

### Getting Started

| File | Description |
|------|-------------|
| [01-architecture-overview.md](./01-architecture-overview.md) | System layers, data flow, and key design decisions |
| [02-project-structure.md](./02-project-structure.md) | Directory layout and module dependencies |
| [03-quick-start-guide.md](./03-quick-start-guide.md) | Installation, wiring, and first messages |

### Core Reference

| File | Description |
|------|-------------|
| [04-domain-models.md](./04-domain-models.md) | All domain entities, enums, DTOs, and relationships |
| [05-signalr-hub-reference.md](./05-signalr-hub-reference.md) | Hub methods, server-push events, client connection setup |
| [06-api-endpoints.md](./06-api-endpoints.md) | `IChatEngine` operations, request/response shapes, error codes |

### Infrastructure & Quality

| File | Description |
|------|-------------|
| [07-database-schema.md](./07-database-schema.md) | Table definitions, indexes, EF Core config, migrations |
| [08-testing-strategies.md](./08-testing-strategies.md) | Unit tests, integration tests, conventions, coverage gaps |
| [09-configuration.md](./09-configuration.md) | Registration, connection strings, auth, interceptors, Docker |

### Community

| File | Description |
|------|-------------|
| [10-contributing-guidelines.md](./10-contributing-guidelines.md) | Branching, PR process, coding standards, test requirements |
| [../CODE_OF_CONDUCT.md](../CODE_OF_CONDUCT.md) | Community standards and enforcement |
| [../TROUBLESHOOTING.md](../TROUBLESHOOTING.md) | Common errors and solutions |

---

## 🗺️ Where to Start

**"What is this and how does it work?"** → [01-architecture-overview.md](./01-architecture-overview.md)

**"I want to get it running in my app."** → [03-quick-start-guide.md](./03-quick-start-guide.md) → [09-configuration.md](./09-configuration.md)

**"What SignalR events does the client need to listen for?"** → [05-signalr-hub-reference.md](./05-signalr-hub-reference.md)

**"What does the engine return and what error codes exist?"** → [06-api-endpoints.md](./06-api-endpoints.md)

**"How do I write or run tests?"** → [08-testing-strategies.md](./08-testing-strategies.md)

**"I want to contribute."** → [10-contributing-guidelines.md](./10-contributing-guidelines.md)

---

**Last Updated:** June 13, 2026
