using InternalOperations.Application.Abstractions.Authentication;
using InternalOperations.Application.Abstractions.Persistence;

namespace InternalOperations.Application.Features.Auth;

public sealed record TokenPairResult(string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc, string RefreshToken, DateTimeOffset RefreshTokenExpiresAtUtc, string TokenType = "Bearer");
public sealed record LoginCommand(string Identifier, string Password, string? DeviceDescription) : IRequest<Result<TokenPairResult>>;
public sealed record RefreshSessionCommand(string RefreshToken, string? DeviceDescription) : IRequest<Result<TokenPairResult>>;
public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;

public sealed class LoginCommandHandler(IIdentityAuthenticationService accounts, IAccessTokenIssuer accessTokens, IRefreshTokenGenerator refreshTokens, IRefreshTokenSessionRepository sessions, IUnitOfWork unitOfWork, IClock clock, IAuthenticationSessionSettings settings) : IRequestHandler<LoginCommand, Result<TokenPairResult>>
{
    public async Task<Result<TokenPairResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Password))
            return Result<TokenPairResult>.Failure(Error.Validation("auth.invalid_request", "Identifier and password are required."));
        var authenticated = await accounts.AuthenticateAsync(request.Identifier, request.Password, cancellationToken);
        if (!authenticated.IsSuccess) return Result<TokenPairResult>.Failure(AuthErrors.InvalidCredentials);
        var now = clock.UtcNow;
        var generated = refreshTokens.Generate();
        var session = new RefreshTokenSession(Guid.NewGuid(), authenticated.Value!.Id, Guid.NewGuid(), generated.Hash, now, now.Add(settings.RefreshTokenLifetime), Limit(request.DeviceDescription));
        await sessions.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Pair(authenticated.Value, generated, session, accessTokens, now);
    }
    private static string? Limit(string? value) => value is { Length: > 200 } ? value[..200] : value;
    internal static Result<TokenPairResult> Pair(AuthenticatedAccount account, GeneratedRefreshToken refresh, RefreshTokenSession session, IAccessTokenIssuer issuer, DateTimeOffset now)
    { var access = issuer.Issue(account, now); return Result<TokenPairResult>.Success(new(access.Token, access.ExpiresAtUtc, refresh.Plaintext, session.ExpiresAtUtc)); }
}

public sealed class RefreshSessionCommandHandler(IIdentityAuthenticationService accounts, IAccessTokenIssuer accessTokens, IRefreshTokenGenerator refreshTokens, IRefreshTokenSessionRepository sessions, IUnitOfWork unitOfWork, IClock clock, IAuthenticationSessionSettings settings) : IRequestHandler<RefreshSessionCommand, Result<TokenPairResult>>
{
    public async Task<Result<TokenPairResult>> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken)) return Invalid();
        var now = clock.UtcNow;
        var old = await sessions.GetByHashAsync(refreshTokens.Hash(request.RefreshToken), cancellationToken);
        if (old is null) return Invalid();
        if (old.ReplacedByTokenId is not null)
        {
            await sessions.RevokeFamilyAsync(old.FamilyId, now, cancellationToken);
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (PersistenceConcurrencyException)
            {
                // A concurrent refresh or replay won the race; never issue from this token.
            }
            return Invalid();
        }
        if (old.IsExpired(now) || old.RevokedAtUtc is not null) return Invalid();
        var account = await accounts.GetActiveAccountAsync(old.UserId, cancellationToken);
        if (!account.IsSuccess) return Invalid();
        var generated = refreshTokens.Generate();
        var replacement = new RefreshTokenSession(Guid.NewGuid(), old.UserId, old.FamilyId, generated.Hash, now, now.Add(settings.RefreshTokenLifetime), Limit(request.DeviceDescription));
        old.Revoke(now, replacement.Id);
        await sessions.AddAsync(replacement, cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConcurrencyException)
        {
            return Invalid();
        }
        return LoginCommandHandler.Pair(account.Value!, generated, replacement, accessTokens, now);
    }
    private static Result<TokenPairResult> Invalid() => Result<TokenPairResult>.Failure(AuthErrors.InvalidRefreshToken);
    private static string? Limit(string? value) => value is { Length: > 200 } ? value[..200] : value;
}

public sealed class LogoutCommandHandler(IRefreshTokenGenerator refreshTokens, IRefreshTokenSessionRepository sessions, IUnitOfWork unitOfWork, IClock clock) : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken)) return Result.Success();
        var session = await sessions.GetByHashAsync(refreshTokens.Hash(request.RefreshToken), cancellationToken);
        if (session is null || session.RevokedAtUtc is not null) return Result.Success();
        session.Revoke(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public static class AuthErrors
{
    public static Error InvalidCredentials { get; } = Error.Unauthorized("auth.invalid_credentials", "Invalid credentials.");
    public static Error InvalidRefreshToken { get; } = Error.Unauthorized("auth.invalid_refresh_token", "Invalid refresh token.");
}
