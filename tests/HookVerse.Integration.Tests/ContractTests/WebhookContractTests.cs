using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using HookVerse.Core.Entities;
using HookVerse.Core.Enums;
using Xunit;

namespace HookVerse.Integration.Tests.ContractTests;

public class WebhookContractTests : IntegrationTestBase
{
    [Fact(Skip = ".NET 10 RC bug: PipeWriter.UnflushedBytes not implemented in TestHost - will be fixed in RTM")]
    public async Task PostWebhook_WithValidData_ShouldReturn_Accepted()
    {
        await InitializeAsync();
        try
        {
            // Arrange: Seed test data
            var subscriber = new Subscriber
            {
                Id = Guid.NewGuid(),
                Name = "Test Subscriber",
                Email = "test@example.com",
                ApiKeyHash = "test-hash",
                IsActive = true,
                RetentionDays = 90
            };

            // Create API key with hash of "test-api-key"
            var testApiKey = "test-api-key";
            var hashedKey = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(testApiKey)));

            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                TenantId = subscriber.Id,
                Name = "Test API Key",
                HashedKey = hashedKey,
                KeyPrefix = testApiKey.Substring(0, Math.Min(8, testApiKey.Length)),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var eventType = new EventType
            {
                Id = Guid.NewGuid(),
                Name = "test.event",
                Description = "Test event type",
                Version = "1.0.0",
                SubscriberId = subscriber.Id,
                IsActive = true
            };

            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                SubscriberId = subscriber.Id,
                EventTypeId = eventType.Id,
                EndpointUrl = "https://example.com/webhook",
                Secret = "test-secret-minimum-32-characters-long!",
                IsActive = true,
                AuthType = AuthType.None
            };

            await SeedTestDataAsync<Subscriber>(subscriber);
            await SeedTestDataAsync<ApiKey>(apiKey);
            await SeedTestDataAsync<EventType>(eventType);
            await SeedTestDataAsync<Subscription>(subscription);

            var client = CreateClient();

            // Act: Send webhook with proper request model
            var payload = new
            {
                eventTypeId = eventType.Id,
                payload = "{\"message\":\"hello\",\"timestamp\":\"2025-10-20T12:00:00Z\"}",
                scheduledFor = (DateTime?)null,
                metadata = (string?)null
            };

            // Add API key header to simulate authentication
            client.DefaultRequestHeaders.Add("X-Api-Key", testApiKey);

            var response = await client.PostAsJsonAsync("/api/v1/webhooks/send", payload);

            // Assert: Verify accepted response
            var responseContent = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted, 
                $"response was: {responseContent}");

            responseContent.Should().NotBeNullOrEmpty();
        }
        finally
        {
            await DisposeAsync();
        }
    }

    [Fact(Skip = ".NET 10 RC bug: PipeWriter.UnflushedBytes not implemented in TestHost - will be fixed in RTM")]
    public async Task PostWebhook_WithInvalidEventType_ShouldReturn_NotFound()
    {
        await InitializeAsync();
        try
        {
            // Arrange: Seed only subscriber, no event type
            var subscriber = new Subscriber
            {
                Id = Guid.NewGuid(),
                Name = "Test Subscriber",
                Email = "test2@example.com",
                ApiKeyHash = "test-hash",
                IsActive = true
            };

            await SeedTestDataAsync<Subscriber>(subscriber);

            var client = CreateClient();

            // Act: Send webhook with non-existent event type
            var payload = new
            {
                eventType = "nonexistent.event",
                data = new { message = "hello" },
                subscriberId = subscriber.Id.ToString()
            };

            var response = await client.PostAsJsonAsync("/api/v1/webhooks/send", payload);

            // Assert: Should return NotFound or BadRequest
            response.StatusCode.Should().BeOneOf(
                System.Net.HttpStatusCode.NotFound, 
                System.Net.HttpStatusCode.BadRequest);
        }
        finally
        {
            await DisposeAsync();
        }
    }
}
