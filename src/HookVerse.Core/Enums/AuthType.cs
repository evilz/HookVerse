namespace HookVerse.Core.Enums;

/// <summary>
/// Authentication type for subscription endpoints.
/// </summary>
public enum AuthType
{
    None = 0,
    CustomHeaders = 1,
    BasicAuth = 2,
    BearerToken = 3
}
