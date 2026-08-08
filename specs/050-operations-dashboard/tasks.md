# 050 — Operations Dashboard: Tasks

**Estado:** Implementing
**Fecha:** 8 de agosto de 2026
**Modo:** Fast-track proporcional para showcase.

- [x] **TASK-DSH-001** Definir contratos, validator, handlers y puerto Application mediante TDD.
- [x] **TASK-DSH-002** Implementar summary y trends provider-agnostic mediante TDD.
- [x] **TASK-DSH-003** Publicar endpoints autorizados y contrato HTTP.
- [x] **TASK-DSH-004** Ampliar provider contract con agregados conocidos.
- [ ] **TASK-DSH-005** Ejecutar gates locales y matriz alojada.
- [ ] **TASK-DSH-006** Sincronizar README/evidencia y cerrar spec.

## Gate

El cierre requiere ejecución alojada real sobre PostgreSQL y SQL Server.

## Evidencia local

- TDD focalizado: Application 3/3, Persistence 1/1 y API contract 1/1.
- Regresión `Category!=ProviderMatrix`: 138/138 pruebas aprobadas.
- Build Release CI: 0 warnings y 0 errores.
- Formato, `git diff --check` y drift dual-provider correctos.
- Provider contract ampliado; ejecución alojada pendiente del push.
