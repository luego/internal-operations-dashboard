# Internal Operations Dashboard

Backend-first internal operations system for tickets, departments, users, comments, history and operational metrics. The backend baseline targets .NET 10 and follows pragmatic Clean Architecture with Ports and Adapters.

The repository has completed **phase 1: application and persistence foundation**. It contains the project boundaries, cross-cutting application primitives, EF Core persistence adapters, dual-provider composition and a minimal ticket-creation endpoint that validates the vertical architecture.

**Phase 2: identity and access** is completed. The backend includes ASP.NET Core Identity, JWT access tokens, rotating refresh sessions with replay-family revocation, role policies, secure Development seeding, authentication rate limits, OpenAPI bearer metadata and separate EF Core migrations verified on PostgreSQL and SQL Server.

**Logical deletion** is completed. Business entities use `IsDeleted` so normal operations retain records instead of physically deleting them; the contract and migrations are verified on PostgreSQL and SQL Server.

**Phase 3: departments and users** is completed. It provides authorized, paginated administration for departments and users, synchronized Identity/domain profiles, role and department assignment, activation/deactivation, refresh-session revocation, optimistic concurrency and provider-specific migrations verified on PostgreSQL and SQL Server.

**Phase 4: ticket management** is completed. It provides authorized creation, retrieval, filtered pagination, updates and status transitions with database-generated numbers, optimistic concurrency and dual-provider migration contracts.

**Phase 5: ticket comments and history** is completed. It provides authenticated comments, paginated timelines and immutable activities for ticket creation, updates and status changes, verified on PostgreSQL and SQL Server.

**Phase 6: operations dashboard** is completed. It provides authorized summary metrics and zero-filled daily trends for ticket creation and comments, verified on PostgreSQL and SQL Server.

**Release readiness** is completed. Structured liveness/readiness endpoints, container artifacts, an operations runbook and the complete PostgreSQL/SQL Server provider matrix are verified.

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

The provider contracts apply the complete migration history and verify authentication/session behavior, logical deletion, administration, tickets, comments/history, dashboard aggregation, relational constraints, optimistic concurrency, rollback and reapplication independently on PostgreSQL and SQL Server. GitHub Actions runs both providers as an explicit matrix after the foundation job succeeds.

## Runtime health and container demo

Anonymous health endpoints are available at:

- `/health/live` — process liveness, with no external dependency.
- `/health/ready` — readiness including database connectivity.

A multi-stage `Dockerfile` and PostgreSQL `compose.yaml` are provided. Configuration, explicit migration, startup and troubleshooting commands are documented in the [operations runbook](docs/operations-runbook.md). Secrets must be supplied through environment variables and must not be committed.

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
- [Identity and access requirements — Completed](specs/010-identity-and-access/requirements.md)
- [Identity and access design — Completed](specs/010-identity-and-access/design.md)
- [Identity and access tasks and evidence — Completed](specs/010-identity-and-access/tasks.md)
- [Logical deletion requirements — Completed](specs/015-logical-deletion/requirements.md)
- [Logical deletion design — Completed](specs/015-logical-deletion/design.md)
- [Logical deletion tasks — Completed](specs/015-logical-deletion/tasks.md)
- [Departments and users requirements — Completed](specs/020-departments-and-users/requirements.md)
- [Departments and users design — Completed](specs/020-departments-and-users/design.md)
- [Departments and users tasks and evidence — Completed](specs/020-departments-and-users/tasks.md)
- [Ticket management requirements — Completed](specs/030-ticket-management/requirements.md)
- [Ticket management design — Completed](specs/030-ticket-management/design.md)
- [Ticket management tasks and evidence — Completed](specs/030-ticket-management/tasks.md)
- [Ticket comments and history requirements — Completed](specs/040-ticket-comments-and-history/requirements.md)
- [Ticket comments and history design — Completed](specs/040-ticket-comments-and-history/design.md)
- [Ticket comments and history tasks and evidence — Completed](specs/040-ticket-comments-and-history/tasks.md)
- [Operations dashboard requirements — Completed](specs/050-operations-dashboard/requirements.md)
- [Operations dashboard design — Completed](specs/050-operations-dashboard/design.md)
- [Operations dashboard tasks and evidence — Completed](specs/050-operations-dashboard/tasks.md)
- [Release readiness requirements — Completed](specs/060-release-readiness/requirements.md)
- [Release readiness design — Completed](specs/060-release-readiness/design.md)
- [Release readiness tasks and evidence — Completed](specs/060-release-readiness/tasks.md)
- [Operations runbook](docs/operations-runbook.md)
- [Architecture decision records](docs/adr/)

Changes are developed from an approved feature spec. Requirements, design, tasks, tests and evidence must remain synchronized. Frontend work is outside the current backend scope.
