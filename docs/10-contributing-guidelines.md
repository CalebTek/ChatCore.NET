# Contributing Guidelines

## 🤝 Welcome Contributors!

ChatCore.NET is an open-source project and we welcome contributions from the community. This guide explains how to contribute effectively.

---

## 📋 Code of Conduct

We are committed to providing a welcoming and inspiring community for all. Please read and follow our [Code of Conduct](./CODE_OF_CONDUCT.md).

**Be respectful, inclusive, and professional in all interactions.**

---

## 🎯 Ways to Contribute

### 1. Report Bugs

Found a bug? Help us fix it!

**Before reporting:**
- Check if issue already exists
- Verify it's reproducible
- Gather system information

**Report format:**
```markdown
**Description:** Clear description of the bug

**Steps to Reproduce:**
1. Step 1
2. Step 2
3. Bug occurs

**Expected Behavior:** What should happen

**Actual Behavior:** What actually happens

**System Info:**
- OS: Windows 10
- .NET Version: 8.0.1
- ChatCore.NET Version: 1.0.0

**Screenshots/Logs:** Any relevant attachments
```

### 2. Suggest Features

Have an idea? We'd love to hear it!

**Before suggesting:**
- Check if feature exists or is planned
- Explain use case clearly
- Consider scope and impact

**Feature format:**
```markdown
**Problem:** What problem does this solve?

**Proposed Solution:** How should this work?

**Examples:** Use case examples

**Alternatives Considered:** Other approaches?

**Impact:** Who benefits? Any breaking changes?
```

### 3. Submit Code Changes

Want to code? Here's how:

- Fork the repository
- Create a feature branch
- Make your changes
- Submit a pull request
- Participate in code review

### 4. Improve Documentation

Documentation improvements are always welcome!

- Fix typos
- Add examples
- Clarify instructions
- Update outdated info
- Add missing sections

### 5. Help the Community

Help others by:
- Answering questions
- Mentoring new contributors
- Sharing your experience
- Creating tutorials

---

## 🔧 Development Setup

### Prerequisites

- **.NET 8.0 SDK** or later
- **SQL Server** (LocalDB or full)
- **Visual Studio 2022** or **VS Code**
- **Git**

### Local Environment Setup

```bash
# 1. Fork and clone
git clone https://github.com/YOUR_USERNAME/ChatCore.NET.git
cd ChatCore.NET

# 2. Add upstream remote
git remote add upstream https://github.com/CalebTek/ChatCore.NET.git

# 3. Restore dependencies
dotnet restore

# 4. Configure database
# Update appsettings.Development.json with your connection string

# 5. Apply migrations
dotnet ef database update --project src/ChatCore.Infrastructure --startup-project src/ChatCore.API

# 6. Run tests
dotnet test

# 7. Start development server
cd src/ChatCore.API
dotnet run
```

---

## 📝 Making Changes

### Branch Naming Convention

```
feature/short-description      # New feature
bugfix/issue-number            # Bug fix
docs/section-name              # Documentation
refactor/area-name             # Code refactoring
perf/optimization-name         # Performance improvement
```

**Examples:**
- `feature/real-time-notifications`
- `bugfix/issue-123-login-fail`
- `docs/authentication-guide`
- `refactor/message-service`

### Commit Message Format

```
type(scope): subject

body

footer
```

**Example:**
```
feat(messages): add message reply functionality

Implement reply-to messages feature:
- Add RepliedToMessageId column to Message entity
- Update MessageService with reply logic
- Create UI components for message replies

Fixes #456
```

**Type:** `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`

**Scope:** `messages`, `auth`, `ui`, `db`, etc.

**Subject:** Present tense, no period, max 50 characters

### Pull Request Process

#### 1. Create Pull Request

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Related Issues
Fixes #(issue number)

## Testing
- [ ] Unit tests added
- [ ] Integration tests added
- [ ] Manual testing done

## Checklist
- [ ] Code follows style guide
- [ ] Self-review completed
- [ ] Comments added to complex logic
- [ ] Documentation updated
- [ ] Tests pass locally
- [ ] No new warnings
```

#### 2. Code Review Process

- **Maintainers** will review your code
- **Address feedback** constructively
- **Iterate** until approval
- **PR merged** after approval and passing CI

#### 3. After Merge

- Your code is deployed in next release
- You're added to contributors list
- Celebrate your contribution! 🎉

---

## ✅ Coding Standards

### General Principles

- **Clean Code:** Readable, maintainable code
- **SOLID:** Follow SOLID principles
- **DRY:** Don't repeat yourself
- **KISS:** Keep it simple, stupid
- **YAGNI:** You aren't gonna need it

### C# Style Guide

```csharp
// Naming conventions
public class UserService { }           // PascalCase for classes
public interface IUserRepository { }    // I prefix for interfaces
private string _userName;              // _ prefix for private fields
public const int MaxConnections = 10;  // UPPER_CASE for constants
public List<User> GetUsers() { }       // PascalCase for methods

// Formatting
public async Task<User> GetUserAsync(Guid id)
{
    // 4 spaces indentation
    if (id == Guid.Empty)
        throw new ArgumentException(nameof(id));

    var user = await _repository.GetByIdAsync(id);
    
    return user ?? throw new NotFoundException("User not found");
}

// Use var when type is obvious
var users = await _repository.GetAllAsync();

// Use meaningful names
var isActive = user.IsActive;  // Good
var ia = user.IsActive;        // Bad

// Comments explain WHY, not WHAT
// Retry with exponential backoff (Max 3 attempts)
for (int i = 0; i < 3; i++)
{
    try { break; }
    catch { await Task.Delay(1000 * (i + 1)); }
}
```

### File Organization

```
Namespace: ChatCore.[Layer].[Feature]
File Name: ClassName.cs

Example:
- Namespace: ChatCore.Application.Services
- File: MessageService.cs
- Location: src/ChatCore.Application/Services/MessageService.cs
```

---

## 🧪 Testing Requirements

### Test Coverage

- **Target:** ≥ 80% code coverage
- **Domain:** ≥ 90% coverage
- **Application:** ≥ 85% coverage

### Test File Location

```
tests/
├── ChatCore.Domain.Tests/
│   └── [FeatureName]/
│       └── [ClassName]Tests.cs
├── ChatCore.Application.Tests/
│   └── Services/
│       └── [ServiceName]Tests.cs
└── ChatCore.API.Tests/
    └── Controllers/
        └── [ControllerName]ControllerTests.cs
```

### Writing Tests

```csharp
public class MessageServiceTests
{
    // Follow Arrange-Act-Assert pattern
    [Fact]
    public async Task CreateMessage_WithValidData_ReturnsMessage()
    {
        // Arrange - Setup test data and mocks
        var mockRepository = new Mock<IMessageRepository>();
        var service = new MessageService(mockRepository.Object);

        // Act - Execute the method
        var result = await service.CreateMessageAsync(conversationId, userId, content);

        // Assert - Verify results
        result.Should().NotBeNull();
    }

    // Use descriptive names: Method_Scenario_Expected
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CreateMessage_WithEmptyContent_ThrowsException(string content)
    {
        // Test multiple scenarios
    }
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific project
dotnet test tests/ChatCore.Domain.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test
dotnet test --filter Name~CreateMessage
```

---

## 📚 Documentation

### What to Document

- ✅ Public APIs and methods
- ✅ Complex business logic
- ✅ Configuration options
- ✅ Breaking changes
- ✅ Usage examples

### XML Documentation

```csharp
/// <summary>
/// Creates a new message in the specified conversation.
/// </summary>
/// <param name="conversationId">The conversation identifier.</param>
/// <param name="userId">The sender user identifier.</param>
/// <param name="content">The message content (max 5000 chars).</param>
/// <returns>The created message.</returns>
/// <exception cref="ArgumentException">Thrown when content is empty or too long.</exception>
/// <exception cref="NotFoundException">Thrown when conversation doesn't exist.</exception>
public async Task<Message> CreateMessageAsync(
    Guid conversationId, 
    Guid userId, 
    string content)
{
    // Implementation
}
```

### Updating Documentation Files

When adding features:
1. Update relevant markdown files in `/docs`
2. Update code comments
3. Add examples if applicable
4. Update README if needed

---

## 🔍 Code Review Checklist

As a contributor:
- [ ] Code follows style guide
- [ ] Tests added for new functionality
- [ ] Existing tests pass
- [ ] Documentation updated
- [ ] No hardcoded values
- [ ] Proper error handling
- [ ] Security implications considered
- [ ] Performance impact assessed

As a reviewer:
- [ ] Code is clear and maintainable
- [ ] No duplicate functionality
- [ ] Tests adequately cover changes
- [ ] Documentation is accurate
- [ ] Follows project standards
- [ ] No security vulnerabilities
- [ ] Performance acceptable

---

## 🐛 Bug Fix Workflow

```
1. Find/Report Bug
   └─ Create issue with reproduction steps

2. Create Feature Branch
   └─ git checkout -b bugfix/issue-number

3. Write Failing Test
   └─ Test that reproduces the bug

4. Fix the Bug
   └─ Make test pass

5. Verify Fix
   └─ Run full test suite

6. Submit Pull Request
   └─ Reference issue number

7. Code Review
   └─ Address feedback

8. Merge & Deploy
   └─ Bug fixed in next release
```

---

## 🎁 Feature Development Workflow

```
1. Discuss Feature
   └─ Create issue for discussion

2. Design & Plan
   └─ Plan implementation approach

3. Create Feature Branch
   └─ git checkout -b feature/feature-name

4. Implement Feature
   └─ Write tests first (TDD)
   └─ Implement functionality
   └─ Document changes

5. Local Testing
   └─ Run full test suite
   └─ Manual testing
   └─ Test edge cases

6. Submit Pull Request
   └─ Detailed description
   └─ Screenshots/demos if applicable

7. Code Review
   └─ Iterate on feedback

8. Merge & Deploy
   └─ Feature available in next release
```

---

## 📖 Useful Resources

- **[Architecture Overview](./01-architecture-overview.md)** - System design
- **[Project Structure](./02-project-structure.md)** - Directory layout
- **[Quick Start Guide](./03-quick-start-guide.md)** - Setup instructions
- **[Testing Strategies](./08-testing-strategies.md)** - Testing guide
- **[Microsoft C# Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)**

---

## 🆘 Getting Help

- **Questions?** Open a discussion
- **Stuck?** Comment on the issue
- **Need guidance?** DM a maintainer
- **Found a bug?** Report it
- **Have ideas?** Share suggestions

---

## 🎖️ Contributors

Thanks to all who contribute! Your effort makes ChatCore.NET better.

[See all contributors](https://github.com/CalebTek/ChatCore.NET/graphs/contributors)

---

## 📞 Contact

- **Issues:** [GitHub Issues](https://github.com/CalebTek/ChatCore.NET/issues)
- **Discussions:** [GitHub Discussions](https://github.com/CalebTek/ChatCore.NET/discussions)
- **Email:** [calebinfotek@gmail.com](mailto:calebinfotek@gmail.com)

---

**Happy Contributing! 🚀**

**Last Updated:** May 29, 2026
