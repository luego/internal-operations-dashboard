# ADR 0004: Support PostgreSQL and SQL Server as equal persistence providers

- **Status:** Accepted
- **Date:** 2026-08-04
- **Decision owners:** Backend maintainers
- **Related:** Master baseline sections 2.4 and 8

## Context

The backend must run on PostgreSQL or SQL Server through configuration without rewriting Domain, Application, controllers or use cases. EF Core provider parity at compile time is insufficient because migrations, collation, dates, concurrency and query translation can differ at runtime.

## Decision

Persistence will select `PostgreSql` or `SqlServer` in the composition root through configuration. Application owns repository and Unit of Work ports; Persistence implements them with EF Core and provider-specific registration.

Each provider has a separate migration history and design-time factory. The same persistence and API contract suites run against real PostgreSQL and SQL Server containers. Baseline mappings use portable GUIDs, explicit string lengths and decimal precision, consistent UTC handling, explicit delete behavior and a provider-neutral concurrency strategy.

Provider-exclusive types or optimizations require an adapter, a functionally equivalent fallback, tests for both providers and a documented decision.

## Consequences

- Switching providers changes configuration and migration assembly, not use cases.
- CI and local compatibility testing are more expensive because two real engines are required.
- Migrations must be created and reviewed twice.
- Some provider-specific optimizations will be unavailable or deliberately encapsulated.

## Alternatives considered

- **PostgreSQL only:** rejected because it does not satisfy the portability requirement.
- **SQL Server only:** rejected for the same reason and is heavier for default local development.
- **One shared migration set:** rejected because EF Core generates provider-specific SQL and metadata.
- **Lowest-common-denominator SQL everywhere:** rejected as too restrictive; adapters with tested fallbacks are allowed when justified.
