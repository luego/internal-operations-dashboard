using InternalOperations.Application.Common.Authorization;
using InternalOperations.Domain.Users;
using InternalOperations.Infrastructure.Authentication;
using InternalOperations.Persistence.Authentication;
using InternalOperations.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace InternalOperations.Api.Authentication;

public sealed class DevelopmentIdentitySeeder(IServiceProvider services, IHostEnvironment environment, IOptions<SeedOptions> options)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var seed = options.Value;
        if (!environment.IsDevelopment() || !seed.Enabled) return;
        if (string.IsNullOrWhiteSpace(seed.AdministratorIdentifier) || string.IsNullOrWhiteSpace(seed.AdministratorPassword))
            throw new InvalidOperationException("Development identity seed is enabled but external administrator credentials are missing.");
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using IDbContextTransaction? transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var roleName in ApplicationRoles.All)
            if (!await roles.RoleExistsAsync(roleName)) Ensure(await roles.CreateAsync(new IdentityRole<Guid>(roleName)));
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var account = await users.FindByNameAsync(seed.AdministratorIdentifier) ?? await users.FindByEmailAsync(seed.AdministratorIdentifier);
        if (account is null)
        {
            var id = Guid.NewGuid();
            account = new IdentityAccount { Id = id, UserName = seed.AdministratorIdentifier, Email = seed.AdministratorIdentifier.Contains('@', StringComparison.Ordinal) ? seed.AdministratorIdentifier : null, DisplayName = seed.AdministratorDisplayName, IsActive = true };
            Ensure(await users.CreateAsync(account, seed.AdministratorPassword));
        }
        var profile = await context.DomainUsers
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(user => user.Id == account.Id, cancellationToken);
        if (profile is null)
        {
            context.DomainUsers.Add(User.Create(account.Id, seed.AdministratorIdentifier, seed.AdministratorDisplayName));
            await context.SaveChangesAsync(cancellationToken);
        }
        else if (profile.IsDeleted)
        {
            profile.Restore();
            await context.SaveChangesAsync(cancellationToken);
        }
        if (!await users.IsInRoleAsync(account, ApplicationRoles.Administrator)) Ensure(await users.AddToRoleAsync(account, ApplicationRoles.Administrator));
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }
    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException("Identity seed operation failed: " + string.Join(", ", result.Errors.Select(x => x.Code)));
    }
}
