using System.Security.Cryptography;
using System.Text;
using InternalOperations.Application;
using InternalOperations.Application.Abstractions.Authentication;
using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Application.Features.Auth;
using InternalOperations.Application.Features.Departments;
using InternalOperations.Domain.Departments;
using InternalOperations.Domain.Users;
using InternalOperations.Persistence;
using InternalOperations.Persistence.Authentication;
using InternalOperations.Persistence.Context;
using InternalOperations.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

namespace InternalOperations.ProviderContractTests;

public sealed class RelationalProviderContractTests
{

    [Fact]
    [Trait("Category", "ProviderMatrix")]
    [Trait("Provider", "PostgreSql")]
    public async Task PostgreSqlSatisfiesMigrationConstraintAndConcurrencyContract()
    {
        await using var container = new PostgreSqlBuilder("postgres:15.1").Build();
        await container.StartAsync();

        await using var services = Services(options => options.UseNpgsql(
            container.GetConnectionString(),
            provider => provider.MigrationsAssembly(MigrationAssemblyNames.PostgreSql)));

        await VerifyProviderAsync(services);
    }

    [Fact]
    [Trait("Category", "ProviderMatrix")]
    [Trait("Provider", "SqlServer")]
    public async Task SqlServerSatisfiesMigrationConstraintAndConcurrencyContract()
    {
        await using var container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await container.StartAsync();

        await using var services = Services(options => options.UseSqlServer(
            container.GetConnectionString(),
            provider => provider.MigrationsAssembly(MigrationAssemblyNames.SqlServer)));

        await VerifyProviderAsync(services);
    }

    private static async Task VerifyProviderAsync(ServiceProvider services)
    {
        var contexts = services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        Func<ApplicationDbContext> createContext = contexts.CreateDbContext;
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tokenHash = new string('A', 64);
        var now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        await using (var context = createContext())
        {
            await context.Database.MigrateAsync();
            await AssertAllMigrationsAppliedAsync(context);

            context.Set<IdentityAccount>().Add(new IdentityAccount
            {
                Id = userId,
                UserName = "provider-matrix-agent",
                NormalizedUserName = "PROVIDER-MATRIX-AGENT",
                DisplayName = "Provider Matrix Agent",
                IsActive = true,
            });
            context.DomainUsers.Add(new User(userId, "provider-matrix-agent", "Provider Matrix Agent")
            {
                CreatedAtUtc = DateTime.UtcNow,
            });
            context.Set<RefreshTokenSessionEntity>().Add(
                new RefreshTokenSessionEntity(
                    new RefreshTokenSession(
                        sessionId,
                        userId,
                        Guid.NewGuid(),
                        tokenHash,
                        now,
                        now.AddDays(7),
                        "provider-contract")));
            await context.SaveChangesAsync();
        }

        await AssertUniqueTokenHashAsync(createContext, userId, tokenHash, now);
        await AssertDepartmentFoundationAsync(createContext);
        await AssertLogicalDeletionAsync(createContext);
        await AssertAuthenticationAndLockoutAsync(services);
        await AssertHandlerSessionContractAsync(services, now);
        await AssertConcurrentRefreshAsync(services, now);
        await AssertRefreshSessionLifecycleAsync(createContext, now);
        await AssertOptimisticConcurrencyAsync(createContext, sessionId, now);
        await AssertAccountLogicalDeletionPreservesDomainProfileAsync(createContext);
        await AssertAccountLogicalDeletionPreservesRefreshSessionAsync(createContext, now);
        await AssertRollbackAndReapplyAsync(createContext);
    }

    private static async Task AssertDepartmentFoundationAsync(Func<ApplicationDbContext> createContext)
    {
        var createdAtUtc = new DateTime(2026, 8, 7, 18, 0, 0, DateTimeKind.Utc);
        var original = Department.Create(" Provider  Operations ", "Provider contract", createdAtUtc);
        await using (var context = createContext())
        {
            await new DepartmentRepository(context).AddAsync(original, default);
            await new UnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = createContext())
        {
            var duplicate = Department.Create("provider operations", null, createdAtUtc);
            await new DepartmentRepository(context).AddAsync(duplicate, default);
            await Assert.ThrowsAsync<PersistenceUniqueConstraintException>(
                () => new UnitOfWork(context).SaveChangesAsync());
        }

        await using (var context = createContext())
        {
            var projected = await new DepartmentReadService(context).GetAsync(original.Id, default);
            Assert.NotNull(projected);
            Assert.Equal("Provider Operations", projected.Name);
            Assert.Equal(createdAtUtc, projected.CreatedAtUtc);

            var page = await new DepartmentReadService(context).ListAsync(
                new DepartmentListFilter(1, 25, "provider", true, "name", "asc"),
                default);
            Assert.Contains(page.Items, item => item.Id == original.Id);
        }

        await using (var firstContext = createContext())
        await using (var secondContext = createContext())
        {
            var first = await firstContext.Departments.SingleAsync(x => x.Id == original.Id);
            var stale = await secondContext.Departments.SingleAsync(x => x.Id == original.Id);
            first.Update("Provider Customer Operations", "First writer", createdAtUtc.AddMinutes(1));
            await new UnitOfWork(firstContext).SaveChangesAsync();

            stale.Update("Provider Service Operations", "Stale writer", createdAtUtc.AddMinutes(2));
            await Assert.ThrowsAsync<PersistenceConcurrencyException>(
                () => new UnitOfWork(secondContext).SaveChangesAsync());
        }

        await using (var context = createContext())
        {
            var tracked = await context.Departments.SingleAsync(x => x.Id == original.Id);
            tracked.Deactivate(createdAtUtc.AddMinutes(3));
            await new UnitOfWork(context).SaveChangesAsync();
            Assert.False(tracked.IsActive);
            Assert.NotEqual(original.Version, tracked.Version);

            context.Remove(tracked);
            await context.SaveChangesAsync();
        }

        await using (var context = createContext())
        {
            context.Departments.Add(Department.Create("Provider Customer Operations", "Reused after logical deletion", createdAtUtc));
            await context.SaveChangesAsync();
        }
    }

    private static async Task AssertLogicalDeletionAsync(Func<ApplicationDbContext> createContext)
    {
        var department = Department.Create("Logical deletion contract", null);
        await using (var context = createContext())
        {
            context.Departments.Add(department);
            await context.SaveChangesAsync();
            context.Remove(department);
            await context.SaveChangesAsync();
        }

        await using (var context = createContext())
        {
            Assert.False(await context.Departments.AnyAsync(x => x.Id == department.Id));
            var persisted = await context.Departments.IgnoreQueryFilters().SingleAsync(x => x.Id == department.Id);
            Assert.True(persisted.IsDeleted);
        }
    }

    private static async Task AssertAuthenticationAndLockoutAsync(ServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        Assert.True((await roles.CreateAsync(new IdentityRole<Guid>("Agent"))).Succeeded);

        var account = Account("authentication-contract-agent");
        account.Email = "authentication-contract-agent@example.test";
        Assert.True((await users.CreateAsync(account, "Valid-password-123!")).Succeeded);
        Assert.True((await users.AddToRoleAsync(account, "Agent")).Succeeded);
        var authentication = new IdentityAuthenticationService(users);

        var byName = await authentication.AuthenticateAsync(account.UserName!, "Valid-password-123!", default);
        var byEmail = await authentication.AuthenticateAsync(account.Email, "Valid-password-123!", default);
        Assert.True(byName.IsSuccess);
        Assert.True(byEmail.IsSuccess);
        Assert.Contains("Agent", byName.Value!.Roles);

        for (var attempt = 0; attempt < 5; attempt++)
            Assert.False((await authentication.AuthenticateAsync(account.UserName!, "wrong-password", default)).IsSuccess);

        Assert.True(await users.IsLockedOutAsync(account));
        await users.SetLockoutEndDateAsync(account, DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.True((await authentication.AuthenticateAsync(account.UserName!, "Valid-password-123!", default)).IsSuccess);
        Assert.Equal(0, await users.GetAccessFailedCountAsync(account));

        account.IsActive = false;
        Assert.True((await users.UpdateAsync(account)).Succeeded);
        var inactive = await authentication.AuthenticateAsync(account.UserName!, "Valid-password-123!", default);
        var unknown = await authentication.AuthenticateAsync("unknown-provider-account", "Valid-password-123!", default);
        Assert.Equal("auth.invalid_credentials", inactive.Error!.Code);
        Assert.Equal(inactive.Error, unknown.Error);
    }

    private static async Task AssertHandlerSessionContractAsync(ServiceProvider services, DateTimeOffset now)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var users = provider.GetRequiredService<UserManager<IdentityAccount>>();
        var account = Account("handler-session-agent");
        account.Email = "handler-session-agent@example.test";
        Assert.True((await users.CreateAsync(account, "Valid-password-123!")).Succeeded);
        Assert.True((await users.AddToRoleAsync(account, "Agent")).Succeeded);

        var context = provider.GetRequiredService<ApplicationDbContext>();
        var authentication = new IdentityAuthenticationService(users);
        var tokenGenerator = new DeterministicRefreshTokenGenerator("handler");
        var accessTokens = new DeterministicAccessTokenIssuer();
        var clock = new FixedClock(now);
        var settings = new FixedSessionSettings();
        var sessions = new RefreshTokenSessionRepository(context);
        var unitOfWork = new UnitOfWork(context);

        var login = await new LoginCommandHandler(authentication, accessTokens, tokenGenerator, sessions, unitOfWork, clock, settings)
            .Handle(new LoginCommand(account.UserName!, "Valid-password-123!", "provider contract"), default);
        Assert.True(login.IsSuccess);
        Assert.Contains("handler-access", login.Value!.AccessToken, StringComparison.Ordinal);

        var firstPlaintext = login.Value.RefreshToken;
        var firstHash = tokenGenerator.Hash(firstPlaintext);
        context.ChangeTracker.Clear();
        var first = await context.RefreshTokenSessions.SingleAsync(x => x.TokenHash == firstHash);
        Assert.NotEqual(firstPlaintext, first.TokenHash);
        Assert.DoesNotContain(firstPlaintext, first.DeviceDescription ?? string.Empty, StringComparison.Ordinal);

        var refreshHandler = new RefreshSessionCommandHandler(authentication, accessTokens, tokenGenerator, sessions, unitOfWork, clock, settings);
        var rotated = await refreshHandler.Handle(new RefreshSessionCommand(firstPlaintext, "rotated"), default);
        Assert.True(rotated.IsSuccess);
        var secondPlaintext = rotated.Value!.RefreshToken;

        context.ChangeTracker.Clear();
        first = await context.RefreshTokenSessions.SingleAsync(x => x.TokenHash == firstHash);
        var second = await context.RefreshTokenSessions.SingleAsync(x => x.TokenHash == tokenGenerator.Hash(secondPlaintext));
        Assert.Equal(first.FamilyId, second.FamilyId);
        Assert.Equal(second.Id, first.ReplacedByTokenId);
        Assert.Equal(now, first.RevokedAtUtc);

        var replay = await refreshHandler.Handle(new RefreshSessionCommand(firstPlaintext, null), default);
        Assert.False(replay.IsSuccess);
        Assert.Equal(AuthErrors.InvalidRefreshToken, replay.Error);

        context.ChangeTracker.Clear();
        var family = await context.RefreshTokenSessions.Where(x => x.FamilyId == first.FamilyId).ToArrayAsync();
        Assert.All(family, session => Assert.NotNull(session.RevokedAtUtc));

        var nextLogin = await new LoginCommandHandler(authentication, accessTokens, tokenGenerator, sessions, unitOfWork, clock, settings)
            .Handle(new LoginCommand(account.Email!, "Valid-password-123!", null), default);
        Assert.True(nextLogin.IsSuccess);
        var logoutToken = nextLogin.Value!.RefreshToken;
        var logout = new LogoutCommandHandler(tokenGenerator, sessions, unitOfWork, clock);
        Assert.True((await logout.Handle(new LogoutCommand(logoutToken), default)).IsSuccess);
        Assert.True((await logout.Handle(new LogoutCommand(logoutToken), default)).IsSuccess);
        Assert.True((await logout.Handle(new LogoutCommand("unknown-token"), default)).IsSuccess);
        Assert.True((await logout.Handle(new LogoutCommand(string.Empty), default)).IsSuccess);

        context.ChangeTracker.Clear();
        Assert.NotNull((await context.RefreshTokenSessions.SingleAsync(x => x.TokenHash == tokenGenerator.Hash(logoutToken))).RevokedAtUtc);

        context.RefreshTokenSessions.Add(Session(Guid.NewGuid(), account.Id, Guid.NewGuid(), tokenGenerator.Hash("expired-token"), now.AddDays(-8)));
        await context.SaveChangesAsync();
        Assert.False((await refreshHandler.Handle(new RefreshSessionCommand("expired-token", null), default)).IsSuccess);
    }

    private static async Task AssertConcurrentRefreshAsync(ServiceProvider services, DateTimeOffset now)
    {
        string token;
        Guid familyId;
        await using (var setupScope = services.CreateAsyncScope())
        {
            var provider = setupScope.ServiceProvider;
            var users = provider.GetRequiredService<UserManager<IdentityAccount>>();
            var account = Account("concurrent-refresh-agent");
            Assert.True((await users.CreateAsync(account, "Valid-password-123!")).Succeeded);
            Assert.True((await users.AddToRoleAsync(account, "Agent")).Succeeded);
            var context = provider.GetRequiredService<ApplicationDbContext>();
            var generator = new DeterministicRefreshTokenGenerator("race-login");
            var login = await new LoginCommandHandler(
                new IdentityAuthenticationService(users),
                new DeterministicAccessTokenIssuer(),
                generator,
                new RefreshTokenSessionRepository(context),
                new UnitOfWork(context),
                new FixedClock(now),
                new FixedSessionSettings())
                .Handle(new LoginCommand(account.UserName!, "Valid-password-123!", null), default);
            Assert.True(login.IsSuccess);
            token = login.Value!.RefreshToken;
            familyId = (await context.RefreshTokenSessions.SingleAsync(x => x.TokenHash == generator.Hash(token))).FamilyId;
        }

        var barrier = new AsyncReadBarrier(2);
        var first = RefreshInIndependentScopeAsync(services, token, "race-a", now, barrier);
        var second = RefreshInIndependentScopeAsync(services, token, "race-b", now, barrier);
        var results = await Task.WhenAll(first, second);
        Assert.Single(results, result => result.IsSuccess);
        Assert.Single(results, result => !result.IsSuccess && result.Error == AuthErrors.InvalidRefreshToken);

        await using var verificationScope = services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var family = await verificationContext.RefreshTokenSessions.Where(x => x.FamilyId == familyId).ToArrayAsync();
        Assert.Equal(2, family.Length);
        Assert.Single(family, session => session.RevokedAtUtc is null);
        Assert.Single(family, session => session.ReplacedByTokenId is not null);
    }

    private static async Task<Result<TokenPairResult>> RefreshInIndependentScopeAsync(
        ServiceProvider services,
        string token,
        string tokenPrefix,
        DateTimeOffset now,
        AsyncReadBarrier barrier)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var context = provider.GetRequiredService<ApplicationDbContext>();
        var users = provider.GetRequiredService<UserManager<IdentityAccount>>();
        var generator = new DeterministicRefreshTokenGenerator(tokenPrefix);
        var repository = new BarrierRefreshTokenSessionRepository(new RefreshTokenSessionRepository(context), barrier);
        return await new RefreshSessionCommandHandler(
            new IdentityAuthenticationService(users),
            new DeterministicAccessTokenIssuer(),
            generator,
            repository,
            new UnitOfWork(context),
            new FixedClock(now),
            new FixedSessionSettings())
            .Handle(new RefreshSessionCommand(token, null), default);
    }

    private static async Task AssertRefreshSessionLifecycleAsync(
        Func<ApplicationDbContext> createContext,
        DateTimeOffset now)
    {
        var familyId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var account = Account("refresh-lifecycle-agent");

        await using (var context = createContext())
        {
            context.Set<IdentityAccount>().Add(account);
            context.RefreshTokenSessions.AddRange(
                Session(firstId, account.Id, familyId, new string('C', 64), now),
                Session(Guid.NewGuid(), account.Id, familyId, new string('D', 64), now));
            await context.SaveChangesAsync();
        }

        await using (var context = createContext())
        {
            var repository = new RefreshTokenSessionRepository(context);
            var found = await repository.GetByHashAsync(new string('C', 64), default);
            Assert.Equal(firstId, found!.Id);

            await repository.RevokeFamilyAsync(familyId, now.AddMinutes(3), default);
            await context.SaveChangesAsync();
        }

        await using (var context = createContext())
        {
            var sessions = await context.RefreshTokenSessions
                .Where(x => x.FamilyId == familyId)
                .ToArrayAsync();
            Assert.Equal(2, sessions.Length);
            Assert.All(sessions, session => Assert.Equal(now.AddMinutes(3), session.RevokedAtUtc));
        }
    }

    private static async Task AssertAllMigrationsAppliedAsync(ApplicationDbContext context)
    {
        var expected = context.Database.GetMigrations().ToArray();
        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();

        Assert.NotEmpty(expected);
        Assert.Equal(expected, applied);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    private static async Task AssertUniqueTokenHashAsync(
        Func<ApplicationDbContext> createContext,
        Guid userId,
        string tokenHash,
        DateTimeOffset now)
    {
        await using var context = createContext();
        context.Set<RefreshTokenSessionEntity>().Add(
            new RefreshTokenSessionEntity(
                new RefreshTokenSession(
                    Guid.NewGuid(),
                    userId,
                    Guid.NewGuid(),
                    tokenHash,
                    now,
                    now.AddDays(7),
                    null)));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static async Task AssertOptimisticConcurrencyAsync(
        Func<ApplicationDbContext> createContext,
        Guid sessionId,
        DateTimeOffset now)
    {
        await using var winnerContext = createContext();
        await using var loserContext = createContext();
        var winner = await winnerContext.Set<RefreshTokenSessionEntity>().SingleAsync(x => x.Id == sessionId);
        var loser = await loserContext.Set<RefreshTokenSessionEntity>().SingleAsync(x => x.Id == sessionId);

        winner.Revoke(now.AddMinutes(1));
        await winnerContext.SaveChangesAsync();
        loser.Revoke(now.AddMinutes(2));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => loserContext.SaveChangesAsync());
    }

    private static async Task AssertAccountLogicalDeletionPreservesDomainProfileAsync(
        Func<ApplicationDbContext> createContext)
    {
        await using var context = createContext();
        var account = Account("profile-restrict-agent");
        context.Set<IdentityAccount>().Add(account);
        context.DomainUsers.Add(new User(account.Id, account.UserName!, "Profile Restrict Agent")
        {
            CreatedAtUtc = DateTime.UtcNow,
        });
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var persistedAccount = await context.Set<IdentityAccount>().SingleAsync(x => x.Id == account.Id);
        context.Remove(persistedAccount);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        Assert.False(await context.Set<IdentityAccount>().AnyAsync(x => x.Id == account.Id));
        var deletedAccount = await context.Set<IdentityAccount>().IgnoreQueryFilters().SingleAsync(x => x.Id == account.Id);
        Assert.True(deletedAccount.IsDeleted);
        Assert.False(deletedAccount.IsActive);
        Assert.True(await context.DomainUsers.AnyAsync(x => x.Id == account.Id));
    }

    private static async Task AssertAccountLogicalDeletionPreservesRefreshSessionAsync(
        Func<ApplicationDbContext> createContext,
        DateTimeOffset now)
    {
        await using var context = createContext();
        var account = Account("session-restrict-agent");
        context.Set<IdentityAccount>().Add(account);
        context.Set<RefreshTokenSessionEntity>().Add(
            new RefreshTokenSessionEntity(
                new RefreshTokenSession(
                    Guid.NewGuid(),
                    account.Id,
                    Guid.NewGuid(),
                    new string('B', 64),
                    now,
                    now.AddDays(7),
                    null)));
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var persistedAccount = await context.Set<IdentityAccount>().SingleAsync(x => x.Id == account.Id);
        context.Remove(persistedAccount);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        Assert.False(await context.Set<IdentityAccount>().AnyAsync(x => x.Id == account.Id));
        Assert.True(await context.RefreshTokenSessions.AnyAsync(x => x.UserId == account.Id));
    }

    private static async Task AssertRollbackAndReapplyAsync(Func<ApplicationDbContext> createContext)
    {
        await using var context = createContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(Migration.InitialDatabase);
        Assert.Empty(await context.Database.GetAppliedMigrationsAsync());

        await context.Database.MigrateAsync();
        await AssertAllMigrationsAppliedAsync(context);
    }

    private static IdentityAccount Account(string userName) => new()
    {
        Id = Guid.NewGuid(),
        UserName = userName,
        NormalizedUserName = userName.ToUpperInvariant(),
        DisplayName = userName,
        IsActive = true,
    };

    private static RefreshTokenSessionEntity Session(
        Guid id,
        Guid userId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset now)
        => new(new RefreshTokenSession(id, userId, familyId, tokenHash, now, now.AddDays(7), null));

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FixedSessionSettings : IAuthenticationSessionSettings
    {
        public TimeSpan RefreshTokenLifetime { get; } = TimeSpan.FromDays(7);
    }

    private sealed class DeterministicAccessTokenIssuer : IAccessTokenIssuer
    {
        public AccessTokenResult Issue(AuthenticatedAccount account, DateTimeOffset now)
            => new($"handler-access-{account.Id:N}", now.AddMinutes(15));
    }

    private sealed class DeterministicRefreshTokenGenerator(string prefix) : IRefreshTokenGenerator
    {
        private int _sequence;

        public GeneratedRefreshToken Generate()
        {
            var plaintext = $"{prefix}-refresh-{Interlocked.Increment(ref _sequence)}";
            return new GeneratedRefreshToken(plaintext, Hash(plaintext));
        }

        public string Hash(string token)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private sealed class AsyncReadBarrier(int participants)
    {
        private int _arrivals;
        private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task SignalAndWaitAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _arrivals) == participants)
                _released.TrySetResult();
            await _released.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class BarrierRefreshTokenSessionRepository(
        IRefreshTokenSessionRepository inner,
        AsyncReadBarrier barrier) : IRefreshTokenSessionRepository
    {
        public async Task<RefreshTokenSession?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            var session = await inner.GetByHashAsync(tokenHash, cancellationToken);
            await barrier.SignalAndWaitAsync(cancellationToken);
            return session;
        }

        public Task AddAsync(RefreshTokenSession session, CancellationToken cancellationToken)
            => inner.AddAsync(session, cancellationToken);

        public Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken)
            => inner.RevokeFamilyAsync(familyId, now, cancellationToken);
    }

    private static ServiceProvider Services(Action<DbContextOptionsBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContextFactory<ApplicationDbContext>(configure);
        services.AddIdentityCore<IdentityAccount>(options =>
        {
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<ApplicationDbContext>();
        return services.BuildServiceProvider();
    }
}
