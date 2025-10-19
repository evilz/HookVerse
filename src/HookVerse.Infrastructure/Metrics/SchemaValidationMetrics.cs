using System.Diagnostics.Metrics;
using HookVerse.Core.Enums;

namespace HookVerse.Infrastructure.Metrics;

/// <summary>
/// Metrics for schema validation operations
/// </summary>
public class SchemaValidationMetrics
{
    private readonly Counter<long> _validationSuccessCounter;
    private readonly Counter<long> _validationFailureCounter;
    private readonly Histogram<double> _validationDurationHistogram;
    private readonly Counter<long> _schemaCreatedCounter;
    private readonly Counter<long> _schemaUpdatedCounter;

    public SchemaValidationMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("HookVerse.SchemaValidation");

        _validationSuccessCounter = meter.CreateCounter<long>(
            "schema.validation.success",
            description: "Number of successful schema validations");

        _validationFailureCounter = meter.CreateCounter<long>(
            "schema.validation.failure",
            description: "Number of failed schema validations");

        _validationDurationHistogram = meter.CreateHistogram<double>(
            "schema.validation.duration",
            unit: "ms",
            description: "Duration of schema validation operations in milliseconds");

        _schemaCreatedCounter = meter.CreateCounter<long>(
            "schema.created",
            description: "Number of schemas created");

        _schemaUpdatedCounter = meter.CreateCounter<long>(
            "schema.updated",
            description: "Number of schemas updated");
    }

    public void RecordValidationSuccess(Guid eventTypeId, SchemaFormat format)
    {
        _validationSuccessCounter.Add(1,
            new KeyValuePair<string, object?>("event_type_id", eventTypeId.ToString()),
            new KeyValuePair<string, object?>("schema_format", format.ToString()));
    }

    public void RecordValidationFailure(Guid eventTypeId, SchemaFormat format, string errorType)
    {
        _validationFailureCounter.Add(1,
            new KeyValuePair<string, object?>("event_type_id", eventTypeId.ToString()),
            new KeyValuePair<string, object?>("schema_format", format.ToString()),
            new KeyValuePair<string, object?>("error_type", errorType));
    }

    public void RecordValidationDuration(double durationMs, SchemaFormat format, bool success)
    {
        _validationDurationHistogram.Record(durationMs,
            new KeyValuePair<string, object?>("schema_format", format.ToString()),
            new KeyValuePair<string, object?>("success", success.ToString()));
    }

    public void RecordSchemaCreated(Guid eventTypeId, SchemaFormat format)
    {
        _schemaCreatedCounter.Add(1,
            new KeyValuePair<string, object?>("event_type_id", eventTypeId.ToString()),
            new KeyValuePair<string, object?>("schema_format", format.ToString()));
    }

    public void RecordSchemaUpdated(Guid eventTypeId, SchemaFormat format)
    {
        _schemaUpdatedCounter.Add(1,
            new KeyValuePair<string, object?>("event_type_id", eventTypeId.ToString()),
            new KeyValuePair<string, object?>("schema_format", format.ToString()));
    }
}
