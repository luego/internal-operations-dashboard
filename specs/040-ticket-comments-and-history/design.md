# 040 — Ticket Comments and History: Design

**Estado:** Completed
**Fecha:** 7 de agosto de 2026
**Requisitos:** `requirements.md`

## Diseño mínimo

- `TicketComment` se endurece como entidad de dominio con fábrica, canonicalización Unicode Form KC e invariantes.
- `TicketActivity` es una entidad append-only con `TicketActivityType`: `Created`, `Updated`, `StatusChanged` y `CommentAdded`.
- `ITicketCollaborationService` es el puerto Application para añadir/listar comentarios y consultar historia.
- `TicketCollaborationService` valida ticket/autor, persiste comentario + actividad en un único `SaveChanges` y proyecta DTOs.
- `TicketAdministrationService` añade actividades al crear, actualizar y cambiar estado.
- EF aplica filtro lógico solo a comentarios; las actividades son historial inmutable y no usan soft delete.
- Índices: comentarios `(TicketId, CreatedAtUtc, Id)` y actividades `(TicketId, OccurredAtUtc, Id)`.
- Relaciones usan `Restrict` para conservar el historial.

## HTTP

- `POST /api/v1/tickets/{ticketId}/comments` → 201.
- `GET /api/v1/tickets/{ticketId}/comments?page=1&pageSize=25` → 200.
- `GET /api/v1/tickets/{ticketId}/history?page=1&pageSize=50` → 200.
- El `authorId` se obtiene del claim `sub`/`NameIdentifier` y nunca del body.

## Verificación

- Pruebas Domain para invariantes.
- Pruebas Application para validación y forwarding.
- Integración Persistence para atomicidad, orden y actividad.
- Contrato HTTP para rutas, policies y ausencia de author controlado por cliente.
- Migraciones/drift y contrato real PostgreSQL/SQL Server.
