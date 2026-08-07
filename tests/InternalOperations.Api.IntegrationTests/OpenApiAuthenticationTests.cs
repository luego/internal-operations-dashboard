using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InternalOperations.Api.IntegrationTests;

public sealed class OpenApiAuthenticationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SigningKey = "OPENAPI_TEST_SIGNING_KEY_32_BYTES_MINIMUM";
    private readonly WebApplicationFactory<Program> _factory;

    public OpenApiAuthenticationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task DevelopmentDocumentDescribesBearerAndProtectedOperationsWithoutSecrets()
    {
        using var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Authentication:Jwt:Issuer", "openapi-tests");
            builder.UseSetting("Authentication:Jwt:Audience", "internal-operations-api");
            builder.UseSetting("Authentication:Jwt:SigningKey", SigningKey);
            builder.UseSetting("Authentication:Seed:Enabled", "false");
        }).CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var bearer = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());

        var paths = root.GetProperty("paths");
        var ticketsPath = paths.EnumerateObject().Single(path => path.Name.Equals("/api/v1/tickets", StringComparison.OrdinalIgnoreCase));
        var protectedOperation = ticketsPath.Value.GetProperty("post");
        Assert.True(protectedOperation.TryGetProperty("security", out var protectedSecurity), json);
        Assert.Contains(protectedSecurity.EnumerateArray(), requirement => requirement.TryGetProperty("Bearer", out _));

        var login = root.GetProperty("paths").GetProperty("/api/v1/auth/login").GetProperty("post");
        Assert.False(login.TryGetProperty("security", out var loginSecurity) && loginSecurity.GetArrayLength() > 0);
        Assert.DoesNotContain(SigningKey, json, StringComparison.Ordinal);
    }
}
