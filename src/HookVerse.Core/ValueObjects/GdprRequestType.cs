namespace HookVerse.Core.ValueObjects;

/// <summary>
/// Types of GDPR requests.
/// </summary>
public enum GdprRequestType
{
    /// <summary>
    /// Data export request (Right to Access)
    /// </summary>
    Export = 1,

    /// <summary>
    /// Data deletion request (Right to Erasure)
    /// </summary>
    Delete = 2
}
