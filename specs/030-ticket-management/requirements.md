# 030 — Ticket Management: Requirements

**Estado:** Completed
**Fecha:** 7 de agosto de 2026
**Aprobación:** Fast-track autorizado por el usuario para completar el showcase sin gates intermedios.
**Dependencias:** `../020-departments-and-users/`

## Objetivo

Completar un CRUD operativo sencillo de tickets con consultas paginadas, asignación, prioridad, máquina de estados y concurrencia optimista, sin borrado físico.

## Alcance

- `POST`, `GET`, listado, `PUT` y `PATCH /status` bajo `/api/v1/tickets`.
- Título 1–200, descripción 1–4000, prioridad canónica y número correlativo generado por persistencia.
- Departamento obligatorio, existente y activo; agente opcional, existente y activo.
- Estado inicial `Open`; transiciones permitidas: `Open -> InProgress|Closed`, `InProgress -> Resolved|Closed`, `Resolved -> InProgress|Closed`; `Closed` es terminal.
- Versión GUID opaca en cada mutación y conflictos como `tickets.version_conflict`.
- Listado con página 25/100, búsqueda, estado, prioridad, departamento, agente y orden allowlisted.
- Policy `Tickets.Create` para crear, `Tickets.Assign` para editar/asignar y `Tickets.ChangeStatus` para transicionar; lecturas usan `Tickets.Read`.
- No se publica `DELETE`; `IsDeleted` continúa reservado para baja lógica transversal.

## Requisitos

### REQ-TKT-001 Crear
Crea un ticket `Open` con GUID, número único, timestamps UTC y versión; devuelve `201 + Location`. Referencias inválidas o inactivas producen errores 404/409 estables.

### REQ-TKT-002 Consultar y listar
Get devuelve DTO o `tickets.not_found`. List filtra, ordena y pagina en SQL mediante proyección no-tracking y orden determinista.

### REQ-TKT-003 Actualizar
Actualiza título, descripción, prioridad, departamento y agente con versión actual; no cambia estado.

### REQ-TKT-004 Transicionar estado
Solo permite la máquina de estados aprobada; transición inválida devuelve `tickets.invalid_transition`; estado igual es idempotente.

### REQ-TKT-005 Persistencia portable
Mappings, índices, número único, FK restrictivas, concurrency token y migraciones se mantienen separados para PostgreSQL y SQL Server.

### REQ-TKT-006 Verificación
Domain, Application, Persistence, HTTP y OpenAPI se prueban localmente; las afirmaciones dual-provider esperan ejecución real alojada.

## Fuera de alcance

Comentarios, historial inmutable y dashboard pertenecen a specs 040 y 050. No hay adjuntos, SLA, notificaciones, jerarquías, bulk operations ni workflows configurables.

