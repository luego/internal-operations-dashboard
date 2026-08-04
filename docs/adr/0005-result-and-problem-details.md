# ADR 0005: Use typed results for expected failures and Problem Details over HTTP

- **Status:** Accepted
- **Date:** 2026-08-04
- **Decision owners:** Backend maintainers
- **Related:** Master baseline sections 2.4, 7.1 and 7.2

## Context

Validation failures, missing resources, authorization denials, concurrency conflicts and business-rule rejections are expected outcomes. Modeling them as exceptions produces noisy logs, inconsistent handlers and accidental disclosure. Unexpected failures still need centralized handling and traceability.

## Decision

Application use cases return `Result` or `Result<T>` with a stable error code and a typed category: Validation, NotFound, Conflict, Unauthorized, Forbidden or Failure.

Api maps results centrally to HTTP and serializes errors as RFC Problem Details-compatible responses containing `type`, `title`, `status`, `detail`, `instance`, `traceId`, `errorCode` and field errors when applicable. Baseline mappings are 400, 401, 403, 404 and 409; a feature spec decides whether a business `Failure` is 400 or 422.

ASP.NET Core's global exception handler handles unexpected exceptions once, logs them with trace context and returns a generic 500 without stack traces, SQL or secrets. Controllers do not contain general `try/catch` blocks.

## Consequences

- Expected outcomes are explicit, testable and consistently mapped.
- Error codes form part of the API contract and require compatibility care.
- Handlers must distinguish expected failures from exceptional states.
- Infrastructure exceptions must be translated at an appropriate boundary when they represent an expected conflict.

## Alternatives considered

- **Exceptions for all failures:** rejected because expected control flow becomes expensive and inconsistently logged.
- **HTTP-aware results in Application:** rejected because it couples use cases to ASP.NET Core.
- **Ad hoc controller responses:** rejected because clients would receive inconsistent error contracts.
- **One generic error category:** rejected because authorization, validation, absence and conflicts have different semantics.
