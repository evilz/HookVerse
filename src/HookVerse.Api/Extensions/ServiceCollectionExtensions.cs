using Microsoft.EntityFrameworkCore;
using HookVerse.Core.Interfaces;
using HookVerse.Infrastructure.Data;
using HookVerse.Infrastructure.Repositories;
using HookVerse.Infrastructure.Services;
using HookVerse.Infrastructure.SchemaValidation;
using FluentValidation;

namespace HookVerse.Api.Extensions;

/// <summary>
/// Extension methods for configuring services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add database context and repositories
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured");

        services.AddDbContext<HookVerseDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });
        });

        // Register generic repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Register specific repositories
        services.AddScoped<ISubscriberRepository, SubscriberRepository>();
        services.AddScoped<IEventTypeRepository, EventTypeRepository>();
        services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IDeliveryAttemptRepository, DeliveryAttemptRepository>();

        return services;
    }

    /// <summary>
    /// Add authentication services
    /// </summary>
    public static IServiceCollection AddApiKeyAuthentication(this IServiceCollection services)
    {
        services.AddScoped<IApiKeyService, ApiKeyService>();
        return services;
    }

    /// <summary>
    /// Add business services
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        // Register core business services
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<ISignatureService, HmacSignatureService>();
        services.AddScoped<IDeliveryService, DeliveryService>();
        
        // Register HttpClient for DeliveryService
        services.AddHttpClient<IDeliveryService, DeliveryService>();

        // Register schema validators
        services.AddScoped<ISchemaValidator, JsonSchemaValidator>();
        services.AddScoped<SchemaValidatorFactory>();

        // Register FluentValidation
        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }
}
