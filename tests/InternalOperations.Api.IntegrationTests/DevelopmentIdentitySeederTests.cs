using InternalOperations.Api.Authentication;
using InternalOperations.Application.Common.Authorization;
using InternalOperations.Domain.Users;
using InternalOperations.Infrastructure.Authentication;
using InternalOperations.Persistence.Authentication;
using InternalOperations.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InternalOperations.Api.IntegrationTests;

public sealed class DevelopmentIdentitySeederTests
{
    private static readonly SeedOptions ValidSeed = new()
    {
        Enabled = true,
        AdministratorIdentifier = "development-admin@example.test",
        AdministratorPassword = "Development-only-Password1!",
        AdministratorDisplayName = "Development Administrator",
    };

    [Fact]
    public async Task DisabledSeedCreatesNothing()
    {
        await using var provider = CreateProvider();
        var seeder = CreateSeeder(provider, Environments.Development, new SeedOptions { Enabled = false });

        await seeder.SeedAsync();

        await AssertEmptyAsync(provider);
    }

    [Fact]
    public async Task SeedOutsideDevelopmentCreatesNothing()
    {
        await using var provider = CreateProvider();
        var seeder = CreateSeeder(provider, Environments.Production, ValidSeed);

        await seeder.SeedAsync();

        await AssertEmptyAsync(provider);
    }

    [Theory]
    [InlineData("", "Development-only-Password1!")]
    [InlineData("development-admin@example.test", "")]
    public async Task EnabledSeedRequiresExternalCredentials(string identifier, string password)
    {
        await using var provider = CreateProvider();
        var options = new SeedOptions { Enabled = true, AdministratorIdentifier = identifier, AdministratorPassword = password };
        var seeder = CreateSeeder(provider, Environments.Development, options);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync());

        if (identifier.Length > 0) Assert.DoesNotContain(identifier, exception.Message, StringComparison.Ordinal);
        if (password.Length > 0) Assert.DoesNotContain(password, exception.Message, StringComparison.Ordinal);
        await AssertEmptyAsync(provider);
    }

    [Fact]
    public async Task FirstAndRepeatedRunsCreateOneAccountProfileRoleMembershipAndEachRole()
    {
        await using var provider = CreateProvider();
        var seeder = CreateSeeder(provider, Environments.Development, ValidSeed);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var accounts = await context.Users.ToListAsync();
        var profiles = await context.DomainUsers.ToListAsync();
        var roles = await context.Roles.ToListAsync();
        var memberships = await context.UserRoles.ToListAsync();
        var account = Assert.Single(accounts);
        var profile = Assert.Single(profiles);
        Assert.Equal(account.Id, profile.Id);
        Assert.Equal(ValidSeed.AdministratorIdentifier, profile.UserName);
        Assert.Equal(ApplicationRoles.All.Count, roles.Count);
        Assert.Equal(ApplicationRoles.All.Count, roles.Select(role => role.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.Single(memberships);
        Assert.Equal(account.Id, memberships[0].UserId);
        Assert.Equal(roles.Single(role => role.Name == ApplicationRoles.Administrator).Id, memberships[0].RoleId);
    }

    [Fact]
    public async Task RepeatedRunReconcilesMissingProfileWithoutDuplicatingAccountOrRoles()
    {
        await using var provider = CreateProvider();
        var seeder = CreateSeeder(provider, Environments.Development, ValidSeed);
        await seeder.SeedAsync();
        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.DomainUsers.Remove(await context.DomainUsers.SingleAsync());
            await context.SaveChangesAsync();
        }

        await seeder.SeedAsync();

        await using var verificationScope = provider.CreateAsyncScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(await verification.Users.ToListAsync());
        var profile = Assert.Single(await verification.DomainUsers.ToListAsync());
        Assert.Equal((await verification.Users.SingleAsync()).Id, profile.Id);
        Assert.Equal(ApplicationRoles.All.Count, await verification.Roles.CountAsync());
        Assert.Single(await verification.UserRoles.ToListAsync());
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<IdentityAccount>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        return services.BuildServiceProvider();
    }

    private static DevelopmentIdentitySeeder CreateSeeder(IServiceProvider provider, string environment, SeedOptions options)
        => new(provider, new TestHostEnvironment { EnvironmentName = environment }, Options.Create(options));

    private static async Task AssertEmptyAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await context.Users.ToListAsync());
        Assert.Empty(await context.DomainUsers.ToListAsync());
        Assert.Empty(await context.Roles.ToListAsync());
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
