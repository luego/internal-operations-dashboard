# ADR 0003: Separate mediator handlers from reusable application services

- **Status:** Accepted
- **Date:** 2026-08-04
- **Decision owners:** Backend maintainers
- **Related:** Master baseline sections 2.3 and 7.3

## Context

Mediator handlers and service classes can easily become two parallel layers that only forward calls. That duplication obscures where a use case is coordinated and creates interfaces without meaningful substitution boundaries.

## Decision

When the application layer is implemented:

- controllers translate HTTP, send a command/query through the mediator, and map the result to HTTP;
- one handler is the entry point for each use case and coordinates validation, contextual authorization, ports and transactions;
- an application service exists only for behavior reused by multiple handlers or behavior complex enough to justify a stable interface;
- domain entities and value objects own invariants and valid state transitions;
- repositories express persistence access and do not contain application orchestration.

Public repositories and meaningful service boundaries have interfaces. Interfaces that merely mirror one implementation without a substitute or boundary are not created. A service such as `TicketService.GetById` that only delegates to a repository is prohibited.

## Consequences

- Every use case has one clear orchestration point.
- Reusable rules can be shared without duplicating handler logic.
- Handlers may contain coordination code; this is intentional and not a reason by itself to add a service.
- Reviews must distinguish domain behavior, reusable application behavior and persistence concerns.

## Alternatives considered

- **Service layer for every entity operation:** rejected because it duplicates handlers and repositories.
- **Handlers contain every rule:** rejected because cross-use-case behavior would be duplicated and domain invariants could be bypassed.
- **Controllers call services directly:** rejected because it removes the consistent application pipeline and encourages HTTP-aware business logic.
