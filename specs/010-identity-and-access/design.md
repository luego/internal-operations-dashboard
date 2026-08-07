# 010 — Identity and Access: Design

**Estado:** Implementing
**Requisitos:** `requirements.md`
**Fecha:** 6 de agosto de 2026
**Aprobada:** 6 de agosto de 2026
**Gate:** Aprobado por el usuario el 6 de agosto de 2026.

## 1. Resumen

La fase 2 agrega autenticación local mediante ASP.NET Core Identity, access tokens JWT y refresh tokens rotatorios. Application conserva los casos de uso y puertos; Persistence almacena cuentas, roles y sesiones mediante EF Core; Infrastructure firma tokens; API configura autenticación, autorización, rate limiting y contratos HTTP.

La cuenta técnica de Identity y el perfil de negocio `Domain.Users.User` son modelos distintos, con el mismo GUID como correlación. Identity conserva credenciales, normalización, lockout y roles. Domain conserva nombre visible, departamento, estado operativo y relaciones de negocio. Esto evita que Domain dependa de Identity y permite sustituir autenticación local por OIDC en el futuro.

## 2. Dirección de dependencias

```text
Api -> Application -> Domain
 |         \-> Shared
 |-> Infrastructure -> Application + Shared
 \-> Persistence -> Application + Domain + Shared
```

Reglas:

- Domain no conoce Identity, JWT, claims ni refresh tokens.
- Application define modelos y puertos neutrales de autenticación.
- Persistence implementa el store de Identity y sesiones con EF Core.
- Infrastructure implementa emisión/validación técnica del JWT.
- API es composition root y único lugar que configura esquemas/policies HTTP.
- No se agrega referencia Infrastructure ↔ Persistence.

## 3. Flujo de componentes

### Login

```text
POST /api/v1/auth/login
  -> AuthController
  -> LoginCommand
  -> LoginCommandHandler
  -> IIdentityAuthenticationService (Persistence adapter over UserManager)
  -> IAccessTokenIssuer (Infrastructure)
  -> IRefreshTokenGenerator (Infrastructure)
  -> IRefreshTokenSessionRepository + IUnitOfWork (Persistence)
  -> TokenPairResult
```

### Refresh

```text
POST /api/v1/auth/refresh
  -> RefreshSessionCommandHandler
  -> hash presented token
  -> IRefreshTokenSessionRepository.GetByHashForUpdate
  -> validate account + expiry + session state
  -> revoke old token and create replacement in same Unit of Work
  -> issue access token
  -> TokenPairResult
```

### Logout

```text
POST /api/v1/auth/logout
  -> LogoutCommandHandler
  -> lookup hash
  -> revoke when active
  -> commit if changed
  -> idempotent success
```

### Protected request

```text
Authorization: Bearer <jwt>
  -> JwtBearer authentication
  -> ASP.NET Core policy
  -> controller
  -> MediatR handler
  -> ICurrentUser for actor identity
```

## 4. Application contracts

Proposed namespaces and contracts:

```text
InternalOperations.Application/
├── Abstractions/
│   ├── Authentication/
│   │   ├── IIdentityAuthenticationService.cs
│   │   ├── IAccessTokenIssuer.cs
│   │   ├── IRefreshTokenGenerator.cs
│   │   └── IRefreshTokenSessionRepository.cs
│   └── Persistence/
│       └── IUnitOfWork.cs
├── Common/Authorization/
│   ├── AuthorizationPolicies.cs
│   └── ApplicationRoles.cs
└── Features/Auth/
    ├── Login/
    ├── RefreshSession/
    └── Logout/
```

Interfaces orientativas:

```csharp
public interface IIdentityAuthenticationService
{
    Task<Result<AuthenticatedAccount>> AuthenticateAsync(
        string identifier,
        string password,
        CancellationToken cancellationToken);

    Task<Result<AuthenticatedAccount>> GetActiveAccountAsync(
        Guid userId,
        CancellationToken cancellationToken);
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

public interface IRefreshTokenSessionRepository
{
    Task<RefreshTokenSession?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    Task AddAsync(
        RefreshTokenSession session,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RefreshTokenSession>> GetActiveFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken);
}
```

Application models contain GUIDs, roles, timestamps and opaque token values only where needed for returning them once. They do not expose `IdentityUser`, `IdentityRole`, `ClaimsPrincipal`, `SecurityToken` or EF types.

## 5. Persistence design

### DES-AUTH-001 Identity store

`ApplicationDbContext` will derive from the appropriate `IdentityDbContext` specialization using GUID keys. A persistence-only account type stores:

- `Id`;
- normalized username/email and Identity security fields;
- `IsActive`;
- lockout state inherited from Identity.

The domain `User` profile keeps the same `Id`. During phase 2, the Development seed creates both records atomically in the shared context. Phase `020` will define administrative lifecycle and profile fields.

Identity tables retain explicit provider-neutral names/configuration. Delete behavior is restrictive for user/session and user/domain relationships.

### DES-AUTH-003 Credential verification and lockout

A Persistence adapter wraps `UserManager`/`SignInManager` behavior behind `IIdentityAuthenticationService`. It:

- normalizes identifier lookup;
- produces one public invalid-credentials error for unknown, inactive, locked or wrong-password cases;
- enables lockout on failure;
- never returns password/hash/security-stamp values;
- returns an immutable `AuthenticatedAccount` containing ID, display name and roles.

### DES-AUTH-004 Refresh session model

```text
RefreshTokenSession
- Id: Guid
- UserId: Guid
- FamilyId: Guid
- TokenHash: fixed-length string, unique
- CreatedAtUtc: DateTimeOffset
- ExpiresAtUtc: DateTimeOffset
- RevokedAtUtc: DateTimeOffset?
- ReplacedByTokenId: Guid?
- DeviceDescription: string? (max 200)
```

Indexes:

- unique `TokenHash`;
- `UserId, ExpiresAtUtc`;
- `FamilyId, RevokedAtUtc`.

No navigation or DTO exposes `TokenHash`. The presented token is hashed before lookup.

### DES-AUTH-005 Rotation transaction

The handler loads the session, validates it against injected UTC time, checks the account, then marks it revoked and inserts the replacement before one `SaveChangesAsync`. Reuse of a replaced token revokes every active record in the same family.

A concurrency token protects session mutation. A concurrent second refresh either observes revocation or receives an EF concurrency conflict translated to invalid refresh token; it never issues two valid successors.

Provider-specific migrations live in the established PostgreSQL and SQL Server migration assemblies/folders. Both create equivalent constraints and indexes.

## 6. Token design

### DES-AUTH-002 JWT

JWT settings are options validated at startup:

```text
Authentication:Jwt:Issuer
Authentication:Jwt:Audience
Authentication:Jwt:SigningKey
Authentication:Jwt:AccessTokenMinutes = 15
Authentication:RefreshTokenDays = 7
Authentication:ClockSkewSeconds = 30
```

Rules:

- symmetric HMAC signing is sufficient for the initial single-service deployment;
- accepted algorithm is pinned and never inferred from the incoming token;
- signing key is at least 256 bits of entropy;
- claims use stable role/policy constants;
- no email, department name, PII or security stamps are included unless a future requirement proves necessity;
- secrets are supplied through environment/user-secrets/secret store, never committed configuration.

Changing to asymmetric keys or an external authority requires a later ADR/spec update but not changes to Application handlers.

### DES-AUTH-007 Configuration validation

Production-like environments fail fast for missing issuer, audience or weak/default signing key. Testing may register an explicit deterministic test issuer. Development documentation uses placeholders and `dotnet user-secrets`, not real values in `appsettings*.json`.

## 7. Authorization and API

### DES-AUTH-006 Policies and current user

API defines authorization policies from constants owned by Application. Policy mapping uses role requirements for this phase. Future resource checks remain in handlers and do not turn controllers into business-policy code.

A fallback policy requires authentication. Endpoints explicitly marked anonymous:

- login;
- refresh;
- logout;
- health endpoints;
- OpenAPI/Scalar only while enabled in Development.

`CurrentUserAccessor` reads `sub` as GUID and `name` from the authenticated principal. Unit tests verify missing/malformed claims return null.

The existing ticket creation endpoint receives `Tickets.Create`; a simple protected read/smoke endpoint or the same route proves `401`, `403` and allowed execution without expanding ticket scope.

### HTTP errors

- invalid login: `401`, `auth.invalid_credentials`;
- invalid refresh: `401`, `auth.invalid_refresh_token`;
- unauthenticated protected request: `401`;
- authenticated without policy: `403`;
- invalid body: `400` ValidationProblemDetails;
- rate limit: `429` with `Retry-After`;
- unexpected failure: existing global ProblemDetails mapping.

Authentication challenge/forbid responses use the same safe ProblemDetails shape as application errors.

## 8. Development seed

### DES-AUTH-008 Seed strategy

Startup invokes a scoped initializer only when all are true:

- environment is Development;
- `Authentication:Seed:Enabled=true`;
- administrator identifier and password arrive from user-secrets or environment variables.

The initializer:

1. creates/reconciles the four roles;
2. creates the Identity account and matching Domain user with a shared GUID;
3. assigns Administrator;
4. is idempotent;
5. logs only stable IDs and outcomes, never credentials.

Production startup never seeds an account. Missing seed secrets while seed is enabled produces an actionable startup failure.

## 9. Rate limiting and hardening

### DES-AUTH-009 Endpoint limits

Use ASP.NET Core built-in rate limiting:

- named `auth-login`: fixed window, 5/minute;
- named `auth-refresh`: fixed window, 30/minute;
- partition by normalized remote address plus a bounded, hashed identifier component when present;
- queue disabled;
- standardized `429` response and `Retry-After`.

Forwarded headers are not trusted unless known proxies/networks are explicitly configured. No raw identifier, password or token becomes a partition key or log field.

Request JSON size is bounded. Authentication endpoints do not accept form or query-string credentials.

## 10. OpenAPI

OpenAPI adds HTTP bearer JWT security scheme and marks protected operations. Login/refresh/logout examples use placeholders. Response schemas document `400`, `401`, `403` and `429` where applicable. A test generates the document and confirms security metadata without embedding example secrets.

## 11. Testing strategy

### Unit tests

- login result mapping and generic failure;
- token claims, lifetime and injected time;
- hash determinism and no plaintext persistence;
- refresh expiration, rotation, replay-family revocation and logout idempotence;
- `CurrentUserAccessor` claim parsing;
- role/policy constants and handler short-circuit behavior.

### Persistence integration tests

- Identity account/role creation;
- password verification and lockout;
- unique token hash;
- rotation committed atomically;
- concurrent refresh produces one successor;
- provider-neutral date and key behavior.

In-memory EF is not sufficient evidence for uniqueness, transactions or concurrency. Those checks run against real PostgreSQL and SQL Server in the provider matrix.

### API integration tests

- login success and generic failures;
- refresh success, expiration and replay;
- logout idempotence;
- protected endpoint `401`, `403`, success;
- disabled user cannot login/refresh;
- rate limit returns `429`;
- OpenAPI bearer contract;
- no sensitive response/log fields.

### Architecture tests

- Domain/Application do not reference Identity/JWT/ASP.NET authentication packages;
- API remains the composition root;
- no adapter-to-adapter project reference is introduced.

## 12. Risks y mitigaciones

- **Replay de refresh token:** tokens de un solo uso, familia y revocación transaccional.
- **Doble refresh concurrente:** concurrency token y una sola operación de commit.
- **Enumeración de cuentas:** mismo error/status y cuerpo para todas las causas de login fallido.
- **JWT con privilegios obsoletos:** vida de 15 minutos; cambios críticos revocan refresh sessions. Revocación inmediata de access tokens queda fuera de alcance explícitamente.
- **Secret leakage:** options validadas, redacción, placeholders y pruebas negativas.
- **Acoplamiento a Identity:** puertos de Application y tipos de Identity confinados a Persistence/API composition.
- **Perfil y cuenta divergentes:** GUID compartido y creación inicial atómica; phase `020` deberá mantener ambas partes en una transacción.
- **Diferencias entre providers:** constraints/migrations separadas y misma suite de contrato.

## 13. Alternativas descartadas

- **Hacer que Domain `User` herede de `IdentityUser`:** viola el aislamiento del dominio.
- **Guardar refresh token en texto claro:** aumenta el impacto de una filtración de base de datos.
- **Cookies en esta fase:** no existe todavía contrato de navegador/CSRF; body opaco es más neutral para la API backend.
- **Guardar denylist para cada access token:** agrega estado y complejidad fuera del riesgo aceptado con 15 minutos de vida.
- **Identity y dominio en DbContexts/adapters desconectados:** complica la creación atómica y la consistencia del GUID compartido.
