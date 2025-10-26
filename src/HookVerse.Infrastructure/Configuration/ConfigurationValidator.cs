using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace HookVerse.Infrastructure.Configuration;

/// <summary>
/// Validates required configuration settings on application startup
/// </summary>
public class ConfigurationValidator
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly List<string> _errors = new();

    public ConfigurationValidator(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    /// <summary>
    /// Validates all required configuration settings
    /// Throws InvalidOperationException if validation fails
    /// </summary>
    public void Validate()
    {
        _errors.Clear();

        // Validate connection strings based on environment
        ValidateConnectionStrings();

        // Validate Key Vault configuration in production
        if (_environment.IsProduction() || _environment.IsStaging())
        {
            ValidateKeyVaultConfiguration();
        }

        // Validate OpenTelemetry configuration if OTLP endpoint is configured
        var otlpEndpoint = _configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            ValidateOpenTelemetryConfiguration();
        }

        // Throw if any errors were found
        if (_errors.Any())
        {
            var errorMessage = string.Join(Environment.NewLine, new[]
            {
                "Configuration validation failed:",
                string.Join(Environment.NewLine, _errors.Select(e => $"  - {e}"))
            });

            throw new InvalidOperationException(errorMessage);
        }
    }

    private void ValidateConnectionStrings()
    {
        if (_environment.IsDevelopment())
        {
            // Development: Validate container-based connection strings
            ValidateRequiredConnectionString("postgres", 
                "PostgreSQL connection string for Development environment");
            ValidateRequiredConnectionString("rabbitmq", 
                "RabbitMQ connection string for Development environment");
            ValidateRequiredConnectionString("redis", 
                "Redis connection string for Development environment");
        }
        else
        {
            // Production/Staging: Validate Azure managed service connection strings
            ValidateRequiredConnectionString("AzureSqlDatabase", 
                "Azure SQL Database connection string for Production/Staging environment",
                allowKeyVaultReference: true);
            ValidateRequiredConnectionString("AzureServiceBus", 
                "Azure Service Bus connection string for Production/Staging environment",
                allowKeyVaultReference: true);
            ValidateRequiredConnectionString("AzureRedisCache", 
                "Azure Redis Cache connection string for Production/Staging environment",
                allowKeyVaultReference: true);
        }
    }

    private void ValidateRequiredConnectionString(string name, string description, bool allowKeyVaultReference = false)
    {
        var connectionString = _configuration.GetConnectionString(name);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _errors.Add($"Missing required connection string: '{name}' ({description})");
            return;
        }

        // If it's a Key Vault reference, validate format
        if (connectionString.StartsWith("@Microsoft.KeyVault("))
        {
            if (!allowKeyVaultReference)
            {
                _errors.Add($"Connection string '{name}' cannot be a Key Vault reference in {_environment.EnvironmentName} environment");
            }
            else if (!connectionString.Contains("SecretUri="))
            {
                _errors.Add($"Invalid Key Vault reference format for '{name}'. Expected format: @Microsoft.KeyVault(SecretUri=https://...)");
            }
        }
    }

    private void ValidateKeyVaultConfiguration()
    {
        var keyVaultName = _configuration["Azure:KeyVault:Name"];
        var keyVaultUri = _configuration["Azure:KeyVault:VaultUri"];

        if (string.IsNullOrWhiteSpace(keyVaultName) && string.IsNullOrWhiteSpace(keyVaultUri))
        {
            _errors.Add("Azure Key Vault configuration missing in Production/Staging. Set 'Azure:KeyVault:Name' or 'Azure:KeyVault:VaultUri'");
            return;
        }

        // Validate VaultUri format if provided
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            if (!Uri.TryCreate(keyVaultUri, UriKind.Absolute, out var uri) || 
                !uri.Host.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase))
            {
                _errors.Add($"Invalid Key Vault URI: '{keyVaultUri}'. Expected format: https://<vault-name>.vault.azure.net/");
            }
        }
    }

    private void ValidateOpenTelemetryConfiguration()
    {
        var endpoint = _configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            _errors.Add($"Invalid OpenTelemetry OTLP endpoint: '{endpoint}'. Must be a valid URI (e.g., http://otel-collector:4317)");
            return;
        }

        // Validate scheme
        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            _errors.Add($"Invalid OpenTelemetry OTLP endpoint scheme: '{uri.Scheme}'. Must be http or https");
        }

        // Warn if using http in production
        if (uri.Scheme == "http" && _environment.IsProduction())
        {
            // Note: This is a warning, not an error, as some internal collectors may use http
            Console.WriteLine($"WARNING: Using insecure http for OTLP endpoint in Production: {endpoint}");
        }
    }

    /// <summary>
    /// Validates required setting exists
    /// </summary>
    private void ValidateRequiredSetting(string key, string description)
    {
        var value = _configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            _errors.Add($"Missing required configuration setting: '{key}' ({description})");
        }
    }

    /// <summary>
    /// Validates setting is a valid integer
    /// </summary>
    private void ValidateIntegerSetting(string key, string description, int? minValue = null, int? maxValue = null)
    {
        var value = _configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return; // Optional setting
        }

        if (!int.TryParse(value, out var intValue))
        {
            _errors.Add($"Invalid integer value for '{key}': '{value}' ({description})");
            return;
        }

        if (minValue.HasValue && intValue < minValue.Value)
        {
            _errors.Add($"Value for '{key}' ({intValue}) is below minimum ({minValue.Value})");
        }

        if (maxValue.HasValue && intValue > maxValue.Value)
        {
            _errors.Add($"Value for '{key}' ({intValue}) is above maximum ({maxValue.Value})");
        }
    }

    /// <summary>
    /// Validates setting is a valid boolean
    /// </summary>
    private void ValidateBooleanSetting(string key, string description)
    {
        var value = _configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return; // Optional setting
        }

        if (!bool.TryParse(value, out _))
        {
            _errors.Add($"Invalid boolean value for '{key}': '{value}' ({description}). Must be 'true' or 'false'");
        }
    }
}
