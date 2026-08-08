# Full-Stack Showcase Operations Runbook

## Scope

This runbook starts the Fiverr showcase with Next.js, ASP.NET Core and PostgreSQL. It intentionally avoids production-platform-specific deployment machinery.

## Prerequisite

Install and start Docker Desktop. The supplied images support Apple Silicon and x86-64 hosts.

## Local verification

```bash
dotnet tool restore
dotnet restore InternalOperations.slnx --locked-mode
dotnet format InternalOperations.slnx --verify-no-changes --no-restore
dotnet build InternalOperations.slnx --configuration Release --no-restore -p:ContinuousIntegrationBuild=true
dotnet test InternalOperations.slnx --configuration Release --no-build --no-restore --filter "Category!=ProviderMatrix"

cd frontend
npm ci
npm audit --audit-level=high
npm test
npm run typecheck
npm run lint
npm run build
cd ..
```

## Start the demo

```bash
chmod +x scripts/start-showcase.sh
./scripts/start-showcase.sh
```

The script asks for the PostgreSQL and administrator passwords using hidden input. It generates the JWT signing key, writes the values only to the Git-ignored `.env.showcase` file with mode `600`, builds the images, applies PostgreSQL migrations through the one-shot `migrator` service and waits for API/frontend readiness.

PostgreSQL, API and frontend ports bind exclusively to `127.0.0.1`. This is a local HTTP showcase, not an internet-facing deployment.

## Verify runtime health

```bash
curl --fail http://localhost:${API_PORT:-8080}/health/live
curl --fail http://localhost:${API_PORT:-8080}/health/ready
curl --fail http://localhost:${FRONTEND_PORT:-3000}
```

- `live` proves the API process can answer requests.
- `ready` returns HTTP 200 only when PostgreSQL is reachable.
- API errors use ProblemDetails; unhandled exceptions are logged without returning stack traces.
- Login at the frontend with the email and password entered during installation.

## Inspect, stop or resume

```bash
docker compose --env-file .env.showcase ps
docker compose --env-file .env.showcase logs -f frontend api migrator database
docker compose --env-file .env.showcase stop
docker compose --env-file .env.showcase start
```

## Reset

The following command removes containers, the PostgreSQL volume and `.env.showcase`, then asks for fresh credentials:

```bash
./scripts/start-showcase.sh --reset
```

This is destructive and intended only for disposable showcase data.

## Troubleshooting

- `ready` returns 503: inspect `database` and `migrator` logs.
- API fails during startup: inspect JWT configuration and the Development seed logs.
- Frontend login returns a service error: verify the `api` service is healthy.
- Ports are occupied: set `POSTGRES_PORT`, `API_PORT` or `FRONTEND_PORT` before running the script.
- Existing credentials should change: use `--reset`; do not edit a running PostgreSQL stack to change only its environment password.
- Provider behavior is uncertain: inspect the latest GitHub Actions provider-matrix run; do not substitute InMemory evidence for PostgreSQL or SQL Server.
