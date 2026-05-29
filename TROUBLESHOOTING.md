# Troubleshooting Guide

## 🆘 Common Issues and Solutions

This guide covers common problems and their solutions for ChatCore.NET.

---

## 🔧 Development Environment

### Issue: Database Connection Failed

**Error:**
```
SqlException: A network-related or instance-specific error occurred while establishing a connection to SQL Server.
```

**Solutions:**

1. **Verify SQL Server is Running**
   ```bash
   # Windows
   Get-Service MSSQLSERVER | Select-Object Status
   
   # Or start if stopped
   Start-Service MSSQLSERVER
   ```

2. **Check Connection String**
   ```json
   // appsettings.Development.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ChatCoreDb;Trusted_Connection=true;"
     }
   }
   ```

3. **Use LocalDB Correctly**
   ```bash
   # List LocalDB instances
   sqllocaldb info
   
   # Create instance if needed
   sqllocaldb create MSSQLLocalDB
   
   # Start instance
   sqllocaldb start MSSQLLocalDB
   ```

4. **Firewall Issues**
   - Check Windows Firewall allows SQL Server
   - Verify network connectivity
   - Try connecting with SSMS first

---

### Issue: Migrations Not Working

**Error:**
```
The entity type 'X' requires a primary key to be defined.
```

**Solutions:**

1. **Ensure Entity Has Primary Key**
   ```csharp
   public class User
   {
       public Guid Id { get; set; }  // Primary key required
       public string Email { get; set; }
   }
   ```

2. **Configure in DbContext**
   ```csharp
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       modelBuilder.Entity<User>()
           .HasKey(u => u.Id);
   }
   ```

3. **Clean and Retry Migrations**
   ```bash
   # Remove last migration
   dotnet ef migrations remove
   
   # Re-create
   dotnet ef migrations add InitialCreate
   
   # Apply
   dotnet ef database update
   ```

---

### Issue: Port Already in Use

**Error:**
```
System.Net.Sockets.SocketException: Only one usage of each socket address is normally permitted
```

**Solutions:**

```bash
# Windows - Find process using port 5000
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Linux/Mac - Find process
lsof -i :5000
kill -9 <PID>

# Or use different port in launchSettings.json
{
  "profiles": {
    "ChatCore.API": {
      "applicationUrl": "https://localhost:5002;http://localhost:5001"
    }
  }
}
```

---

### Issue: NuGet Package Not Found

**Error:**
```
error: Unable to find package 'PackageName'
```

**Solutions:**

```bash
# Clear NuGet cache
nuget locals all -clear

# Restore with verbose output
dotnet restore --verbosity diagnostic

# Update NuGet
nuget update -self

# Check package exists
dotnet package search EntityFrameworkCore
```

---

## 🔐 Authentication Issues

### Issue: JWT Token Invalid

**Error:**
```
Authorization header must contain a Bearer token
```

**Solutions:**

1. **Verify Token Format**
   ```
   Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```

2. **Check Token Expiration**
   ```csharp
   var tokenHandler = new JwtSecurityTokenHandler();
   var token = tokenHandler.ReadJwtToken(bearerToken);
   var isExpired = token.ValidTo < DateTime.UtcNow;
   ```

3. **Verify Signing Key**
   - Ensure same secret key used for signing and verification
   - Check `appsettings.json` has correct key

4. **Clear and Re-Login**
   ```bash
   # Remove stored token
   localStorage.removeItem('token');
   
   # Re-login to get fresh token
   ```

---

### Issue: Access Denied - Not Authorized

**Error:**
```
403 Forbidden - User does not have required permission
```

**Solutions:**

1. **Check User Role**
   ```csharp
   var user = await _userService.GetUserAsync(userId);
   var participant = await _participantService.GetUserInConversation(convId, userId);
   // Verify participant role is sufficient
   ```

2. **Verify [Authorize] Attributes**
   ```csharp
   [Authorize(Roles = "Admin,Owner")]  // Correct roles?
   public IActionResult DeleteConversation(Guid id)
   ```

3. **Check CORS Configuration**
   ```csharp
   services.AddCors(options =>
   {
       options.AddPolicy("Default", builder =>
           builder.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader());
   });
   ```

---

## 💬 SignalR Issues

### Issue: SignalR Connection Fails

**Error:**
```
WebSocket is closed with status code: 1000
Connection ID is required
```

**Solutions:**

1. **Enable WebSocket in IIS (if on Windows)**
   - Server Manager → Roles → Web Server (IIS) → Role Services
   - Add "WebSocket Protocol"
   - Restart IIS

2. **Check Hub URL**
   ```typescript
   const connection = new signalR.HubConnectionBuilder()
       .withUrl("/chat-hub")  // Correct endpoint?
       .build();
   ```

3. **Verify Token in SignalR**
   ```typescript
   const connection = new signalR.HubConnectionBuilder()
       .withUrl("/chat-hub", {
           accessTokenFactory: () => localStorage.getItem("token")
       })
       .build();
   ```

4. **Check CORS for WebSocket**
   ```csharp
   services.AddCors(options =>
   {
       options.AddPolicy("ChatPolicy", builder =>
           builder.WithOrigins("https://client.example.com")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials());  // Required for WebSocket
   });
   ```

---

### Issue: Messages Not Received

**Error:**
```
Sent message but didn't receive response
```

**Solutions:**

1. **Check Connection Status**
   ```typescript
   console.log("Connection state:", connection.state);
   // 0 = Connecting, 1 = Connected, 2 = Reconnecting, 3 = Disconnected
   ```

2. **Verify User in Conversation**
   ```typescript
   await connection.invoke("JoinConversation", conversationId)
       .catch(err => console.error("Join failed:", err));
   ```

3. **Check Browser Console**
   - Look for error messages
   - Check network tab in DevTools
   - Verify SignalR messages in network traffic

4. **Restart Connection**
   ```typescript
   await connection.stop();
   await connection.start();
   ```

---

## 🗄️ Database Issues

### Issue: Migration Creates Wrong Schema

**Error:**
```
Column 'X' cannot be null
```

**Solutions:**

1. **Review Migration File**
   ```csharp
   // Check generated migration
   public partial class AddNewColumn : Migration
   {
       protected override void Up(MigrationBuilder migrationBuilder)
       {
           migrationBuilder.AddColumn<string>(
               name: "NewColumn",
               table: "Users",
               type: "nvarchar(max)",
               nullable: false);  // Should be nullable for existing data
       }
   }
   ```

2. **Fix Migration**
   ```bash
   # Remove migration
   dotnet ef migrations remove
   
   # Re-create with correct defaults
   dotnet ef migrations add FixColumnDefinition
   ```

3. **Rollback Database**
   ```bash
   # Revert to previous migration
   dotnet ef database update PreviousMigrationName
   ```

---

### Issue: Query Performance Slow

**Error:**
```
Query timeout after 30 seconds
```

**Solutions:**

1. **Add Indexes**
   ```csharp
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       modelBuilder.Entity<Message>()
           .HasIndex(m => new { m.ConversationId, m.CreatedAt });
   }
   ```

2. **Optimize Queries**
   ```csharp
   // Bad - N+1 query problem
   var messages = await _context.Messages.ToListAsync();
   foreach (var msg in messages)
       Console.WriteLine(msg.Sender.Name);  // Extra query per message
   
   // Good - Eager loading
   var messages = await _context.Messages
       .Include(m => m.Sender)
       .ToListAsync();
   ```

3. **Use AsNoTracking**
   ```csharp
   var messages = await _context.Messages
       .AsNoTracking()
       .ToListAsync();
   ```

---

## 🧪 Testing Issues

### Issue: Tests Fail Locally But Pass in CI

**Solutions:**

1. **Ensure Clean Test Data**
   ```csharp
   [Fact]
   public async Task MyTest()
   {
       // Use new in-memory context
       var options = new DbContextOptionsBuilder<ChatCoreDbContext>()
           .UseInMemoryDatabase(Guid.NewGuid().ToString())
           .Options;
   }
   ```

2. **Check Test Isolation**
   ```csharp
   public class TestFixture : IAsyncLifetime
   {
       public async Task InitializeAsync()
       {
           // Setup per test
       }
       
       public async Task DisposeAsync()
       {
           // Cleanup per test
       }
   }
   ```

3. **Verify Mock Setup**
   ```csharp
   _mockRepository
       .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
       .ReturnsAsync((User)null);
   ```

---

### Issue: Async Test Hangs

**Error:**
```
Test timed out
```

**Solutions:**

```csharp
// Add timeout
[Fact(Timeout = 5000)]  // 5 seconds
public async Task MyAsyncTest()
{
    // Should complete within timeout
}

// Ensure proper async/await usage
public async Task ProperlyAsyncTest()
{
    var result = await _service.GetDataAsync();  // Proper await
    Assert.NotNull(result);
}
```

---

## 🚀 Deployment Issues

### Issue: Application Won't Start After Deployment

**Error:**
```
The type initializer for 'X' threw an exception
```

**Solutions:**

1. **Check Configuration**
   - Verify appsettings.Production.json exists
   - Check environment variables are set
   - Verify database is accessible

2. **Review Logs**
   ```bash
   # Check application logs
   tail -f /var/log/chatcore/app.log
   
   # Check system logs
   journalctl -u chatcore -n 50
   ```

3. **Test Database Connection**
   ```csharp
   using (var context = new ChatCoreDbContext(options))
   {
       context.Database.CanConnect();  // Returns bool
   }
   ```

---

### Issue: Out of Memory

**Error:**
```
OutOfMemoryException
```

**Solutions:**

1. **Increase Memory Limit**
   ```json
   // Docker
   {
     "memory": "512m",
     "memory-swap": "512m"
   }
   ```

2. **Monitor and Optimize**
   - Profile memory usage
   - Identify memory leaks
   - Optimize data loading (pagination)

3. **Configure Garbage Collection**
   ```xml
   <!-- .csproj -->
   <PropertyGroup>
     <TieredCompilation>true</TieredCompilation>
     <RetainVMGarbageCollectingOnAppExit>true</RetainVMGarbageCollectingOnAppExit>
   </PropertyGroup>
   ```

---

## 📊 Performance Issues

### Issue: API Response Slow

**Solutions:**

1. **Profile Application**
   - Use Application Insights
   - Add timing logs
   - Monitor database queries

2. **Optimize Database**
   - Add missing indexes
   - Pagination instead of fetching all data
   - Connection pooling

3. **Implement Caching**
   ```csharp
   services.AddStackExchangeRedisCache(options =>
   {
       options.Configuration = configuration.GetConnectionString("Redis");
   });
   ```

---

## 📞 Getting Help

If you can't find a solution:

1. **Check Documentation:** [docs/README.md](./docs/README.md)
2. **Search Issues:** [GitHub Issues](https://github.com/CalebTek/ChatCore.NET/issues)
3. **Ask Questions:** [GitHub Discussions](https://github.com/CalebTek/ChatCore.NET/discussions)
4. **Report Bug:** [Create New Issue](https://github.com/CalebTek/ChatCore.NET/issues/new)
5. **Contact:** calebinfotek@gmail.com

---

## 🐛 Debugging Tips

### Enable Verbose Logging

```csharp
// Program.cs
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Logging.AddConsole();
```

### Check Connection Strings

```bash
# Don't log full connection string in production!
var (server, database) = ExtractConnectionDetails(connectionString);
_logger.LogInformation($"Connected to {server}/{database}");
```

### Use Debugger

```csharp
// Set breakpoint
if (someCondition)
{
    System.Diagnostics.Debugger.Break();  // Breaks in debugger
}
```

---

**Last Updated:** May 29, 2026

**Still stuck?** Open an issue on [GitHub](https://github.com/CalebTek/ChatCore.NET/issues) with:
- Error message
- Steps to reproduce
- Expected behavior
- Actual behavior
- Environment info
