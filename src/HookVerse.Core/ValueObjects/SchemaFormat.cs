namespace HookVerse.Core.ValueObjects;

/// <summary>
/// Schema format types supported for webhook payload validation
/// </summary>
public enum SchemaFormat
{
    /// <summary>
    /// JSON Schema validation (https://json-schema.org/)
    /// </summary>
    JsonSchema = 1,

    /// <summary>
    /// Apache Avro schema (https://avro.apache.org/)
    /// </summary>
    Avro = 2,

    /// <summary>
    /// Protocol Buffers schema (https://developers.google.com/protocol-buffers)
    /// </summary>
    Protobuf = 3,

    /// <summary>
    /// .NET assembly type validation using reflection
    /// </summary>
    DotNetAssembly = 4
}
