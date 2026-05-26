namespace ChatCore.Abstractions.Domain.Enums;

/// <summary>
/// Defines the delivery status of a message.
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// Message created but not yet delivered.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Message successfully persisted to storage.
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Message delivered to transport layer.
    /// </summary>
    Delivered = 2,

    /// <summary>
    /// Message has been read by recipient.
    /// </summary>
    Read = 3
}