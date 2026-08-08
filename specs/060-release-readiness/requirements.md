# 060 — Release Readiness: Requirements

**Estado:** Completed
**Fecha:** 8 de agosto de 2026
**Aprobación:** Fast-track para cerrar el showcase backend.

## Objetivo

Dejar el backend reproducible, diagnosticable y ejecutable como demo sin incorporar infraestructura empresarial innecesaria.

## Requisitos

- **REQ-RLS-001:** Exponer liveness y readiness anónimos; readiness debe comprobar acceso a la base de datos.
- **REQ-RLS-002:** Mantener respuestas de error centralizadas y logging de excepciones sin exponer detalles sensibles.
- **REQ-RLS-003:** Proporcionar una imagen Docker multi-stage, no-root y con puerto HTTP explícito.
- **REQ-RLS-004:** Proporcionar un compose de demo PostgreSQL sin credenciales reales versionadas.
- **REQ-RLS-005:** Documentar configuración, migraciones, arranque, health checks y verificación.
- **REQ-RLS-006:** Ejecutar build estricto, regresión completa y provider matrix alojada.

## Fuera de alcance

- Kubernetes, tracing distribuido, Prometheus/Grafana y despliegue cloud específico.
- Gestión automática de secretos o migraciones destructivas.
- Frontend y pipeline de publicación de imágenes.
