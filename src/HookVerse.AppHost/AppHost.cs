var builder = DistributedApplication.CreateBuilder(args);

// Check if publishing to Azure (detected by presence of Azure-specific environment variables or publish profile)
var isAzurePublish = builder.Configuration["ASPIRE_ENVIRONMENT"] == "azure" 
    || builder.ExecutionContext.IsPublishMode;

// Infrastructure Resources - Conditional based on environment

IResourceBuilder<IResourceWithConnectionString> webhookDb;
IResourceBuilder<IResourceWithConnectionString> rabbitmq;
IResourceBuilder<IResourceWithConnectionString> redis;

if (isAzurePublish)
{
    // Azure Managed Resources for Production
    
    // Azure SQL Database
    var sql = builder.AddAzureSqlServer("sql")
        .AddDatabase("hookverse-db");
    webhookDb = sql;
    
    // Azure Service Bus
    rabbitmq = builder.AddAzureServiceBus("servicebus");
    
    // Azure Redis Cache
    redis = builder.AddAzureRedis("redis-cache");
}
else
{
    // Container Resources for Local Development
    
    // PostgreSQL database with PgAdmin management UI
    var postgres = builder.AddPostgres("postgres")
        .WithPgAdmin();
    webhookDb = postgres.AddDatabase("webhookdb");
    
    // RabbitMQ message broker with Management Plugin
    rabbitmq = builder.AddRabbitMQ("rabbitmq")
        .WithManagementPlugin();
    
    // Redis cache
    redis = builder.AddRedis("redis");
}

// Service Projects

// Service Projects - These will automatically be published as Azure Container Apps when using azure.pubxml

// API service - REST API for webhook management
var api = builder.AddProject<Projects.HookVerse_Api>("api")
    .WithReference(webhookDb)
    .WithReference(rabbitmq)
    .WithReference(redis)
    .WithReplicas(3);

// Worker service - Background job processor for webhook delivery
var worker = builder.AddProject<Projects.HookVerse_Worker>("worker")
    .WithReference(webhookDb)
    .WithReference(rabbitmq)
    .WithReference(redis)
    .WithReplicas(2);

// Dashboard service - Admin UI for webhook monitoring
var dashboard = builder.AddProject<Projects.HookVerse_Dashboard>("dashboard")
    .WithReference(webhookDb)
    .WithReference(api)
    .WithReplicas(1);

builder.Build().Run();
