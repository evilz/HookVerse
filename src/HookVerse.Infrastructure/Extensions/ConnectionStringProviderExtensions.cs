using HookVerse.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HookVerse.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering connection string providers
/// </summary>
public static class ConnectionStringProviderExtensions
{
    /// <summary>
    /// Registers the appropriate connection string provider based on the current environment.
    /// - Development: Uses LocalConnectionStringProvider (Aspire-injected container connections)
    /// - Staging/Production: Uses AzureConnectionStringProvider (Azure managed services)
    /// </summary>
    public static IServiceCollection AddConnectionStringProvider(
        this IServiceCollection services, 
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (environment.IsProduction() || environment.IsStaging())
        {
            // Production/Staging: Use Azure managed services
            services.AddSingleton<IConnectionStringProvider>(sp => 
                new AzureConnectionStringProvider(configuration));
        }
        else
        {
            // Development: Use local containers (Aspire-orchestrated)
            services.AddSingleton<IConnectionStringProvider>(sp => 
                new LocalConnectionStringProvider(configuration));
        }

        return services;
    }
}
