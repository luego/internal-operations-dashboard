# ADR 0001: Use .NET 10 LTS

- **Status:** Accepted
- **Date:** 2026-08-04
- **Decision owners:** Backend maintainers
- **Related:** REQ-FND-001; DES-FND-001

## Context

Internal Operations Dashboard is a new backend intended to remain maintainable for several years. Its SDK, runtime, ASP.NET Core and EF Core generations must remain aligned. At project initialization the available environment provides .NET SDK 10.0.302 and .NET/ASP.NET Core runtime 10.0.10.

## Decision

Use .NET 10 LTS, ASP.NET Core 10, C# 14 and, when persistence is introduced, EF Core 10.

All projects target `net10.0`. `global.json` fixes feature band `10.0.3xx` through version `10.0.300`, uses `latestPatch`, and rejects prerelease SDKs. Runtime and package patches will be kept current through normal maintenance. Package versions are centralized and prerelease dependencies are not allowed on the main branch.

## Consequences

- The solution has an LTS maintenance window appropriate for a new system.
- Developers and CI need a compatible .NET 10.0.3xx SDK.
- Language and framework features newer than C# 14/.NET 10 are unavailable until an explicit upgrade ADR.
- Security patches can advance without changing feature band.

## Alternatives considered

- **.NET 8 LTS:** rejected because its remaining support window is too short for a new baseline.
- **.NET 9 STS:** rejected because it is not LTS and would force an early upgrade.
- **.NET 11 preview:** rejected because prerelease runtimes and packages are not acceptable for the main branch.
