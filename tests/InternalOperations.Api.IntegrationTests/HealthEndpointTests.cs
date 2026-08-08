using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InternalOperations.Api.IntegrationTests;

public sealed class HealthEndpointTests
{
    private const string SigningKey = "HEALTH_TEST_SIGNING_KEY_32_BYTES_MINIMUM";

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpointsAreAnonymousAndReturnStructuredStatus(string path)
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Authentication:Jwt:Issuer", "health-tests");
            builder.UseSetting("Authentication:Jwt:Audience", "internal-operations-api");
            builder.UseSetting("Authentication:Jwt:SigningKey", SigningKey);
            builder.UseSetting("Authentication:Seed:Enabled", "false");
            builder.UseSetting("ConnectionStrings:DefaultConnection", string.Empty);
        });
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", document.RootElement.GetProperty("status").GetString());
    }
}
