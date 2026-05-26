namespace ChatCore.Core.Services;

using ChatCore.Abstractions.Services;

/// <summary>
/// Default implementation of <see cref="IClock"/>.
/// </summary>
public class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}