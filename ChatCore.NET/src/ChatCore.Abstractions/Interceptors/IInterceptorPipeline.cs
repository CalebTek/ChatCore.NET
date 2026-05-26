namespace ChatCore.Abstractions.Interceptors;

/// <summary>
/// Defines a pipeline for executing message interceptors.
/// </summary>
public interface IInterceptorPipeline
{
    /// <summary>
    /// Executes all before-send interceptors.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteBeforeAsync(MessageContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all after-send interceptors.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAfterAsync(MessageContext context, CancellationToken cancellationToken = default);
}