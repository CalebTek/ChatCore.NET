namespace ChatCore.Abstractions.Services;

/// <summary>
/// Defines a clock abstraction for testability.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }
}