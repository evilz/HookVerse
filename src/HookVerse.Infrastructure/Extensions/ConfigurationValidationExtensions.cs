using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HookVerse.Infrastructure.Configuration;

/// <summary>
/// Extension methods for configuration validation
/// </summary>
public static class ConfigurationValidationExtensions
{
    /// <summary>
    /// Adds and runs configuration validation on application startup
    /// </summary>
    /// <param name="host">The host builder</param>
    /// <returns>The host builder for chaining</returns>
    public static IHost ValidateConfiguration(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        var validator = new ConfigurationValidator(configuration, environment);
        
        try
        {
            validator.Validate();
            Console.WriteLine($"✓ Configuration validation passed for {environment.EnvironmentName} environment");
        }
        catch (InvalidOperationException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Configuration validation failed:");
            Console.WriteLine(ex.Message);
            Console.ResetColor();
            
            // Fail fast - exit the application
            throw;
        }

        return host;
    }

    /// <summary>
    /// Adds configuration validation to the service collection
    /// This is useful for testing or manual validation
    /// </summary>
    public static IServiceCollection AddConfigurationValidation(this IServiceCollection services)
    {
        services.AddSingleton<ConfigurationValidator>();
        return services;
    }
}
