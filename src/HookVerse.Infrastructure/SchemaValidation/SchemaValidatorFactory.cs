using HookVerse.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HookVerse.Infrastructure.SchemaValidation;

/// <summary>
/// Factory for creating schema validators
/// </summary>
public class SchemaValidatorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _validators = new();

    public SchemaValidatorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        
        // Register known validators
        RegisterValidator("json-schema", typeof(JsonSchemaValidator));
        RegisterValidator("avro", typeof(AvroSchemaValidator));
        RegisterValidator("protobuf", typeof(ProtobufSchemaValidator));
    }

    /// <summary>
    /// Register a schema validator type
    /// </summary>
    public void RegisterValidator(string schemaType, Type validatorType)
    {
        if (!typeof(ISchemaValidator).IsAssignableFrom(validatorType))
        {
            throw new ArgumentException($"Type {validatorType.Name} must implement ISchemaValidator");
        }

        _validators[schemaType.ToLowerInvariant()] = validatorType;
    }

    /// <summary>
    /// Get validator for schema type
    /// </summary>
    public ISchemaValidator GetValidator(string schemaType)
    {
        var normalizedType = schemaType.ToLowerInvariant();

        if (!_validators.TryGetValue(normalizedType, out var validatorType))
        {
            throw new NotSupportedException($"Schema type '{schemaType}' is not supported");
        }

        return (ISchemaValidator)ActivatorUtilities.CreateInstance(_serviceProvider, validatorType);
    }

    /// <summary>
    /// Get all supported schema types
    /// </summary>
    public IEnumerable<string> GetSupportedSchemaTypes()
    {
        return _validators.Keys;
    }
}
