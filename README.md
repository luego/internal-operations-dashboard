# Internal Operations Dashboard

Backend-first internal operations system for tickets, departments, users, comments, history and operational metrics. The backend baseline targets .NET 10 and follows pragmatic Clean Architecture with Ports and Adapters.

The repository has completed **phase 1: application and persistence foundation**. It contains the project boundaries, cross-cutting application primitives, EF Core persistence adapters, dual-provider composition and a minimal ticket-creation endpoint that validates the vertical architecture.

## Prerequisites

- .NET SDK compatible with `global.json` (`10.0.3xx`; latest patch)
- Git

Docker, PostgreSQL and SQL Server are not required to build or run the automated tests. Running the API against a real database requires either PostgreSQL or SQL Server and the corresponding `Database` configuration.

## Validate the foundation

Run these commands from the repository root:

```bash
dotnet tool restore
dotnet restore InternalOperations.slnx --locked-mode
dotnet format InternalOperations.slnx --verify-no-changes --no-restore
dotnet build InternalOperations.slnx --configuration Release --no-restore -p:ContinuousIntegrationBuild=true
dotnet test InternalOperations.slnx --configuration Release --no-build --no-restore
```

The build treats warnings as errors under the CI flag. The architecture suite verifies both assembly dependencies and exact project-reference boundaries.

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
- [Architecture decision records](docs/adr/)

Changes are developed from an approved feature spec. Requirements, design, tasks, tests and evidence must remain synchronized. Frontend work is outside the current backend scope.
