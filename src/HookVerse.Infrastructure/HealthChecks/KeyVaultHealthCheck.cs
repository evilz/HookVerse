using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace HookVerse.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Azure Key Vault connectivity in production environments
/// </summary>
public class KeyVaultHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly TokenCredential _credential;

    public KeyVaultHealthCheck(
        IConfiguration configuration, 
        IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
        
        // Use DefaultAzureCredential for production environments
        // This supports Managed Identity, Azure CLI, and other credential types
        _credential = new DefaultAzureCredential();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        // Skip Key Vault health check in Development environment
        if (_environment.IsDevelopment())
        {
            return HealthCheckResult.Healthy("Key Vault not required in Development environment");
        }

        try
        {
            // Get Key Vault configuration
            var vaultUri = _configuration["Azure:KeyVault:VaultUri"];
            
            if (string.IsNullOrWhiteSpace(vaultUri))
            {
                // Key Vault not configured - this may be intentional in some environments
                return HealthCheckResult.Degraded(
                    "Azure Key Vault not configured. Set 'Azure:KeyVault:VaultUri' in configuration.");
            }

            // Validate URI format
            if (!Uri.TryCreate(vaultUri, UriKind.Absolute, out var uri))
            {
                return HealthCheckResult.Unhealthy(
                    $"Invalid Key Vault URI: '{vaultUri}'. Expected format: https://<vault-name>.vault.azure.net/");
            }

            // Create SecretClient to test connectivity
            var client = new SecretClient(uri, _credential);

            // Test connectivity by attempting to list secrets (requires 'List' permission)
            // We don't actually enumerate - just check if we can connect
            var secretsEnumerator = client.GetPropertiesOfSecretsAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
            
            try
            {
                // Try to get the first item (or none if empty) - just to validate connection
                await secretsEnumerator.MoveNextAsync();
                
                return HealthCheckResult.Healthy(
                    $"Successfully connected to Azure Key Vault: {uri.Host}",
                    new Dictionary<string, object>
                    {
                        { "vault_uri", vaultUri },
                        { "environment", _environment.EnvironmentName }
                    });
            }
            finally
            {
                await secretsEnumerator.DisposeAsync();
            }
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            // Access denied - credentials are valid but permissions are insufficient
            return HealthCheckResult.Degraded(
                $"Key Vault authentication succeeded but access denied. Verify RBAC permissions (Key Vault Secrets User role required). Error: {ex.Message}",
                ex,
                new Dictionary<string, object>
                {
                    { "error_code", ex.ErrorCode ?? "AccessDenied" },
                    { "status_code", ex.Status }
                });
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 401)
        {
            // Authentication failed
            return HealthCheckResult.Unhealthy(
                $"Key Vault authentication failed. Verify Managed Identity or credentials are configured. Error: {ex.Message}",
                ex,
                new Dictionary<string, object>
                {
                    { "error_code", ex.ErrorCode ?? "Unauthorized" },
                    { "status_code", ex.Status }
                });
        }
        catch (Azure.RequestFailedException ex)
        {
            // Other Azure-specific errors
            return HealthCheckResult.Unhealthy(
                $"Key Vault request failed: {ex.Message}",
                ex,
                new Dictionary<string, object>
                {
                    { "error_code", ex.ErrorCode ?? "Unknown" },
                    { "status_code", ex.Status }
                });
        }
        catch (CredentialUnavailableException ex)
        {
            // No credentials available (e.g., Managed Identity not configured)
            return HealthCheckResult.Unhealthy(
                $"Azure credentials unavailable. In production, ensure Managed Identity is configured. Error: {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            // General error
            return HealthCheckResult.Unhealthy(
                $"Key Vault health check failed: {ex.Message}",
                ex);
        }
    }
}
