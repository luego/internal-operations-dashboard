using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using InternalOperations.Application.Abstractions.Authentication;
using InternalOperations.Application.Common.Authorization;
using InternalOperations.Infrastructure;
using InternalOperations.Infrastructure.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InternalOperations.Api.IntegrationTests;

public sealed class AuthenticationSecurityTests
{
    private const string Issuer = "integration-tests";
    private const string Audience = "internal-operations-api";
    private const string Key = "0123456789ABCDEF0123456789ABCDEF";

    [Fact]
    public async Task ProtectedEndpointChallengesInvalidJwtAndForbidsInsufficientRole()
    {
        await using var factory = Factory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken(ApplicationRoles.Viewer));
        var forbidden = await client.PostAsJsonAsync("/api/v1/tickets", new { });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Contains("auth.forbidden", await forbidden.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken(ApplicationRoles.Agent, new string('Z', 32)));
        var challenged = await client.PostAsJsonAsync("/api/v1/tickets", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, challenged.StatusCode);
        Assert.Contains("auth.unauthenticated", await challenged.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidJwtWithRequiredRoleExecutesProtectedEndpoint()
    {
        await using var factory = Factory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken(ApplicationRoles.Agent));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/tickets", new { })).StatusCode);
    }

    [Fact]
    public async Task LoginRateLimitReturnsSafeProblemAndRetryAfter()
    {
        await using var factory = Factory();
        var client = factory.CreateClient();
        HttpResponseMessage? response = null;
        for (var i = 0; i < 6; i++) response = await client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = "same@example.test", password = "secret" });
        Assert.Equal(HttpStatusCode.TooManyRequests, response!.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("auth.rate_limit_exceeded", body, StringComparison.Ordinal);
        Assert.DoesNotContain("same@example.test", body, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthEndpointsRejectNonJsonAndOversizedBodies()
    {
        await using var factory = Factory();
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await client.PostAsync("/api/v1/auth/login", new StringContent("identifier=x", Encoding.UTF8, "text/plain"))).StatusCode);
        var oversized = new StringContent("{\"identifier\":\"" + new string('x', 17_000) + "\",\"password\":\"x\"}", Encoding.UTF8, "application/json");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/auth/login", oversized)).StatusCode);
    }

    [Fact]
    public void JwtIssuerEmitsOnlyApprovedClaimsAndConfiguredLifetime()
    {
        var now = new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);
        var issuer = new JwtAccessTokenIssuer(Options.Create(new JwtOptions { Issuer = Issuer, Audience = Audience, SigningKey = Key }));
        var issued = issuer.Issue(new AuthenticatedAccount(Guid.NewGuid(), "Agent", [ApplicationRoles.Agent]), now);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(issued.Token);
        Assert.Equal(now.AddMinutes(15), issued.ExpiresAtUtc);
        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Header.Alg);
        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Equal([Audience], jwt.Audiences);
        Assert.All(jwt.Claims, claim => Assert.Contains(claim.Type, new[] { "sub", "name", "jti", "iat", "nbf", "exp", "iss", "aud", ClaimTypes.Role }));
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type.Contains("email", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("wrong-issuer", Audience, -1, 5)]
    [InlineData(Issuer, "wrong-audience", -1, 5)]
    [InlineData(Issuer, Audience, -10, -5)]
    public async Task JwtValidationRejectsInvalidIssuerAudienceAndLifetime(string issuer, string audience, int startsMinutes, int expiresMinutes)
    {
        await using var factory = Factory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken(ApplicationRoles.Agent, issuer: issuer, audience: audience, startsMinutes: startsMinutes, expiresMinutes: expiresMinutes));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/tickets", new { })).StatusCode);
    }

    [Fact]
    public void JwtAndSessionOptionsRejectUnsafeValues()
    {
        Assert.False(new JwtOptions().IsValid());
        Assert.False(new JwtOptions { Issuer = Issuer, Audience = Audience, SigningKey = "short" }.IsValid());
        Assert.False(new JwtOptions { Issuer = Issuer, Audience = Audience, SigningKey = new string('a', 32) }.IsValid());
        Assert.False(new JwtOptions { Issuer = Issuer, Audience = Audience, SigningKey = Key, ClockSkewSeconds = 31 }.IsValid());
        Assert.False(new JwtOptions { Issuer = Issuer, Audience = Audience, SigningKey = Key, AccessTokenMinutes = 16 }.IsValid());
        Assert.True(new JwtOptions { Issuer = Issuer, Audience = Audience, SigningKey = Key }.IsValid());
        Assert.False(new RefreshTokenOptions { Days = 0 }.IsValid());
        Assert.False(new RefreshTokenOptions { Days = 8 }.IsValid());
        Assert.False(new AuthenticationLockoutOptions { MaxFailedAccessAttempts = 0 }.IsValid());
        Assert.False(new AuthenticationLockoutOptions { MaxFailedAccessAttempts = 6 }.IsValid());
        Assert.False(new AuthenticationLockoutOptions { Minutes = 14 }.IsValid());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public void CurrentUserReturnsNullForMissingOrMalformedSubject(string? subject)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(subject is null ? [] : [new Claim("sub", subject)], "test"));
        var accessor = new CurrentUserAccessor(new HttpContextAccessor { HttpContext = context });
        Assert.Null(accessor.UserId);
        Assert.Null(accessor.UserName);
    }

    private static WebApplicationFactory<Program> Factory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Authentication:Jwt:Issuer", Issuer);
        builder.UseSetting("Authentication:Jwt:Audience", Audience);
        builder.UseSetting("Authentication:Jwt:SigningKey", Key);
    });

    private static string CreateToken(
        string role,
        string? signingKey = null,
        string issuer = Issuer,
        string audience = Audience,
        int startsMinutes = 0,
        int expiresMinutes = 5)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer,
            audience,
            [new Claim("sub", Guid.NewGuid().ToString()), new Claim("name", "Test"), new Claim(ClaimTypes.Role, role)],
            now.AddMinutes(startsMinutes).AddSeconds(-1),
            now.AddMinutes(expiresMinutes),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? Key)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
