# 040 — Ticket Comments and History: Requirements

**Estado:** Completed
**Fecha:** 7 de agosto de 2026
**Aprobación:** Fast-track autorizado por el usuario para completar el showcase sin gates intermedios.
**Dependencia:** `../030-ticket-management/`

## Objetivo

Añadir colaboración y una línea de actividad inmutable por ticket, manteniendo un alcance sencillo y demostrable.

## Requisitos funcionales

- **REQ-TCH-001 — Add comment:** un usuario activo puede añadir un comentario no vacío de hasta 4000 caracteres a un ticket activo.
- **REQ-TCH-002 — List comments:** los comentarios se listan por ticket con paginación y orden cronológico determinista.
- **REQ-TCH-003 — History:** cada ticket expone una línea de actividad cronológica que registra creación, actualización, cambio de estado y comentario añadido.
- **REQ-TCH-004 — Attribution:** comentarios y actividades conservan autor, fecha UTC y una descripción segura para mostrar.
- **REQ-TCH-005 — Retention:** comentarios usan la baja lógica transversal; la actividad es inmutable y no tiene endpoint de eliminación.
- **REQ-TCH-006 — HTTP:** publicar `POST/GET /api/v1/tickets/{ticketId}/comments` y `GET /api/v1/tickets/{ticketId}/history`.

## Seguridad y errores

- Lecturas requieren `Tickets.Read`; añadir comentarios requiere `Tickets.Create`.
- No se aceptan `userId`, timestamps ni identificadores técnicos controlados por el cliente; el autor proviene del usuario autenticado.
- Errores estables: `tickets.not_found`, `comments.invalid_request`, `users.not_found`.

## No funcionales

- Persistencia y migraciones equivalentes en PostgreSQL y SQL Server.
- Operación de comentario y su actividad se guardan atómicamente.
- Paginación: página mínima 1; tamaño entre 1 y 100.
- Sin edición de comentarios, menciones, adjuntos ni notificaciones en este showcase.

## Aceptación

- TDD RED → GREEN para dominio, aplicación, persistencia y HTTP.
- Build Release sin warnings, formato limpio, regresión verde y snapshots sin drift.
- La spec solo pasa a `Completed` tras ejecutar la matriz alojada en ambos providers.
