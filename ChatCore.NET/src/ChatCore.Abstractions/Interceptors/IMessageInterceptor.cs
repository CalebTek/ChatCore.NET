namespace ChatCore.Abstractions.Interceptors;

/// <summary>
/// Defines an interceptor for message operations.
/// </summary>
public interface IMessageInterceptor
{
    /// <summary>
    /// Executes before a message is sent.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnBeforeSendAsync(MessageContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes after a message is sent.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnAfterSendAsync(MessageContext context, CancellationToken cancellationToken = default);
}