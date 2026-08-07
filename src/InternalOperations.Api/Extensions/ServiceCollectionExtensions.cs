using System.Text;
using System.Threading.RateLimiting;
using InternalOperations.Api.Authentication;
using InternalOperations.Application;
using InternalOperations.Application.Abstractions.Authentication;
using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Application.Abstractions.Services;
using InternalOperations.Application.Common.Authorization;
using InternalOperations.Application.Features.Departments;
using InternalOperations.Application.Features.Tickets;
using InternalOperations.Application.Features.Users;
using InternalOperations.Application.Services;
using InternalOperations.Infrastructure;
using InternalOperations.Infrastructure.Authentication;
using InternalOperations.Persistence;
using InternalOperations.Persistence.Authentication;
using InternalOperations.Persistence.Context;
using InternalOperations.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace InternalOperations.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly); cfg.AddOpenBehavior(typeof(ValidationBehavior<,>)); });
        services.AddScoped<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, CurrentUserAccessor>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IRequestValidator<CreateDepartmentCommand>, CreateDepartmentCommandValidator>();
        services.AddScoped<IRequestValidator<ListDepartmentsQuery>, ListDepartmentsQueryValidator>();
        services.AddScoped<IRequestValidator<UpdateDepartmentCommand>, UpdateDepartmentCommandValidator>();
        services.AddScoped<IRequestValidator<SetDepartmentStatusCommand>, SetDepartmentStatusCommandValidator>();
        services.AddScoped<IRequestValidator<CreateUserCommand>, CreateUserCommandValidator>();
        services.AddScoped<IRequestValidator<ListUsersQuery>, ListUsersQueryValidator>();
        services.AddScoped<IRequestValidator<UpdateUserCommand>, UpdateUserCommandValidator>();
        services.AddScoped<IRequestValidator<SetUserDepartmentCommand>, SetUserDepartmentCommandValidator>();
        services.AddScoped<IRequestValidator<SetUserStatusCommand>, SetUserStatusCommandValidator>();
        services.AddScoped<IRequestValidator<SetUserRolesCommand>, SetUserRolesCommandValidator>();
        services.AddScoped<IRequestValidator<CreateTicketCommand>, CreateTicketCommandValidator>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        return services;
    }

    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "SqlServer";
        var connectionString = configuration.GetConnectionString(provider) ?? configuration["ConnectionStrings:DefaultConnection"];
        services.AddHttpContextAccessor();
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString, database => database.MigrationsAssembly(MigrationAssemblyNames.PostgreSql));
            }
            else if (string.IsNullOrWhiteSpace(connectionString)) options.UseInMemoryDatabase("InternalOperations-Development");
            else
            {
                options.UseSqlServer(connectionString, database => database.MigrationsAssembly(MigrationAssemblyNames.SqlServer));
            }
        });
        var lockout = configuration.GetSection("Authentication:Lockout").Get<AuthenticationLockoutOptions>() ?? new AuthenticationLockoutOptions();
        services.AddOptions<AuthenticationLockoutOptions>().Bind(configuration.GetSection("Authentication:Lockout"))
            .Validate(x => x.IsValid(), "Authentication lockout settings are invalid.").ValidateOnStart();
        services.AddIdentityCore<IdentityAccount>(options =>
        {
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = lockout.MaxFailedAccessAttempts;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(lockout.Minutes);
            options.User.RequireUniqueEmail = false;
        }).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<IIdentityAuthenticationService, IdentityAuthenticationService>();
        services.AddScoped<IRefreshTokenSessionRepository, RefreshTokenSessionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IDepartmentReadService, DepartmentReadService>();
        services.AddScoped<IUserAdministrationService, UserAdministrationService>();
        services.AddScoped<ITicketAdministrationService, TicketAdministrationService>();
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        return services;
    }

    public static IServiceCollection AddIdentityAccess(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(x => x.IsValid(), "JWT issuer, audience, 256-bit key, lifetime and clock skew must be securely configured.").ValidateOnStart();
        services.AddOptions<RefreshTokenOptions>().Bind(configuration.GetSection("Authentication:RefreshToken"))
            .Validate(x => x.IsValid(), "Refresh token lifetime is invalid.").ValidateOnStart();
        services.AddSingleton<IAuthenticationSessionSettings>(provider => provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RefreshTokenOptions>>().Value);
        services.AddOptions<SeedOptions>().Bind(configuration.GetSection("Authentication:Seed"));
        services.AddSingleton<DevelopmentIdentitySeeder>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsJsonAsync(new { type = "about:blank", title = "Authentication is required.", status = 401, code = "auth.unauthenticated" });
                },
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsJsonAsync(new { type = "about:blank", title = "Access is forbidden.", status = 403, code = "auth.forbidden" });
                },
            };
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                NameClaimType = "name",
                RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            };
        });
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            foreach (var policy in AuthorizationPolicies.All)
            {
                var roles = ApplicationRoles.All.Where(role => AuthorizationPolicies.ForRole(role).Contains(policy, StringComparer.Ordinal)).ToArray();
                options.AddPolicy(policy, builder => builder.RequireRole(roles));
            }
        });
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsJsonAsync(new { type = "about:blank", title = "Too many requests.", status = 429, code = "auth.rate_limit_exceeded" }, cancellationToken);
            };
            options.AddPolicy("auth-login", http => RateLimitPartition.GetFixedWindowLimiter(Normalize(http.Connection.RemoteIpAddress?.ToString()), _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.AddPolicy("auth-refresh", http => RateLimitPartition.GetFixedWindowLimiter(Normalize(http.Connection.RemoteIpAddress?.ToString()), _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        return services;
    }

    private static string Normalize(string? address)
    {
        var normalized = string.IsNullOrWhiteSpace(address) ? "unknown" : address.Trim().ToLowerInvariant();
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
