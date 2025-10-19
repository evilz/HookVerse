using Microsoft.EntityFrameworkCore;
using HookVerse.Core.Interfaces;
using HookVerse.Core.Services;
using HookVerse.Infrastructure.Data;
using HookVerse.Infrastructure.Repositories;
using HookVerse.Infrastructure.Services;
using HookVerse.Infrastructure.SchemaValidation;
using HookVerse.Infrastructure.Metrics;
using FluentValidation;
using System.Diagnostics.Metrics;
using MassTransit;

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
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        
        // Register HttpClient for DeliveryService
        services.AddHttpClient<IDeliveryService, DeliveryService>();

        // Register schema validators
        services.AddScoped<ISchemaValidator, JsonSchemaValidator>();
        services.AddScoped<SchemaValidatorFactory>();

        // Register FluentValidation
        services.AddValidatorsFromAssemblyContaining<Program>();

        // Register metrics
        services.AddSingleton<SubscriptionMetrics>(sp =>
        {
            var meterFactory = sp.GetRequiredService<IMeterFactory>();
            var dbContext = sp.CreateScope().ServiceProvider.GetRequiredService<HookVerseDbContext>();
            return new SubscriptionMetrics(meterFactory, () => dbContext.Subscriptions.Count(s => s.IsActive));
        });

        services.AddSingleton<SchemaValidationMetrics>(sp =>
        {
            var meterFactory = sp.GetRequiredService<IMeterFactory>();
            return new SchemaValidationMetrics(meterFactory);
        });

        return services;
    }

    /// <summary>
    /// Add MassTransit message bus for webhook events
    /// </summary>
    public static IServiceCollection AddMessageBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(busConfig =>
        {
            var transport = configuration["MessageBus:Transport"] ?? "RabbitMQ";

            if (transport.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase))
            {
                busConfig.UsingRabbitMq((context, cfg) =>
                {
                    var host = configuration["MessageBus:RabbitMQ:Host"] ?? "localhost";
                    var portStr = configuration["MessageBus:RabbitMQ:Port"];
                    var port = int.TryParse(portStr, out var p) ? p : 5672;
                    var username = configuration["MessageBus:RabbitMQ:Username"] ?? "guest";
                    var password = configuration["MessageBus:RabbitMQ:Password"] ?? "guest";

                    cfg.Host(host, (ushort)port, "/", h =>
                    {
                        h.Username(username);
                        h.Password(password);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        return services;
    }
}
