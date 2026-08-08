# Backend Operations Runbook

## Scope

This runbook starts the Fiverr showcase with PostgreSQL and validates that the API is ready. It intentionally avoids production-platform-specific deployment machinery.

## Required environment

```bash
export POSTGRES_PASSWORD='<local-demo-password>'
export JWT_ISSUER='https://issuer.example.test'
export JWT_AUDIENCE='internal-operations-api'
export JWT_SIGNING_KEY='<at-least-32-byte-local-demo-signing-key>'
```

Do not commit `.env` files, connection strings, passwords or signing keys.

## Local verification

```bash
dotnet tool restore
dotnet restore InternalOperations.slnx --locked-mode
dotnet format InternalOperations.slnx --verify-no-changes --no-restore
dotnet build InternalOperations.slnx --configuration Release --no-restore -p:ContinuousIntegrationBuild=true
dotnet test InternalOperations.slnx --configuration Release --no-build --no-restore --filter "Category!=ProviderMatrix"
```

## Start the demo

Start PostgreSQL first:

```bash
docker compose up -d database
```

Apply migrations explicitly from the host. This keeps schema changes visible rather than running them as a hidden side effect of every API replica:

```bash
export POSTGRES_CONNECTION_STRING="Host=localhost;Port=${POSTGRES_PORT:-5432};Database=internal_operations;Username=internal_operations;Password=${POSTGRES_PASSWORD}"

dotnet ef database update \
  --project src/InternalOperations.Persistence.Migrations.PostgreSql \
  --startup-project src/InternalOperations.Persistence.Migrations.PostgreSql \
  --context ApplicationDbContext \
  --connection "$POSTGRES_CONNECTION_STRING"
```

Build and start the API:

```bash
docker compose up -d --build api
```

## Verify runtime health

```bash
curl --fail http://localhost:${API_PORT:-8080}/health/live
curl --fail http://localhost:${API_PORT:-8080}/health/ready
```

- `live` proves the process can answer requests.
- `ready` returns HTTP 200 only when the configured database is reachable.
- API errors use ProblemDetails; unhandled exceptions are logged without returning stack traces.

OpenAPI and Scalar are Development-only. For a local interactive demo, run the API with `ASPNETCORE_ENVIRONMENT=Development` and external development-only credentials.

## Stop or reset

```bash
docker compose down
docker compose down --volumes # destructive: removes local demo data
```

## Troubleshooting

- `ready` returns 503: verify PostgreSQL health, connection string and network.
- API fails during startup: verify JWT issuer, audience and a signing key of at least 32 bytes.
- Tables are missing: rerun the explicit `dotnet ef database update` command.
- Provider behavior is uncertain: inspect the latest GitHub Actions provider-matrix run; do not substitute InMemory evidence for PostgreSQL or SQL Server.
