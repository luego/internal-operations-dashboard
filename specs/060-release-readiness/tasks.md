# 060 — Release Readiness: Tasks

**Estado:** Implementing
**Fecha:** 8 de agosto de 2026

- [x] **TASK-RLS-001** Implementar health checks live/ready mediante TDD.
- [x] **TASK-RLS-002** Añadir Dockerfile, `.dockerignore` y compose de demo.
- [x] **TASK-RLS-003** Crear runbook de operación y sincronizar README.
- [x] **TASK-RLS-004** Ejecutar gates locales y verificar artefactos.
- [ ] **TASK-RLS-005** Ejecutar CI alojado y cerrar todas las specs activas.

## Gate

El backend se declara listo solo con build estricto y regresión verdes. La construcción real de imagen se declara únicamente si existe un daemon Docker disponible.

## Evidencia local

- Health endpoints: 2/2 pruebas HTTP aprobadas.
- Regresión `Category!=ProviderMatrix`: 140/140 pruebas aprobadas.
- Build Release CI: 0 warnings y 0 errores.
- Formato, `git diff --check` y drift de PostgreSQL/SQL Server correctos.
- `compose.yaml` superó la validación YAML al escribirse; el CLI local no incluye el plugin Compose.
- La construcción real de la imagen no se ejecutó porque el daemon Docker local no está disponible; CI funcional y provider matrix permanecen como gate alojado.
