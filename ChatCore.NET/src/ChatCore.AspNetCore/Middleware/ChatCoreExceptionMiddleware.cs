namespace ChatCore.AspNetCore.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

/// <summary>
/// Global exception handling middleware for ChatCore.NET.
/// Catches unhandled exceptions from any downstream middleware or controller,
/// logs them, and returns a consistent JSON error response so clients always
/// receive a structured payload rather than an HTML error page or empty 500.
/// </summary>
/// <remarks>
/// Register this as the outermost middleware so it wraps the entire pipeline:
/// <code>
/// app.UseChatCoreExceptionHandler();  // first
/// app.UseRouting();
/// app.MapControllers();
/// app.MapChatHub("/hubs/chat");
/// </code>
/// </remarks>
public class ChatCoreExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ChatCoreExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ChatCoreExceptionMiddleware(RequestDelegate next, ILogger<ChatCoreExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteErrorResponseAsync(context, ex);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
    {
        // Don't overwrite a response that has already started streaming
        if (context.Response.HasStarted)
            return;

        var (statusCode, errorCode, message) = ex switch
        {
            ArgumentException        => (HttpStatusCode.BadRequest,            "INVALID_ARGUMENT",  ex.Message),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden,          "FORBIDDEN",         "Access denied."),
            KeyNotFoundException     => (HttpStatusCode.NotFound,              "NOT_FOUND",         ex.Message),
            OperationCanceledException => (HttpStatusCode.ServiceUnavailable,  "CANCELLED",         "The request was cancelled."),
            _                        => (HttpStatusCode.InternalServerError,   "INTERNAL_ERROR",    "An unexpected error occurred.")
        };

        context.Response.Clear();
        context.Response.StatusCode  = (int)statusCode;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new
        {
            isSuccess = false,
            error     = message,
            errorCode
        }, JsonOptions);

        await context.Response.WriteAsync(body);
    }
}
