using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InternalOperations.Api.IntegrationTests;

public sealed class AuthenticationContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public AuthenticationContractTests(WebApplicationFactory<Program> factory)
        => _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Authentication:Jwt:Issuer", "integration-tests");
            builder.UseSetting("Authentication:Jwt:Audience", "internal-operations-api");
            builder.UseSetting("Authentication:Jwt:SigningKey", "0123456789ABCDEF0123456789ABCDEF");
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task HealthIsAnonymousAndProtectedTicketReturnsProblemDetailsChallenge()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/v1/health")).StatusCode);
        var response = await _client.PostAsJsonAsync("/api/v1/tickets", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("auth.unauthenticated", body!["code"].ToString());
    }

    [Fact]
    public async Task InvalidLoginBodyUsesValidationProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = "", password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task UnknownLoginUsesGenericUnauthorizedCode()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = "missing@example.test", password = "not-a-real-secret" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("auth.invalid_credentials", body!["code"].ToString());
    }
}
