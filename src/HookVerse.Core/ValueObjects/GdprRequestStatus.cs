namespace HookVerse.Core.ValueObjects;

/// <summary>
/// Status of a GDPR request.
/// </summary>
public enum GdprRequestStatus
{
    /// <summary>
    /// Request has been submitted but not yet processed
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Request is currently being processed
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Request has been completed successfully
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Request processing failed
    /// </summary>
    Failed = 4
}
