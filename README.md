# Internal Operations Dashboard

Backend-first internal operations system for tickets, departments, users, comments, history and operational metrics. The backend baseline targets .NET 10 and follows pragmatic Clean Architecture with Ports and Adapters.

The repository has completed **phase 1: application and persistence foundation**. It contains the project boundaries, cross-cutting application primitives, EF Core persistence adapters, dual-provider composition and a minimal ticket-creation endpoint that validates the vertical architecture.

**Phase 2: identity and access** is now implementing. The current checkpoint includes ASP.NET Core Identity, JWT access tokens, rotating refresh sessions with replay-family revocation, role policies, secure Development seeding, authentication rate limits and OpenAPI bearer metadata. Provider-specific migrations and the real PostgreSQL/SQL Server contract matrix remain open before the phase can be marked completed.

## Prerequisites

- .NET SDK compatible with `global.json` (`10.0.3xx`; latest patch)
- Git

Docker, PostgreSQL and SQL Server are not required to build or run the automated tests. Running the API against a real database requires either PostgreSQL or SQL Server and the corresponding `Database` configuration.

## Validate the backend

Run these commands from the repository root:

```bash
dotnet tool restore
dotnet restore InternalOperations.slnx --locked-mode
dotnet format InternalOperations.slnx --verify-no-changes --no-restore
dotnet build InternalOperations.slnx --configuration Release --no-restore -p:ContinuousIntegrationBuild=true
dotnet test InternalOperations.slnx --configuration Release --no-build --no-restore
```

The build treats warnings as errors under the CI flag. The architecture suite verifies both assembly dependencies and exact project-reference boundaries.

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

## Solution structure

```text
src/
├── InternalOperations.Api
├── InternalOperations.Application
├── InternalOperations.Domain
├── InternalOperations.Infrastructure
├── InternalOperations.Persistence
└── InternalOperations.Shared

tests/
├── InternalOperations.Api.IntegrationTests
├── InternalOperations.Application.UnitTests
├── InternalOperations.ArchitectureTests
├── InternalOperations.Domain.UnitTests
└── InternalOperations.Persistence.IntegrationTests
```

Production dependencies point toward the core:

```text
Api -> Application -> Domain
Api -> Infrastructure -> Application
Api -> Persistence -> Application + Domain
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
- [Architecture decision records](docs/adr/)

Changes are developed from an approved feature spec. Requirements, design, tasks, tests and evidence must remain synchronized. Frontend work is outside the current backend scope.
