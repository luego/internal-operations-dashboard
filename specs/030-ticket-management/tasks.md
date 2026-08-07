# 030 — Ticket Management: Tasks

**Estado:** Implementing
**Fecha:** 7 de agosto de 2026
**Modo:** Fast-track proporcional para showcase.

- [x] **TASK-TKT-001** Endurecer `Ticket` con fábrica, validación, versión y máquina de estados mediante TDD.
- [ ] **TASK-TKT-002** Definir DTOs, filtros, validators, commands/queries y puerto específico.
- [ ] **TASK-TKT-003** Implementar create/get y validación de referencias.
- [ ] **TASK-TKT-004** Implementar list/update/status y concurrencia.
- [ ] **TASK-TKT-005** Publicar cinco endpoints autorizados y OpenAPI.
- [ ] **TASK-TKT-006** Añadir mappings e índices explícitos y migraciones dual-provider.
- [ ] **TASK-TKT-007** Ampliar provider contract para número, FKs, consultas y concurrencia.
- [ ] **TASK-TKT-008** Ejecutar format, build estricto, regresión, drift y matriz real.
- [ ] **TASK-TKT-009** Sincronizar evidencia y cerrar la spec.

## Gate

La implementación local puede continuar por autorización fast-track. `TASK-TKT-007..009` no se cierran sin evidencia real; commit local y push permanecen separados.


## Evidencia local

- TASK-TKT-001: RED por API de dominio ausente; GREEN con 4/4 pruebas de Ticket y 21/21 pruebas Domain.
- Build Release estricto: 0 warnings, 0 errores.
- Format verify y `git diff --check`: exitosos.
