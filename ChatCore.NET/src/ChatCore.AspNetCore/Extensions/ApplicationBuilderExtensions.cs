namespace ChatCore.AspNetCore.Extensions;

using ChatCore.AspNetCore.Middleware;
using Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> to register ChatCore middleware.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the ChatCore global exception handler to the middleware pipeline.
    /// Register this before all other middleware so exceptions from any layer are caught.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder, for chaining.</returns>
    public static IApplicationBuilder UseChatCoreExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ChatCoreExceptionMiddleware>();
    }
}
