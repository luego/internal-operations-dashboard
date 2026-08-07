using InternalOperations.Application;
using InternalOperations.Application.Features.Users;
using InternalOperations.Domain.Departments;
using InternalOperations.Domain.Users;
using InternalOperations.Persistence.Authentication;
using InternalOperations.Persistence.Context;
using InternalOperations.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternalOperations.Persistence.IntegrationTests;

public sealed class UserAdministrationServiceTests
{
    [Fact]
    public async Task CreatePersistsSharedIdentityAndDomainIdentifierAndReturnsSafeDto()
    {
        await using var provider = Services();
        await using var scope = provider.CreateAsyncScope();
        var service = Service(scope);

        var result = await service.CreateAsync(new(
            "agent.one", "agent@example.test", "Agent One", "Valid-password-123!", ["Agent"], null), default);

        Assert.True(result.IsSuccess, result.Error is null ? null : $"{result.Error.Code}: {result.Error.Message}");
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var account = await context.Users.IgnoreQueryFilters().SingleAsync();
        var profile = await context.DomainUsers.SingleAsync();
        Assert.Equal(account.Id, profile.Id);
        Assert.Equal(result.Value!.Id, profile.Id);
        Assert.Equal("agent@example.test", result.Value.Email);
        Assert.Equal(["Agent"], result.Value.Roles);
    }

    [Fact]
    public async Task CreateRejectsInactiveDepartmentWithoutPartialAccount()
    {
        await using var provider = Services();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var department = Department.Create("Closed", null);
        department.Deactivate(DateTime.UtcNow);
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var result = await Service(scope).CreateAsync(new(
            "agent.two", "two@example.test", "Agent Two", "Valid-password-123!", ["Agent"], department.Id), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("departments.inactive", result.Error!.Code);
        Assert.Empty(await context.Users.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await context.DomainUsers.ToListAsync());
    }

    [Fact]
    public async Task CreateMapsPasswordPolicyFailureWithoutPersistingPartialUser()
    {
        await using var provider = Services();
        await using var scope = provider.CreateAsyncScope();

        var result = await Service(scope).CreateAsync(new(
            "agent.weak", "weak@example.test", "Weak Agent", "weak", ["Agent"], null), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("users.password_requirements_not_met", result.Error!.Code);
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await context.Users.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await context.DomainUsers.ToListAsync());
    }

    [Fact]
    public async Task ReactivationRequiresAtLeastOneAssignedRole()
    {
        await using var provider = Services();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var id = Guid.NewGuid();
        var account = new IdentityAccount
        {
            Id = id,
            UserName = "inactive.agent",
            Email = "inactive@example.test",
            DisplayName = "Inactive Agent",
            IsActive = false,
        };
        Assert.True((await manager.CreateAsync(account, "Valid-password-123!")).Succeeded);
        var profile = User.Create(id, account.UserName, account.DisplayName, null, DateTime.UtcNow);
        profile.Deactivate(DateTime.UtcNow);
        context.DomainUsers.Add(profile);
        await context.SaveChangesAsync();

        var result = await Service(scope).SetStatusAsync(new(id, true, profile.Version), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("users.invalid_roles", result.Error!.Code);
        Assert.False(profile.IsActive);
        Assert.False(account.IsActive);
    }

    private static UserAdministrationService Service(AsyncServiceScope scope) => new(
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
        scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>(),
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
        new FixedClock(),
        new AnonymousCurrentUser());

    private static ServiceProvider Services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var databaseName = Guid.NewGuid().ToString();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<IdentityAccount>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        Assert.True(roles.CreateAsync(new IdentityRole<Guid>("Agent")).GetAwaiter().GetResult().Succeeded);
        Assert.True(roles.CreateAsync(new IdentityRole<Guid>("Viewer")).GetAwaiter().GetResult().Succeeded);
        Assert.True(roles.CreateAsync(new IdentityRole<Guid>("Manager")).GetAwaiter().GetResult().Succeeded);
        Assert.True(roles.CreateAsync(new IdentityRole<Guid>("Administrator")).GetAwaiter().GetResult().Succeeded);
        return provider;
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 8, 7, 21, 0, 0, TimeSpan.Zero);
    }

    private sealed class AnonymousCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public string? UserName => null;
    }
}
