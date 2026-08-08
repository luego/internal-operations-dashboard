# 060 — Release Readiness: Design

**Estado:** Implementing
**Fecha:** 8 de agosto de 2026

## Diseño mínimo

- ASP.NET Core Health Checks con `live` sin dependencias y `ready` con `ApplicationDbContext.Database.CanConnectAsync`.
- Compatibilidad temporal con `/api/v1/health`.
- Dockerfile multi-stage .NET 10, runtime ASP.NET, usuario no-root y puerto 8080.
- `compose.yaml` para API + PostgreSQL usando variables requeridas desde el entorno.
- Las migraciones se aplican explícitamente antes del arranque según el runbook; no se ocultan como efecto lateral de cada réplica.
- Logging y ProblemDetails existentes se conservan como observabilidad básica proporcional.

## Verificación

- Contrato HTTP de health endpoints.
- Build y pruebas locales.
- Construcción Docker cuando el daemon esté disponible; si no, validar sintaxis/archivos y mantener evidencia honesta.
- CI alojado Foundation + PostgreSQL + SQL Server.
