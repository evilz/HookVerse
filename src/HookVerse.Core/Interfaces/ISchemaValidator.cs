namespace HookVerse.Core.Interfaces;

/// <summary>
/// Schema validation result
/// </summary>
public record SchemaValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();

    public static SchemaValidationResult Success() =>
        new() { IsValid = true };

    public static SchemaValidationResult Failure(params string[] errors) =>
        new() { IsValid = false, Errors = errors.ToList() };
}

/// <summary>
/// Interface for schema validation
/// </summary>
public interface ISchemaValidator
{
    /// <summary>
    /// Validate payload against schema
    /// </summary>
    Task<SchemaValidationResult> ValidateAsync(string payload, string schema, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get schema type supported by this validator
    /// </summary>
    string SchemaType { get; }
}
