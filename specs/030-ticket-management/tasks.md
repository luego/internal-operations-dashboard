# 030 — Ticket Management: Tasks

**Estado:** Completed
**Fecha:** 7 de agosto de 2026
**Modo:** Fast-track proporcional para showcase.

- [x] **TASK-TKT-001** Endurecer `Ticket` con fábrica, validación, versión y máquina de estados mediante TDD.
- [x] **TASK-TKT-002** Definir DTOs, validators, commands/queries y puerto específico para el primer corte.
- [x] **TASK-TKT-003** Implementar create/get y validación de referencias.
- [x] **TASK-TKT-004** Implementar list/update/status y concurrencia.
- [x] **TASK-TKT-005** Publicar cinco endpoints autorizados y OpenAPI.
- [x] **TASK-TKT-006** Añadir mappings e índices explícitos y migraciones dual-provider.
- [x] **TASK-TKT-007** Ampliar provider contract para número, FKs, consultas y concurrencia.
- [x] **TASK-TKT-008** Ejecutar format, build estricto, regresión, drift y matriz real.
- [x] **TASK-TKT-009** Sincronizar evidencia y cerrar la spec.

## Gate

La implementación local puede continuar por autorización fast-track. `TASK-TKT-007..009` no se cierran sin evidencia real; commit local y push permanecen separados.


## Evidencia local

- TASK-TKT-001: RED por API de dominio ausente; GREEN con 4/4 pruebas de Ticket y 21/21 pruebas Domain.
- TASK-TKT-002/003: RED por contratos y servicio ausentes; GREEN con 3/3 pruebas Application, 3/3 Persistence y 1/1 contrato HTTP del corte create/get.
- TASK-TKT-006: migraciones `CompleteTicketFoundation` generadas para PostgreSQL y SQL Server; ambos snapshots sin drift.
- Contrato alojado ejecutado para numeración, FKs, consultas, mutaciones y concurrencia.
- Provider matrix alojada: run `31224821985`, Foundation, PostgreSQL y SQL Server en `success`; rollback/reapply de migraciones incluido.
- TASK-TKT-004/005: listado SQL paginado con filtros y orden allowlisted, actualización, máquina de estados, cinco endpoints y policies específicas; pruebas focalizadas verdes.
- Regresión local rápida: 122/122 pruebas aprobadas (41 Application, 21 Domain, 10 Architecture, 28 API y 22 Persistence).
- Build Release estricto: 0 warnings, 0 errores.
- Format verify y `git diff --check`: exitosos.
