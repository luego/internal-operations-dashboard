namespace InternalOperations.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int ClockSkewSeconds { get; set; } = 30;
    public bool IsValid()
        => !string.IsNullOrWhiteSpace(Issuer)
            && !string.IsNullOrWhiteSpace(Audience)
            && System.Text.Encoding.UTF8.GetByteCount(SigningKey) >= 32
            && SigningKey.Distinct().Count() >= 16
            && AccessTokenMinutes == 15
            && ClockSkewSeconds is >= 0 and <= 30;
}

public sealed class SeedOptions
{
    public bool Enabled { get; set; }
    public string AdministratorIdentifier { get; set; } = string.Empty;
    public string AdministratorPassword { get; set; } = string.Empty;
    public string AdministratorDisplayName { get; set; } = "Administrator";
}

public sealed class RefreshTokenOptions : InternalOperations.Application.Abstractions.Authentication.IAuthenticationSessionSettings
{
    public int Days { get; set; } = 7;
    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(Days);
    public bool IsValid() => Days == 7;
}

public sealed class AuthenticationLockoutOptions
{
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int Minutes { get; set; } = 15;
    public bool IsValid() => MaxFailedAccessAttempts == 5 && Minutes == 15;
}
