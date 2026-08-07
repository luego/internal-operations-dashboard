using System.Data;
using InternalOperations.Application;
using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Application.Common.Authorization;
using InternalOperations.Application.Features.Users;
using InternalOperations.Domain.Users;
using InternalOperations.Persistence.Authentication;
using InternalOperations.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace InternalOperations.Persistence.Repositories;

public sealed class UserAdministrationService(
    ApplicationDbContext context,
    UserManager<IdentityAccount> accounts,
    RoleManager<IdentityRole<Guid>> roles,
    IClock clock,
    ICurrentUser currentUser) : IUserAdministrationService
{
    public async Task<Result<UserDto>> CreateAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var departmentResult = await ValidateDepartmentAsync(command.DepartmentId, cancellationToken);
        if (!departmentResult.IsSuccess) return Result<UserDto>.Failure(departmentResult.Error!);

        if (await IdentifierExistsAsync(command.UserName, command.Email, null, cancellationToken))
            return Result<UserDto>.Failure(UserErrors.IdentifierConflict);
        foreach (var role in command.Roles)
        {
            if (!await roles.RoleExistsAsync(role))
                return Result<UserDto>.Failure(UserErrors.InvalidRoles);
        }

        try
        {
            var id = Guid.NewGuid();
            var account = new IdentityAccount
            {
                Id = id,
                UserName = command.UserName.Trim(),
                Email = command.Email.Trim(),
                DisplayName = command.DisplayName.Trim(),
                IsActive = true,
            };
            var accountResult = await accounts.CreateAsync(account, command.InitialPassword);
            if (!accountResult.Succeeded)
                return await RollbackFailureAsync(transaction, MapIdentityFailure(accountResult), cancellationToken);

            var profile = User.Create(id, command.UserName, command.DisplayName, command.DepartmentId, clock.UtcNow.UtcDateTime);
            await context.DomainUsers.AddAsync(profile, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var roleResult = await accounts.AddToRolesAsync(account, command.Roles);
            if (!roleResult.Succeeded)
                return await RollbackFailureAsync(transaction, MapIdentityFailure(roleResult), cancellationToken);

            await CommitAsync(transaction, cancellationToken);
            return Result<UserDto>.Success(await BuildDtoAsync(profile, account, cancellationToken));
        }
        catch (DbUpdateException)
        {
            return await RollbackFailureAsync(transaction, UserErrors.IdentifierConflict, cancellationToken);
        }
        catch
        {
            await RollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task<UserDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await (from profile in context.DomainUsers.AsNoTracking()
                         join account in context.Users.AsNoTracking() on profile.Id equals account.Id
                         where profile.Id == id
                         select new UserRow(profile, account, profile.Department == null ? null : new DepartmentSummaryDto(profile.Department.Id, profile.Department.Name)))
            .SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : await BuildDtoAsync(row.Profile, row.Account, cancellationToken, row.Department);
    }

    public async Task<UserPage> ListAsync(UserListFilter filter, CancellationToken cancellationToken)
    {
        var query = from profile in context.DomainUsers.AsNoTracking()
                    join account in context.Users.AsNoTracking() on profile.Id equals account.Id
                    select new UserRow(profile, account, profile.Department == null ? null : new DepartmentSummaryDto(profile.Department.Id, profile.Department.Name));

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().Normalize(System.Text.NormalizationForm.FormKC).ToUpperInvariant();
            var displaySearch = $"%{filter.Search.Trim()}%";
            query = query.Where(row =>
                (row.Account.NormalizedUserName != null && row.Account.NormalizedUserName.Contains(search))
                || (row.Account.NormalizedEmail != null && row.Account.NormalizedEmail.Contains(search))
                || EF.Functions.Like(row.Profile.DisplayName, displaySearch));
        }
        if (filter.IsActive.HasValue) query = query.Where(row => row.Profile.IsActive == filter.IsActive.Value);
        if (filter.DepartmentId.HasValue) query = query.Where(row => row.Profile.DepartmentId == filter.DepartmentId.Value);
        if (filter.HasDepartment.HasValue) query = filter.HasDepartment.Value
            ? query.Where(row => row.Profile.DepartmentId != null)
            : query.Where(row => row.Profile.DepartmentId == null);
        if (filter.Role is not null)
        {
            var roleId = await context.Roles.Where(role => role.Name == filter.Role).Select(role => (Guid?)role.Id).SingleOrDefaultAsync(cancellationToken);
            query = roleId.HasValue
                ? query.Where(row => context.UserRoles.Any(userRole => userRole.UserId == row.Profile.Id && userRole.RoleId == roleId.Value))
                : query.Where(_ => false);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var descending = filter.SortDirection == "desc";
        query = filter.SortBy.ToLowerInvariant() switch
        {
            "displayname" => descending ? query.OrderByDescending(x => x.Profile.DisplayName).ThenBy(x => x.Profile.Id) : query.OrderBy(x => x.Profile.DisplayName).ThenBy(x => x.Profile.Id),
            "email" => descending ? query.OrderByDescending(x => x.Account.NormalizedEmail).ThenBy(x => x.Profile.Id) : query.OrderBy(x => x.Account.NormalizedEmail).ThenBy(x => x.Profile.Id),
            "createdatutc" => descending ? query.OrderByDescending(x => x.Profile.CreatedAtUtc).ThenBy(x => x.Profile.Id) : query.OrderBy(x => x.Profile.CreatedAtUtc).ThenBy(x => x.Profile.Id),
            "updatedatutc" => descending ? query.OrderByDescending(x => x.Profile.UpdatedAtUtc).ThenBy(x => x.Profile.Id) : query.OrderBy(x => x.Profile.UpdatedAtUtc).ThenBy(x => x.Profile.Id),
            _ => descending ? query.OrderByDescending(x => x.Account.NormalizedUserName).ThenBy(x => x.Profile.Id) : query.OrderBy(x => x.Account.NormalizedUserName).ThenBy(x => x.Profile.Id),
        };

        var rows = await query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(cancellationToken);
        var ids = rows.Select(row => row.Profile.Id).ToArray();
        var roleRows = await (from userRole in context.UserRoles
                              join role in context.Roles on userRole.RoleId equals role.Id
                              where ids.Contains(userRole.UserId)
                              select new { userRole.UserId, role.Name }).ToListAsync(cancellationToken);
        var roleLookup = roleRows.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Select(y => y.Name!).Order().ToArray());
        var items = rows.Select(row => ToDto(row.Profile, row.Account, row.Department, roleLookup.GetValueOrDefault(row.Profile.Id, []))).ToArray();
        return new UserPage(items, filter.Page, filter.PageSize, totalCount);
    }

    public async Task<Result<UserDto>> UpdateAsync(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var pair = await GetTrackedPairAsync(command.Id, cancellationToken);
        if (pair is null) return Result<UserDto>.Failure(UserErrors.NotFound);
        if (pair.Profile.Version != command.Version) return Result<UserDto>.Failure(UserErrors.VersionConflict);
        if (await IdentifierExistsAsync(command.UserName, command.Email, command.Id, cancellationToken)) return Result<UserDto>.Failure(UserErrors.IdentifierConflict);

        try
        {
            pair.Profile.UpdateProfile(command.UserName, command.DisplayName, clock.UtcNow.UtcDateTime);
            pair.Account.UserName = command.UserName.Trim();
            pair.Account.Email = command.Email.Trim();
            pair.Account.DisplayName = pair.Profile.DisplayName;
            var result = await accounts.UpdateAsync(pair.Account);
            if (!result.Succeeded) return await RollbackFailureAsync(transaction, MapIdentityFailure(result), cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return Result<UserDto>.Success(await BuildDtoAsync(pair.Profile, pair.Account, cancellationToken));
        }
        catch (DbUpdateConcurrencyException) { return await RollbackFailureAsync(transaction, UserErrors.VersionConflict, cancellationToken); }
        catch (DbUpdateException) { return await RollbackFailureAsync(transaction, UserErrors.IdentifierConflict, cancellationToken); }
    }

    public async Task<Result<UserDto>> SetDepartmentAsync(SetUserDepartmentCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var pair = await GetTrackedPairAsync(command.Id, cancellationToken);
        if (pair is null) return Result<UserDto>.Failure(UserErrors.NotFound);
        if (pair.Profile.Version != command.Version) return Result<UserDto>.Failure(UserErrors.VersionConflict);
        if (command.DepartmentId.HasValue && !pair.Profile.IsActive) return Result<UserDto>.Failure(UserErrors.Inactive);
        var department = await ValidateDepartmentAsync(command.DepartmentId, cancellationToken);
        if (!department.IsSuccess) return Result<UserDto>.Failure(department.Error!);

        if (command.DepartmentId.HasValue) pair.Profile.AssignDepartment(command.DepartmentId.Value, clock.UtcNow.UtcDateTime);
        else pair.Profile.RemoveDepartment(clock.UtcNow.UtcDateTime);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return await RollbackFailureAsync(transaction, UserErrors.VersionConflict, cancellationToken);
        }
        return Result<UserDto>.Success(await BuildDtoAsync(pair.Profile, pair.Account, cancellationToken));
    }

    public async Task<Result<UserDto>> SetStatusAsync(SetUserStatusCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var pair = await GetTrackedPairAsync(command.Id, cancellationToken);
        if (pair is null) return Result<UserDto>.Failure(UserErrors.NotFound);
        if (pair.Profile.Version != command.Version) return Result<UserDto>.Failure(UserErrors.VersionConflict);
        if (pair.Profile.IsActive == command.IsActive) return Result<UserDto>.Success(await BuildDtoAsync(pair.Profile, pair.Account, cancellationToken));
        if (!command.IsActive && currentUser.UserId == command.Id) return Result<UserDto>.Failure(UserErrors.SelfManagementConflict);
        var currentRoles = await accounts.GetRolesAsync(pair.Account);
        if (!command.IsActive && currentRoles.Contains(ApplicationRoles.Administrator, StringComparer.Ordinal)
            && await CountActiveAdministratorsAsync(cancellationToken) <= 1)
            return Result<UserDto>.Failure(UserErrors.LastAdministrator);
        if (command.IsActive)
        {
            if (currentRoles.Count == 0 || currentRoles.Any(role => !ApplicationRoles.All.Contains(role, StringComparer.Ordinal)))
                return Result<UserDto>.Failure(UserErrors.InvalidRoles);
            var department = await ValidateDepartmentAsync(pair.Profile.DepartmentId, cancellationToken);
            if (!department.IsSuccess) return Result<UserDto>.Failure(department.Error!);
            pair.Profile.Activate(clock.UtcNow.UtcDateTime);
        }
        else pair.Profile.Deactivate(clock.UtcNow.UtcDateTime);
        pair.Account.IsActive = command.IsActive;
        if (!command.IsActive) await RevokeSessionsAsync(command.Id, cancellationToken);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return await RollbackFailureAsync(transaction, UserErrors.VersionConflict, cancellationToken);
        }
        return Result<UserDto>.Success(await BuildDtoAsync(pair.Profile, pair.Account, cancellationToken));
    }

    public async Task<Result<UserDto>> SetRolesAsync(SetUserRolesCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var pair = await GetTrackedPairAsync(command.Id, cancellationToken);
        if (pair is null) return Result<UserDto>.Failure(UserErrors.NotFound);
        if (pair.Profile.Version != command.Version) return Result<UserDto>.Failure(UserErrors.VersionConflict);
        var existing = await accounts.GetRolesAsync(pair.Account);
        if (currentUser.UserId == command.Id && existing.Contains(ApplicationRoles.Administrator) && !command.Roles.Contains(ApplicationRoles.Administrator))
            return Result<UserDto>.Failure(UserErrors.SelfManagementConflict);
        if (existing.Contains(ApplicationRoles.Administrator) && !command.Roles.Contains(ApplicationRoles.Administrator)
            && pair.Profile.IsActive && await CountActiveAdministratorsAsync(cancellationToken) <= 1)
            return Result<UserDto>.Failure(UserErrors.LastAdministrator);

        try
        {
            pair.Profile.RecordAdministrativeChange(clock.UtcNow.UtcDateTime);
            var removed = existing.Except(command.Roles, StringComparer.Ordinal).ToArray();
            var added = command.Roles.Except(existing, StringComparer.Ordinal).ToArray();
            if (removed.Length > 0)
            {
                var remove = await accounts.RemoveFromRolesAsync(pair.Account, removed);
                if (!remove.Succeeded) return await RollbackFailureAsync(transaction, MapIdentityFailure(remove), cancellationToken);
                await RevokeSessionsAsync(command.Id, cancellationToken);
            }
            if (added.Length > 0)
            {
                var add = await accounts.AddToRolesAsync(pair.Account, added);
                if (!add.Succeeded) return await RollbackFailureAsync(transaction, MapIdentityFailure(add), cancellationToken);
            }
            await context.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return Result<UserDto>.Success(await BuildDtoAsync(pair.Profile, pair.Account, cancellationToken));
        }
        catch (DbUpdateConcurrencyException)
        {
            return await RollbackFailureAsync(transaction, UserErrors.VersionConflict, cancellationToken);
        }
    }

    private async Task<Result> ValidateDepartmentAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        if (!departmentId.HasValue) return Result.Success();
        var department = await context.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == departmentId.Value, cancellationToken);
        if (department is null) return Result.Failure(UserErrors.DepartmentNotFound);
        return department.IsActive ? Result.Success() : Result.Failure(UserErrors.DepartmentInactive);
    }

    private async Task<bool> IdentifierExistsAsync(string userName, string email, Guid? excludingId, CancellationToken cancellationToken)
    {
        var normalizedName = accounts.NormalizeName(userName);
        var normalizedEmail = accounts.NormalizeEmail(email);
        return await context.Users.AnyAsync(x => (!excludingId.HasValue || x.Id != excludingId.Value)
            && (x.NormalizedUserName == normalizedName || x.NormalizedEmail == normalizedEmail), cancellationToken);
    }

    private async Task<UserPair?> GetTrackedPairAsync(Guid id, CancellationToken cancellationToken)
    {
        var profile = await context.DomainUsers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        var account = await context.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return profile is null || account is null ? null : new(profile, account);
    }

    private async Task<int> CountActiveAdministratorsAsync(CancellationToken cancellationToken)
    {
        var roleId = await context.Roles.Where(x => x.Name == ApplicationRoles.Administrator).Select(x => x.Id).SingleAsync(cancellationToken);
        return await (from userRole in context.UserRoles
                      join profile in context.DomainUsers on userRole.UserId equals profile.Id
                      where userRole.RoleId == roleId && profile.IsActive
                      select profile.Id).CountAsync(cancellationToken);
    }

    private async Task RevokeSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var sessions = await context.RefreshTokenSessions.Where(x => x.UserId == userId && x.RevokedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var session in sessions) session.Revoke(now);
    }

    private async Task<UserDto> BuildDtoAsync(User profile, IdentityAccount account, CancellationToken cancellationToken, DepartmentSummaryDto? department = null)
    {
        if (department is null && profile.DepartmentId.HasValue)
            department = await context.Departments.AsNoTracking().Where(x => x.Id == profile.DepartmentId).Select(x => new DepartmentSummaryDto(x.Id, x.Name)).SingleOrDefaultAsync(cancellationToken);
        return ToDto(profile, account, department, (await accounts.GetRolesAsync(account)).Order().ToArray());
    }

    private static UserDto ToDto(User profile, IdentityAccount account, DepartmentSummaryDto? department, IReadOnlyList<string> assignedRoles) =>
        new(profile.Id, profile.UserName, account.Email ?? string.Empty, profile.DisplayName, profile.IsActive, department, assignedRoles, profile.CreatedAtUtc, profile.UpdatedAtUtc, profile.Version);

    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken) =>
        context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
    private static Task CommitAsync(IDbContextTransaction? transaction, CancellationToken cancellationToken) => transaction?.CommitAsync(cancellationToken) ?? Task.CompletedTask;
    private async Task RollbackAsync(IDbContextTransaction? transaction, CancellationToken cancellationToken)
    {
        if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
        context.ChangeTracker.Clear();
    }
    private async Task<Result<UserDto>> RollbackFailureAsync(IDbContextTransaction? transaction, Error error, CancellationToken cancellationToken)
    {
        await RollbackAsync(transaction, cancellationToken);
        return Result<UserDto>.Failure(error);
    }
    private static Error MapIdentityFailure(IdentityResult result) =>
        result.Errors.Any(x => x.Code.Contains("Password", StringComparison.OrdinalIgnoreCase)) ? UserErrors.PasswordRequirementsNotMet : UserErrors.IdentifierConflict;

    private sealed record UserPair(User Profile, IdentityAccount Account);
    private sealed record UserRow(User Profile, IdentityAccount Account, DepartmentSummaryDto? Department);
}
