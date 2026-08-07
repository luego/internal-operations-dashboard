using InternalOperations.Application.Abstractions.Authentication;
using InternalOperations.Domain.Users;
using InternalOperations.Persistence.Authentication;
using InternalOperations.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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

        await VerifyProviderAsync(
            () => new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseNpgsql(
                        container.GetConnectionString(),
                        provider => provider.MigrationsAssembly(MigrationAssemblyNames.PostgreSql))
                    .Options));
    }

    [Fact]
    [Trait("Category", "ProviderMatrix")]
    [Trait("Provider", "SqlServer")]
    public async Task SqlServerSatisfiesMigrationConstraintAndConcurrencyContract()
    {
        await using var container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await container.StartAsync();

        await VerifyProviderAsync(
            () => new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlServer(
                        container.GetConnectionString(),
                        provider => provider.MigrationsAssembly(MigrationAssemblyNames.SqlServer))
                    .Options));
    }

    private static async Task VerifyProviderAsync(Func<ApplicationDbContext> createContext)
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tokenHash = new string('A', 64);
        var now = DateTimeOffset.UtcNow;

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
        await AssertOptimisticConcurrencyAsync(createContext, sessionId, now);
        await AssertRestrictedDomainProfileDeletionAsync(createContext);
        await AssertRestrictedRefreshSessionDeletionAsync(createContext, now);
        await AssertRollbackAndReapplyAsync(createContext);
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

    private static async Task AssertRestrictedDomainProfileDeletionAsync(
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
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static async Task AssertRestrictedRefreshSessionDeletionAsync(
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
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
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
}
