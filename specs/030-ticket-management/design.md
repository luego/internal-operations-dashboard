# 030 — Ticket Management: Design

**Estado:** Implementing
**Fecha:** 7 de agosto de 2026
**Requisitos:** `requirements.md`

## Diseño mínimo

- `Domain.Ticket` encapsula creación, edición, asignación y transición; rota `Version` solo en cambios efectivos.
- Application posee DTOs, validators, commands/queries y un puerto específico `ITicketAdministrationService`.
- Persistence implementa lectura/escritura con `ApplicationDbContext`, `IClock`, proyecciones SQL y traducción de concurrencia.
- API usa un controller delgado con requests explícitos y policies existentes.
- EF configura longitudes, enums como string, versión, número único, índices de listado y FKs `Restrict`.
- El número se asigna mediante secuencia/identity portable en la inserción; no se usa `MAX + 1`.
- Mutaciones verifican Department y User activos dentro de la operación.

## Contrato

`TicketDto`: id, number, title, description, status, priority, department summary, assignee summary nullable, createdAtUtc, updatedAtUtc y version.

Errores públicos: `tickets.invalid_request`, `tickets.not_found`, `tickets.version_conflict`, `tickets.invalid_transition`, `departments.not_found`, `departments.inactive`, `users.not_found`, `users.inactive`.

## Verificación

RED → GREEN por dominio, casos de uso y persistencia; luego contrato HTTP, build estricto, regresión completa, drift de migraciones y provider matrix cuando se publique el lote.

