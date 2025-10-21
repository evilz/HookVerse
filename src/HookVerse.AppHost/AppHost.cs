var builder = DistributedApplication.CreateBuilder(args);

// Container Resources - Infrastructure

// PostgreSQL database with PgAdmin management UI
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var webhookDb = postgres.AddDatabase("webhookdb");

// RabbitMQ message broker with Management Plugin
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

// Redis cache
var redis = builder.AddRedis("redis");

// Service Projects

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
