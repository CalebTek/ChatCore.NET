namespace ChatCore.Abstractions.Results;

/// <summary>
/// Represents a result from a chat operation.
/// </summary>
/// <typeparam name="T">The result data type.</typeparam>
public class ChatResult<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Gets the result data.
    /// </summary>
    public T? Data { get; private set; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Gets the error code if the operation failed.
    /// </summary>
    public string? ErrorCode { get; private set; }

    private ChatResult()
    {
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="data">The result data.</param>
    /// <returns>A successful <see cref="ChatResult{T}"/>.</returns>
    public static ChatResult<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    /// <returns>A failed <see cref="ChatResult{T}"/>.</returns>
    public static ChatResult<T> Failure(string error, string? errorCode = null) => new()
    {
        IsSuccess = false,
        Error = error,
        ErrorCode = errorCode
    };
}