using InternalOperations.Persistence.Authentication;
using InternalOperations.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternalOperations.Persistence.IntegrationTests;

public sealed class IdentityAuthenticationServiceTests
{
    [Fact]
    public async Task FiveWrongPasswordsLockAccountAndSuccessResetsCounter()
    {
        await using var provider = Services();
        await using var scope = provider.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var account = Account();
        Assert.True((await users.CreateAsync(account, "Valid-password-123!")).Succeeded);
        var service = new IdentityAuthenticationService(users);

        for (var attempt = 0; attempt < 5; attempt++)
            Assert.False((await service.AuthenticateAsync(account.UserName!, "wrong", default)).IsSuccess);

        Assert.True(await users.IsLockedOutAsync(account));
        await users.SetLockoutEndDateAsync(account, DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.True((await service.AuthenticateAsync(account.UserName!, "Valid-password-123!", default)).IsSuccess);
        Assert.Equal(0, await users.GetAccessFailedCountAsync(account));
    }

    [Fact]
    public async Task InactiveAndUnknownAccountsReturnIdenticalPublicError()
    {
        await using var provider = Services();
        await using var scope = provider.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var account = Account();
        account.IsActive = false;
        Assert.True((await users.CreateAsync(account, "Valid-password-123!")).Succeeded);
        var service = new IdentityAuthenticationService(users);

        var inactive = await service.AuthenticateAsync(account.UserName!, "Valid-password-123!", default);
        var unknown = await service.AuthenticateAsync("unknown", "Valid-password-123!", default);

        Assert.Equal("auth.invalid_credentials", inactive.Error!.Code);
        Assert.Equal(inactive.Error, unknown.Error);
    }

    private static ServiceProvider Services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<IdentityAccount>(options =>
        {
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<ApplicationDbContext>();
        return services.BuildServiceProvider();
    }

    private static IdentityAccount Account() => new() { Id = Guid.NewGuid(), UserName = "agent", DisplayName = "Agent", IsActive = true };
}
