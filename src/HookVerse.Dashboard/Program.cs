using HookVerse.Dashboard.Components;
using HookVerse.Dashboard.Services;
using HookVerse.Dashboard.Hubs;
using HookVerse.Dashboard.Middleware;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (service discovery, OpenTelemetry, health checks)
builder.AddServiceDefaults();

// Register custom ActivitySource for distributed tracing
builder.Services.AddSingleton(new ActivitySource("HookVerse.Dashboard"));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add SignalR for real-time notifications
builder.Services.AddSignalR();

// Register HttpClient for API calls with service discovery
// Use Aspire service name "http://api" or fallback to configured URL for non-Aspire scenarios
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://api";
builder.Services.AddHttpClient<IWebhookApiClient, WebhookApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    // TODO: Add authentication headers when implementing API key auth
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Use dashboard authentication middleware
app.UseDashboardAuthentication();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hub
app.MapHub<WebhookHub>("/webhookhub");

// Map Aspire default endpoints (/health/live, /health/ready)
app.MapDefaultEndpoints();

app.Run();
