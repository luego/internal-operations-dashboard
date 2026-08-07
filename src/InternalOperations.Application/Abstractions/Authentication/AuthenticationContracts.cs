namespace InternalOperations.Application.Abstractions.Authentication;

public sealed record AuthenticatedAccount(Guid Id, string DisplayName, IReadOnlyList<string> Roles);
public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAtUtc);
public sealed record GeneratedRefreshToken(string Plaintext, string Hash);

public interface IIdentityAuthenticationService
{
    Task<Result<AuthenticatedAccount>> AuthenticateAsync(string identifier, string password, CancellationToken cancellationToken);
    Task<Result<AuthenticatedAccount>> GetActiveAccountAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IAccessTokenIssuer
{
    AccessTokenResult Issue(AuthenticatedAccount account, DateTimeOffset now);
}

public interface IRefreshTokenGenerator
{
    GeneratedRefreshToken Generate();
    string Hash(string token);
}

public interface IAuthenticationSessionSettings
{
    TimeSpan RefreshTokenLifetime { get; }
}

public interface IRefreshTokenSessionRepository
{
    Task<RefreshTokenSession?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task AddAsync(RefreshTokenSession session, CancellationToken cancellationToken);
    Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken);
}

public class RefreshTokenSession(
    Guid id,
    Guid userId,
    Guid familyId,
    string tokenHash,
    DateTimeOffset createdAtUtc,
    DateTimeOffset expiresAtUtc,
    string? deviceDescription)
{
    public Guid Id { get; protected set; } = id;
    public Guid UserId { get; protected set; } = userId;
    public Guid FamilyId { get; protected set; } = familyId;
    public string TokenHash { get; protected set; } = tokenHash;
    public DateTimeOffset CreatedAtUtc { get; protected set; } = createdAtUtc;
    public DateTimeOffset ExpiresAtUtc { get; protected set; } = expiresAtUtc;
    public DateTimeOffset? RevokedAtUtc { get; protected set; }
    public Guid? ReplacedByTokenId { get; protected set; }
    public string? DeviceDescription { get; protected set; } = deviceDescription;
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;
    public virtual void Revoke(DateTimeOffset now, Guid? replacementId = null) { RevokedAtUtc = now; ReplacedByTokenId = replacementId; }
}
