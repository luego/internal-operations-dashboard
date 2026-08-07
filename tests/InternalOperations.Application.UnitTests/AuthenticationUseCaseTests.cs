using InternalOperations.Application;
using InternalOperations.Application.Abstractions.Authentication;
using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Application.Common.Authorization;
using InternalOperations.Application.Features.Auth;

namespace InternalOperations.Application.UnitTests;

public sealed class AuthenticationUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LoginCreatesHashedRefreshSessionAndReturnsPlaintextOnce()
    {
        var sessions = new FakeSessions();
        var handler = LoginHandler(new FakeAccounts(Result<AuthenticatedAccount>.Success(Account())), sessions);
        var result = await handler.Handle(new LoginCommand("agent@example.test", "password", "device"), default);
        Assert.True(result.IsSuccess);
        Assert.Equal("plain-refresh", result.Value!.RefreshToken);
        Assert.Equal("hashed-refresh", sessions.Added!.TokenHash);
        Assert.DoesNotContain("plain-refresh", sessions.Added.TokenHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefreshRotatesTokenAndReplayRevokesEntireFamily()
    {
        var session = ActiveSession();
        var sessions = new FakeSessions { Found = session };
        var handler = RefreshHandler(sessions, new FakeUnitOfWork());
        Assert.True((await handler.Handle(new RefreshSessionCommand("old-token", null), default)).IsSuccess);
        Assert.NotNull(session.RevokedAtUtc);
        Assert.NotNull(session.ReplacedByTokenId);
        sessions.Found = session;
        var replay = await handler.Handle(new RefreshSessionCommand("old-token", null), default);
        Assert.False(replay.IsSuccess);
        Assert.True(sessions.FamilyRevoked);
        Assert.Equal("auth.invalid_refresh_token", replay.Error!.Code);
    }

    [Fact]
    public async Task ReplayOfExpiredReplacedTokenStillRevokesEntireFamily()
    {
        var replaced = new RefreshTokenSession(
            Guid.NewGuid(),
            Account().Id,
            Guid.NewGuid(),
            "hashed-refresh",
            Now.AddDays(-14),
            Now.AddDays(-7),
            null);
        replaced.Revoke(Now.AddDays(-8), Guid.NewGuid());
        var sessions = new FakeSessions { Found = replaced };

        var replay = await RefreshHandler(sessions, new FakeUnitOfWork())
            .Handle(new RefreshSessionCommand("old-token", null), default);

        Assert.False(replay.IsSuccess);
        Assert.True(sessions.FamilyRevoked);
        Assert.Equal("auth.invalid_refresh_token", replay.Error!.Code);
    }

    [Fact]
    public async Task LoginUsesConfiguredRefreshLifetimeAndLimitsDeviceDescription()
    {
        var sessions = new FakeSessions();
        var handler = LoginHandler(new FakeAccounts(Result<AuthenticatedAccount>.Success(Account())), sessions, TimeSpan.FromDays(3));
        await handler.Handle(new LoginCommand("agent", "password", new string('x', 201)), default);
        Assert.Equal(Now.AddDays(3), sessions.Added!.ExpiresAtUtc);
        Assert.Equal(200, sessions.Added.DeviceDescription!.Length);
    }

    [Fact]
    public async Task RefreshLimitsDeviceDescriptionAndTranslatesConcurrentCommit()
    {
        var sessions = new FakeSessions { Found = ActiveSession() };
        var result = await RefreshHandler(sessions, new ThrowingUnitOfWork()).Handle(new RefreshSessionCommand("old", new string('x', 201)), default);
        Assert.False(result.IsSuccess);
        Assert.Equal("auth.invalid_refresh_token", result.Error!.Code);
        Assert.Equal(200, sessions.Added!.DeviceDescription!.Length);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("inactive")]
    [InlineData("locked")]
    [InlineData("wrong-password")]
    public async Task LoginMapsEveryAccountFailureToSamePublicError(string privateReason)
    {
        var failure = Result<AuthenticatedAccount>.Failure(Error.Unauthorized(privateReason, "private"));
        var result = await LoginHandler(new FakeAccounts(failure), new FakeSessions()).Handle(new LoginCommand("identifier", "password", null), default);
        Assert.Equal(AuthErrors.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task LogoutIsIdempotentForUnknownToken()
    {
        var unit = new FakeUnitOfWork();
        var result = await new LogoutCommandHandler(new FakeRefreshTokens(), new FakeSessions(), unit, new FakeClock()).Handle(new LogoutCommand("unknown"), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(0, unit.SaveCount);
    }

    private static LoginCommandHandler LoginHandler(FakeAccounts accounts, FakeSessions sessions, TimeSpan? lifetime = null) => new(accounts, new FakeAccessTokens(), new FakeRefreshTokens(), sessions, new FakeUnitOfWork(), new FakeClock(), new FakeSessionSettings(lifetime));
    private static RefreshSessionCommandHandler RefreshHandler(FakeSessions sessions, IUnitOfWork unit) => new(new FakeAccounts(Result<AuthenticatedAccount>.Success(Account())), new FakeAccessTokens(), new FakeRefreshTokens(), sessions, unit, new FakeClock(), new FakeSessionSettings());
    private static AuthenticatedAccount Account() => new(Guid.NewGuid(), "Agent", [ApplicationRoles.Agent]);
    private static RefreshTokenSession ActiveSession() => new(Guid.NewGuid(), Account().Id, Guid.NewGuid(), "hashed-refresh", Now, Now.AddDays(7), null);
    private sealed class FakeClock : IClock { public DateTimeOffset UtcNow => Now; }
    private sealed class FakeSessionSettings(TimeSpan? lifetime = null) : IAuthenticationSessionSettings { public TimeSpan RefreshTokenLifetime { get; } = lifetime ?? TimeSpan.FromDays(7); }
    private sealed class FakeAccessTokens : IAccessTokenIssuer { public AccessTokenResult Issue(AuthenticatedAccount account, DateTimeOffset now) => new("access", now.AddMinutes(15)); }
    private sealed class FakeRefreshTokens : IRefreshTokenGenerator { public GeneratedRefreshToken Generate() => new("plain-refresh", "hashed-refresh"); public string Hash(string token) => "hashed-refresh"; }
    private sealed class FakeAccounts(Result<AuthenticatedAccount> result) : IIdentityAuthenticationService
    {
        public Task<Result<AuthenticatedAccount>> AuthenticateAsync(string identifier, string password, CancellationToken cancellationToken) => Task.FromResult(result);
        public Task<Result<AuthenticatedAccount>> GetActiveAccountAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(result);
    }
    private sealed class FakeSessions : IRefreshTokenSessionRepository
    {
        public RefreshTokenSession? Found { get; set; }
        public RefreshTokenSession? Added { get; private set; }
        public bool FamilyRevoked { get; private set; }
        public Task<RefreshTokenSession?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult(Found);
        public Task AddAsync(RefreshTokenSession session, CancellationToken cancellationToken) { Added = session; return Task.CompletedTask; }
        public Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken) { FamilyRevoked = true; return Task.CompletedTask; }
    }
    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { SaveCount++; return Task.FromResult(1); }
    }
    private sealed class ThrowingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new PersistenceConcurrencyException();
    }
}
