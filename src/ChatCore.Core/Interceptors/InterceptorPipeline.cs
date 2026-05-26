namespace ChatCore.Core.Interceptors;

using ChatCore.Abstractions.Interceptors;

/// <summary>
/// Implementation of <see cref="IInterceptorPipeline"/>.
/// </summary>
public class InterceptorPipeline : IInterceptorPipeline
{
    private readonly IEnumerable<IMessageInterceptor> _interceptors;

    /// <summary>
    /// Initializes a new instance of the <see cref="InterceptorPipeline"/> class.
    /// </summary>
    /// <param name="interceptors">The interceptors to execute in order.</param>
    public InterceptorPipeline(IEnumerable<IMessageInterceptor> interceptors)
    {
        _interceptors = interceptors;
    }

    /// <inheritdoc />
    public async Task ExecuteBeforeAsync(MessageContext context, CancellationToken cancellationToken = default)
    {
        foreach (var interceptor in _interceptors)
        {
            if (context.IsCancelled)
            {
                break;
            }

            await interceptor.OnBeforeSendAsync(context, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task ExecuteAfterAsync(MessageContext context, CancellationToken cancellationToken = default)
    {
        foreach (var interceptor in _interceptors)
        {
            if (context.IsCancelled)
            {
                break;
            }

            await interceptor.OnAfterSendAsync(context, cancellationToken);
        }
    }
}