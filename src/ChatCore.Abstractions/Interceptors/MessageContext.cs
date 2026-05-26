namespace ChatCore.Abstractions.Interceptors;

using ChatCore.Abstractions.Domain.Entities;

/// <summary>
/// Context passed through the interceptor pipeline.
/// </summary>
public class MessageContext
{
    /// <summary>
    /// Gets or sets the chat message.
    /// </summary>
    public ChatMessage Message { get; set; } = null!;

    /// <summary>
    /// Gets or sets a dictionary of contextual data.
    /// </summary>
    public Dictionary<string, object?> Properties { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the operation should be cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Gets or sets the cancellation reason if the operation is cancelled.
    /// </summary>
    public string? CancellationReason { get; set; }
}