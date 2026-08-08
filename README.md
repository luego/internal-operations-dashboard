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

**Frontend phase 1** is implemented. Next.js provides a responsive landing page, secure BFF login with HttpOnly access/refresh cookies, automatic refresh rotation, protected dashboard routing and live dashboard metrics. The remaining frontend phases add ticket and administration workflows.

## Prerequisites

- .NET SDK compatible with `global.json` (`10.0.3xx`; latest patch)
- Git

Docker, PostgreSQL and SQL Server are not required to build or run the fast foundation suite. Docker is required for the provider-contract suite, which starts disposable PostgreSQL and SQL Server containers through Testcontainers without storing database credentials in the repository. Running the API against a persistent database requires either PostgreSQL or SQL Server and the corresponding `Database` configuration.

## One-command full-stack showcase

This is the quickest way to test the complete flow on Apple Silicon. It requires only Git and Docker Desktop; .NET and Node.js run inside Linux ARM64-compatible containers.

```bash
git clone https://github.com/luego/internal-operations-dashboard.git
cd internal-operations-dashboard
chmod +x scripts/start-showcase.sh
./scripts/start-showcase.sh
```

The installer asks interactively for:

- the administrator email (or accepts `admin@opsdesk.local`);
- a PostgreSQL password and confirmation;
- an administrator login password and confirmation.

Input is hidden. The script validates strong passwords and generates the JWT signing key automatically. It stores them in the local `.env.showcase` file with mode `600`; that file is ignored by Git, is never preconfigured or versioned, and lets Docker Compose resume the existing stack without asking for credentials again. All published ports bind to `127.0.0.1` because local HTTP cookies are intentionally non-secure.

The stack builds and starts in dependency order:

```text
PostgreSQL 17 -> EF Core migrator -> .NET 10 API -> Next.js frontend
```

After the readiness checks pass:

- Frontend and login: <http://localhost:3000>
- API readiness: <http://localhost:8080/health/ready>
- API liveness: <http://localhost:8080/health/live>

Use the administrator email and password entered during installation. The Development seed is idempotent and exists only for this local showcase configuration.

### Stop, resume or reset

Keep the database and resume without entering passwords again:

```bash
docker compose --env-file .env.showcase stop
docker compose --env-file .env.showcase start
```

Inspect status and logs:

```bash
docker compose --env-file .env.showcase ps
docker compose --env-file .env.showcase logs -f frontend api migrator database
```

Remove containers and all PostgreSQL data, then enter fresh credentials:

```bash
./scripts/start-showcase.sh --reset
```

`--reset` is destructive and is intended only for disposable showcase data. If ports `3000`, `8080` or `5432` are occupied, set `FRONTEND_PORT`, `API_PORT` or `POSTGRES_PORT` before running the script.

## Apple Silicon development setup with PostgreSQL

This is the recommended local workflow for an Apple Silicon Mac. PostgreSQL runs in Docker while the API runs from the host with the .NET SDK, which keeps migrations, logs and debugging straightforward.

### 1. Clone and restore

```bash
git clone https://github.com/luego/internal-operations-dashboard.git
cd internal-operations-dashboard
git checkout main
git pull origin main

dotnet --version
docker version
dotnet tool restore
dotnet restore InternalOperations.slnx --locked-mode
```

The SDK selected by `global.json` must be available (`10.0.3xx`, latest compatible patch), and Docker Desktop must be running.

### 2. Configure the local PostgreSQL password

Use a development-only value and never commit it:

```bash
export POSTGRES_PASSWORD='<strong-local-postgres-password>'
```

### 3. Start PostgreSQL

`postgres:17-alpine` works through Docker Desktop on Apple Silicon. Set a different host port first if `5432` is occupied, then create the standalone development container:

```bash
export POSTGRES_PORT=${POSTGRES_PORT:-5432}
docker run --name internal-operations-postgres --detach \
  --publish "127.0.0.1:${POSTGRES_PORT}:5432" \
  --env POSTGRES_DB=internal_operations \
  --env POSTGRES_USER=internal_operations \
  --env POSTGRES_PASSWORD="$POSTGRES_PASSWORD" \
  --volume internal-operations-postgres:/var/lib/postgresql/data \
  --health-cmd='pg_isready -U internal_operations -d internal_operations' \
  --health-interval=5s \
  --health-timeout=5s \
  --health-retries=20 \
  postgres:17-alpine

docker inspect --format '{{.State.Health.Status}}' internal-operations-postgres
docker logs internal-operations-postgres
```

Wait until the service reports `healthy`. PostgreSQL is available only on `127.0.0.1:${POSTGRES_PORT}`.

### 4. Apply the PostgreSQL migrations

```bash
export POSTGRES_CONNECTION_STRING="Host=localhost;Port=${POSTGRES_PORT:-5432};Database=internal_operations;Username=internal_operations;Password=${POSTGRES_PASSWORD}"

dotnet ef database update \
  --project src/InternalOperations.Persistence.Migrations.PostgreSql \
  --startup-project src/InternalOperations.Persistence.Migrations.PostgreSql \
  --context ApplicationDbContext \
  --connection "$POSTGRES_CONNECTION_STRING"
```

Migrations are explicit: starting a container never changes the schema silently.

### 5. Configure local JWT and administrator seed

The seed is opt-in and is accepted only in `Development`:

```bash
dotnet user-secrets --project src/InternalOperations.Api set "Authentication:Jwt:Issuer" "https://issuer.example.test"
dotnet user-secrets --project src/InternalOperations.Api set "Authentication:Jwt:Audience" "internal-operations-api"
dotnet user-secrets --project src/InternalOperations.Api set "Authentication:Jwt:SigningKey" "<at-least-32-byte-development-signing-key>"
dotnet user-secrets --project src/InternalOperations.Api set "Authentication:Seed:Enabled" "true"
dotnet user-secrets --project src/InternalOperations.Api set "Authentication:Seed:AdministratorIdentifier" "<development-admin-email>"
dotnet user-secrets --project src/InternalOperations.Api set "Authentication:Seed:AdministratorPassword" "<strong-development-admin-password>"
dotnet user-secrets --project src/InternalOperations.Api set "Authentication:Seed:AdministratorDisplayName" "Development Administrator"
```

### 6. Run the API from the Mac

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://localhost:5080
export Database__Provider=PostgreSql
export ConnectionStrings__PostgreSql="$POSTGRES_CONNECTION_STRING"

dotnet run --project src/InternalOperations.Api --no-launch-profile
```

Keep that terminal open. On first startup the idempotent Development seed creates the administrator account configured above.

### 7. Verify health and authentication

In a second terminal:

```bash
curl --fail http://localhost:5080/health/live
curl --fail http://localhost:5080/health/ready

curl --request POST http://localhost:5080/api/v1/auth/login \
  --header 'Content-Type: application/json' \
  --data '{
    "identifier": "<development-admin-email>",
    "password": "<strong-development-admin-password>",
    "deviceDescription": "Apple Silicon Mac"
  }'
```

Both health endpoints must report `Healthy`; login returns the JWT access token and rotating refresh token used by protected endpoints.

### 8. Stop or reset the database

Stop containers while preserving local data:

```bash
docker stop internal-operations-postgres
```

Resume it later with `docker start internal-operations-postgres`.

Delete the local PostgreSQL container, volume and all development data:

```bash
docker rm --force internal-operations-postgres
docker volume rm internal-operations-postgres
```

After deleting the volume, repeat the migration step before starting the API. See the [operations runbook](docs/operations-runbook.md) for the fully containerized API workflow and troubleshooting.

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
