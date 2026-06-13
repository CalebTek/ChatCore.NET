namespace ChatCore.Tests.Unit.Middleware;

using ChatCore.AspNetCore.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

public class ChatCoreExceptionMiddlewareTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static ChatCoreExceptionMiddleware BuildMiddleware(RequestDelegate next) =>
        new(next, NullLogger<ChatCoreExceptionMiddleware>.Instance);

    private static DefaultHttpContext BuildContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static async Task<(int statusCode, string body)> InvokeAndRead(
        RequestDelegate next)
    {
        var ctx        = BuildContext();
        var middleware = BuildMiddleware(next);
        await middleware.InvokeAsync(ctx);
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
        return (ctx.Response.StatusCode, body);
    }

    private static T ParseBody<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    // =========================================================================
    // Pass-through (no exception)
    // =========================================================================

    [Fact]
    public async Task InvokeAsync_NoException_CallsNext()
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var ctx        = BuildContext();
        var middleware = BuildMiddleware(next);
        await middleware.InvokeAsync(ctx);

        Assert.True(nextCalled);
        Assert.Equal(200, ctx.Response.StatusCode);
    }

    // =========================================================================
    // Exception → HTTP status mapping
    // =========================================================================

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns400()
    {
        RequestDelegate next = _ => throw new ArgumentException("bad arg");

        var (status, body) = await InvokeAndRead(next);

        Assert.Equal(400, status);
        var doc = ParseBody<JsonElement>(body);
        Assert.False(doc.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("INVALID_ARGUMENT", doc.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns403()
    {
        RequestDelegate next = _ => throw new UnauthorizedAccessException();

        var (status, body) = await InvokeAndRead(next);

        Assert.Equal(403, status);
        var doc = ParseBody<JsonElement>(body);
        Assert.Equal("FORBIDDEN", doc.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404()
    {
        RequestDelegate next = _ => throw new KeyNotFoundException("not found");

        var (status, body) = await InvokeAndRead(next);

        Assert.Equal(404, status);
        var doc = ParseBody<JsonElement>(body);
        Assert.Equal("NOT_FOUND", doc.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task InvokeAsync_OperationCanceledException_Returns503()
    {
        RequestDelegate next = _ => throw new OperationCanceledException();

        var (status, body) = await InvokeAndRead(next);

        Assert.Equal(503, status);
        var doc = ParseBody<JsonElement>(body);
        Assert.Equal("CANCELLED", doc.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task InvokeAsync_UnknownException_Returns500()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("something went wrong");

        var (status, body) = await InvokeAndRead(next);

        Assert.Equal(500, status);
        var doc = ParseBody<JsonElement>(body);
        Assert.Equal("INTERNAL_ERROR", doc.GetProperty("errorCode").GetString());
        Assert.False(doc.GetProperty("isSuccess").GetBoolean());
    }

    // =========================================================================
    // Response shape
    // =========================================================================

    [Fact]
    public async Task InvokeAsync_ResponseContentType_IsApplicationJson()
    {
        RequestDelegate next = _ => throw new Exception("oops");

        var ctx        = BuildContext();
        var middleware = BuildMiddleware(next);
        await middleware.InvokeAsync(ctx);

        Assert.Equal("application/json", ctx.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_ResponseBody_ContainsIsSuccessFalse()
    {
        RequestDelegate next = _ => throw new Exception("oops");

        var (_, body) = await InvokeAndRead(next);
        var doc       = ParseBody<JsonElement>(body);

        Assert.False(doc.GetProperty("isSuccess").GetBoolean());
    }

    [Fact]
    public async Task InvokeAsync_ResponseBody_ContainsErrorMessage()
    {
        RequestDelegate next = _ => throw new ArgumentException("specific message");

        var (_, body) = await InvokeAndRead(next);
        var doc       = ParseBody<JsonElement>(body);

        Assert.Equal("specific message", doc.GetProperty("error").GetString());
    }
}
