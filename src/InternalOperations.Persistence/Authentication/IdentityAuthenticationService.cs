using InternalOperations.Application;
using InternalOperations.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.Authentication;

public sealed class IdentityAuthenticationService(UserManager<IdentityAccount> users) : IIdentityAuthenticationService
{
    public async Task<Result<AuthenticatedAccount>> AuthenticateAsync(string identifier, string password, CancellationToken cancellationToken)
    {
        var account = await FindAsync(identifier);
        if (account is null) return Invalid();
        if (!account.IsActive || account.IsDeleted || await users.IsLockedOutAsync(account)) return Invalid();
        if (!await users.CheckPasswordAsync(account, password))
        {
            await users.AccessFailedAsync(account);
            return Invalid();
        }
        await users.ResetAccessFailedCountAsync(account);
        return await SuccessAsync(account);
    }

    public async Task<Result<AuthenticatedAccount>> GetActiveAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await users.FindByIdAsync(userId.ToString());
        return account is null || !account.IsActive || account.IsDeleted || await users.IsLockedOutAsync(account) ? Invalid() : await SuccessAsync(account);
    }

    private async Task<IdentityAccount?> FindAsync(string identifier)
        => identifier.Contains('@', StringComparison.Ordinal) ? await users.FindByEmailAsync(identifier) : await users.FindByNameAsync(identifier);
    private async Task<Result<AuthenticatedAccount>> SuccessAsync(IdentityAccount account)
        => Result<AuthenticatedAccount>.Success(new(account.Id, account.DisplayName, (await users.GetRolesAsync(account)).ToArray()));
    private static Result<AuthenticatedAccount> Invalid()
        => Result<AuthenticatedAccount>.Failure(Error.Unauthorized("auth.invalid_credentials", "Invalid credentials."));
}

public sealed class RefreshTokenSessionRepository(Context.ApplicationDbContext context) : IRefreshTokenSessionRepository
{
    public async Task<RefreshTokenSession?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
        => await context.RefreshTokenSessions.SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    public async Task AddAsync(RefreshTokenSession session, CancellationToken cancellationToken)
        => await context.RefreshTokenSessions.AddAsync(session as RefreshTokenSessionEntity ?? new RefreshTokenSessionEntity(session), cancellationToken);
    public async Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = await context.RefreshTokenSessions.Where(x => x.FamilyId == familyId && x.RevokedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var session in active) session.Revoke(now);
    }
}
