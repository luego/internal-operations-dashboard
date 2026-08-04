# ADR 0002: Enforce pragmatic Clean Architecture boundaries

- **Status:** Accepted
- **Date:** 2026-08-04
- **Decision owners:** Backend maintainers
- **Related:** REQ-FND-002, REQ-FND-003, REQ-FND-006; DES-FND-002, DES-FND-003

## Context

The backend must support business evolution and interchangeable database providers without leaking ASP.NET Core or EF Core into the business model. A single project would make those boundaries conventional rather than enforceable, while excessive layering would add delegation without value.

## Decision

Use a pragmatic Clean Architecture with Ports and Adapters across six production projects:

```text
Api -> Application -> Domain
Api -> Infrastructure -> Application
Api -> Persistence -> Application + Domain
Shared <- stable technical primitives only
```

Domain does not reference ASP.NET Core, EF Core, MediatR, AutoMapper or database providers. Application owns use cases and ports but does not reference Api, Infrastructure or Persistence. Api references adapters only in the composition root. Shared remains small and contains neither domain language nor application behavior.

Project-reference and assembly-dependency tests enforce these rules. A new boundary or exception requires a traceable need and, when architectural, an ADR.

## Consequences

- Business rules can be tested without web or database infrastructure.
- Provider-specific implementation stays behind Application ports.
- The solution contains more projects and explicit mappings than a monolith.
- Architecture tests become mandatory maintenance assets rather than documentation alone.

## Alternatives considered

- **Single layered project:** rejected because dependency direction cannot be protected adequately.
- **Feature-only modular monolith with no layer projects:** deferred; vertical feature organization will exist inside Application while retaining enforceable outer boundaries.
- **Microservices:** rejected as outside the baseline scope and operationally disproportionate.
