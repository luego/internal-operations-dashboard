# Internal Operations Dashboard

Backend-first internal operations system for tickets, departments, users, comments, history and operational metrics. The backend baseline targets .NET 10 and follows pragmatic Clean Architecture with Ports and Adapters.

The repository has completed **phase 1: application and persistence foundation**. It contains the project boundaries, cross-cutting application primitives, EF Core persistence adapters, dual-provider composition and a minimal ticket-creation endpoint that validates the vertical architecture.

**Phase 2: identity and access** is now implementing. The current checkpoint includes ASP.NET Core Identity, JWT access tokens, rotating refresh sessions with replay-family revocation, role policies, secure Development seeding, authentication rate limits, OpenAPI bearer metadata and separate EF Core migrations for PostgreSQL and SQL Server. The real PostgreSQL/SQL Server contract matrix remains open before the phase can be marked completed.

**Phase 3: departments and users** has an approved specification. Phase 3 production code remains blocked until the phase 2 provider matrix has been published and observed successfully, unless a documented waiver is approved.

## Prerequisites

- .NET SDK compatible with `global.json` (`10.0.3xx`; latest patch)
- Git

Docker, PostgreSQL and SQL Server are not required to build or run the fast foundation suite. Docker is required for the provider-contract suite, which starts disposable PostgreSQL and SQL Server containers through Testcontainers without storing database credentials in the repository. Running the API against a persistent database requires either PostgreSQL or SQL Server and the corresponding `Database` configuration.

## Validate the backend

Run these commands from the repository root:

```bash
dotnet tool restore
dotnet restore InternalOperations.slnx --locked-mode
dotnet format InternalOperations.slnx --verify-no-changes --no-restore
dotnet build InternalOperations.slnx --configuration Release --no-restore -p:ContinuousIntegrationBuild=true
dotnet test InternalOperations.slnx --configuration Release --no-build --no-restore --filter "Category!=ProviderMatrix"
```

The build treats warnings as errors under the CI flag. The architecture suite verifies both assembly dependencies and exact project-reference boundaries.

With a working Docker daemon, execute the real relational provider matrix separately:

```bash
dotnet test tests/InternalOperations.ProviderContractTests \
  --configuration Release \
  --filter "Category=ProviderMatrix"
```

The provider contracts apply the complete migration history, verify unique refresh-token hashes, restrictive account relationships, optimistic concurrency, rollback and reapplication independently on PostgreSQL and SQL Server. GitHub Actions runs both providers as an explicit matrix after the foundation job succeeds.

## Development authentication seed

The administrator seed is opt-in and runs only in `Development`. Keep JWT and seed credentials out of `appsettings*.json`; configure local placeholder values with user secrets:

```bash
dotnet user-secrets --project src/InternalOperations.Api set "Authentication:Jwt:Issuer" "https://issuer.example.test"
dotnet user-secrets --project src/InternalOperations.Api set "Authentication:Jwt:Audience" "internal-operations-api"
dotnet user-secrets --project src/InternalOperations.Api set "Authentication:Jwt:SigningKey" "<at-least-32-byte-development-signing-key>"
dotnet user-secrets --project src/InternalOperations.Api set "Authentication:Seed:Enabled" "true"
dotnet user-secrets --project src/InternalOperations.Api set "Authentication:Seed:AdministratorIdentifier" "<development-admin-identifier>"
dotnet user-secrets --project src/InternalOperations.Api set "Authentication:Seed:AdministratorPassword" "<development-admin-password>"
dotnet user-secrets --project src/InternalOperations.Api set "Authentication:Seed:AdministratorDisplayName" "Development Administrator"
```

Leave `Authentication:Seed:Enabled` unset or `false` when the seed is not required. Never use real account data or production credentials in these examples.

## Database migrations

Each provider has an independent migration assembly and design-time factory:

- `InternalOperations.Persistence.Migrations.PostgreSql`
- `InternalOperations.Persistence.Migrations.SqlServer`

Restore the local `dotnet-ef` tool before inspecting or applying migrations. Supply real connection strings through environment variables or a secret manager; never commit them:

```bash
dotnet tool restore

dotnet ef database update \
  --project src/InternalOperations.Persistence.Migrations.PostgreSql \
  --startup-project src/InternalOperations.Persistence.Migrations.PostgreSql \
  --context ApplicationDbContext \
  --connection "$POSTGRES_CONNECTION_STRING"

dotnet ef database update \
  --project src/InternalOperations.Persistence.Migrations.SqlServer \
  --startup-project src/InternalOperations.Persistence.Migrations.SqlServer \
  --context ApplicationDbContext \
  --connection "$SQLSERVER_CONNECTION_STRING"
```

Generate and review migrations independently for both providers whenever the EF model changes. CI/local provider validation must start from empty databases and apply the complete history before running contract tests.

## Solution structure

```text
src/
├── InternalOperations.Api
├── InternalOperations.Application
├── InternalOperations.Domain
├── InternalOperations.Infrastructure
├── InternalOperations.Persistence
├── InternalOperations.Persistence.Migrations.PostgreSql
├── InternalOperations.Persistence.Migrations.SqlServer
└── InternalOperations.Shared

tests/
├── InternalOperations.Api.IntegrationTests
├── InternalOperations.Application.UnitTests
├── InternalOperations.ArchitectureTests
├── InternalOperations.Domain.UnitTests
├── InternalOperations.Persistence.IntegrationTests
└── InternalOperations.ProviderContractTests
```

Production dependencies point toward the core:

```text
Api -> Application -> Domain
Api -> Infrastructure -> Application
Api -> Persistence -> Application + Domain
Api -> provider migration assemblies -> Persistence
Shared <- stable technical primitives only
```

`Api` references adapters only to serve as the future composition root. `Domain` does not depend on ASP.NET Core, Entity Framework Core, MediatR, AutoMapper or database providers. `Application` does not depend on API or adapter projects.

## Specifications and decisions

- [Master backend specification](Internal-Operations-Dashboard-Backend-Spec.md)
- [Foundation requirements](specs/000-solution-foundation/requirements.md)
- [Foundation design](specs/000-solution-foundation/design.md)
- [Foundation tasks](specs/000-solution-foundation/tasks.md)
- [Application and persistence requirements](specs/005-application-and-persistence-foundation/requirements.md)
- [Application and persistence design](specs/005-application-and-persistence-foundation/design.md)
- [Application and persistence tasks and evidence](specs/005-application-and-persistence-foundation/tasks.md)
- [Identity and access requirements — Implementing](specs/010-identity-and-access/requirements.md)
- [Identity and access design — Implementing](specs/010-identity-and-access/design.md)
- [Identity and access tasks and evidence — Implementing](specs/010-identity-and-access/tasks.md)
- [Departments and users requirements — Approved](specs/020-departments-and-users/requirements.md)
- [Departments and users design — Approved](specs/020-departments-and-users/design.md)
- [Departments and users tasks — Approved](specs/020-departments-and-users/tasks.md)
- [Architecture decision records](docs/adr/)

Changes are developed from an approved feature spec. Requirements, design, tasks, tests and evidence must remain synchronized. Frontend work is outside the current backend scope.
