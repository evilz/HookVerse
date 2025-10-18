using HookVerse.Core.Interfaces;
using HookVerse.Infrastructure.Data;
using HookVerse.Infrastructure.MessageBus;
using HookVerse.Infrastructure.Repositories;
using HookVerse.Infrastructure.SchemaValidation;
using HookVerse.Infrastructure.Services;
using HookVerse.Worker.Consumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/worker-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
    .CreateLogger();

try
{
    Log.Information("Starting HookVerse Worker");

    var builder = Host.CreateApplicationBuilder(args);

    // Add Serilog
    builder.Services.AddSerilog(Log.Logger);

    // Add Database
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection not configured");

    builder.Services.AddDbContext<HookVerseDbContext>(options =>
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });
    });

    // Register repositories
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped<ISubscriberRepository, SubscriberRepository>();
    builder.Services.AddScoped<IEventTypeRepository, EventTypeRepository>();
    builder.Services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
    builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
    builder.Services.AddScoped<IDeliveryAttemptRepository, DeliveryAttemptRepository>();

    // Register business services
    builder.Services.AddScoped<IWebhookService, WebhookService>();
    builder.Services.AddScoped<ISignatureService, HmacSignatureService>();
    builder.Services.AddScoped<IDeliveryService, DeliveryService>();
    builder.Services.AddHttpClient<IDeliveryService, DeliveryService>();

    // Register schema validators
    builder.Services.AddScoped<ISchemaValidator, JsonSchemaValidator>();
    builder.Services.AddScoped<SchemaValidatorFactory>();

    // Add Message Bus
    builder.Services.AddMessageBus(builder.Configuration);

    // Register MassTransit consumer
    builder.Services.AddMassTransit(busConfig =>
    {
        busConfig.AddConsumer<WebhookDeliveryConsumer>();
        busConfig.AddConsumer<MessageBusWebhookConsumer>();

        var transport = builder.Configuration["MessageBus:Transport"] ?? "RabbitMQ";

        if (transport.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase))
        {
            busConfig.UsingRabbitMq((context, cfg) =>
            {
                var host = builder.Configuration["MessageBus:RabbitMQ:Host"] ?? "localhost";
                var portStr = builder.Configuration["MessageBus:RabbitMQ:Port"];
                var port = int.TryParse(portStr, out var p) ? p : 5672;
                var username = builder.Configuration["MessageBus:RabbitMQ:Username"] ?? "guest";
                var password = builder.Configuration["MessageBus:RabbitMQ:Password"] ?? "guest";

                cfg.Host(host, (ushort)port, "/", h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                // Configure external webhooks queue with DLQ support
                cfg.ReceiveEndpoint(builder.Configuration["MessageBus:ExternalWebhooks:QueueName"] ?? "external-webhooks", e =>
                {
                    // Set queue durability
                    e.Durable = builder.Configuration.GetValue<bool>("MessageBus:ExternalWebhooks:Durable");
                    e.AutoDelete = builder.Configuration.GetValue<bool>("MessageBus:ExternalWebhooks:AutoDelete");

                    // Set prefetch count for concurrent message processing
                    var prefetchCount = builder.Configuration.GetValue<int>("MessageBus:ExternalWebhooks:PrefetchCount");
                    if (prefetchCount > 0)
                    {
                        e.PrefetchCount = prefetchCount;
                    }

                    // Configure retry policy with exponential backoff
                    e.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(2)));

                    e.ConfigureConsumer<MessageBusWebhookConsumer>(context);
                });

                cfg.ConfigureEndpoints(context);
            });
        }
    });

    var host = builder.Build();
    await host.RunAsync();

    Log.Information("HookVerse Worker stopped gracefully");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "HookVerse Worker terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
