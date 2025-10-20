using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace HookVerse.Integration.Tests.ContractTests;

public class WebhookContractTests : IntegrationTestBase
{
    [Fact]
    public async Task PostWebhook_ShouldReturn_Accepted()
    {
        await InitializeAsync();
        try
        {
            var client = CreateClient();

            var payload = new
            {
                eventType = "test.event",
                data = new { message = "hello" },
                subscriberId = System.Guid.NewGuid().ToString()
            };

            var response = await client.PostAsJsonAsync("/api/webhooks", payload);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

            // Optionally, assert response body contract if applicable
            // var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            // body.Should().ContainKey("id");
        }
        finally
        {
            await DisposeAsync();
        }
    }
}
