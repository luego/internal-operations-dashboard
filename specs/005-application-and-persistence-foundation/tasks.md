# 005 — Application and Persistence Foundation: Tasks

**Estado:** Completed
**Fecha:** 4 de agosto de 2026
**Completada:** 6 de agosto de 2026

## Convenciones

- Cada tarea requiere evidencia ejecutable.
- Los límites de arquitectura se validan con tests, no solo con documentación.
- Los checkboxes se cierran únicamente después de format, build estricto y tests.

## Gate 1 — Aprobación y reconciliación

- [x] **GATE-APP-000 Aprobar requirements, design y tasks**
  - El usuario autorizó continuar y cerrar el siguiente incremento el 6 de agosto de 2026.
  - Se reconcilió el código adelantado con la fase 1 antes de declarar la spec completada.

## Ola 1 — Cross-cutting de Application

- [x] **TASK-APP-001 Implementar `Result`, `Error` y tipos de error**
  - Fuente única en `InternalOperations.Application/Result.cs`.
  - Pruebas unitarias verifican éxito, valor y fallo, incluido `Result.Error`.

- [x] **TASK-APP-002 Definir `IClock` e `ICurrentUser`**
  - Contratos pequeños en Application sin dependencia de ASP.NET Core.

- [x] **TASK-APP-003 Configurar MediatR mínimo**
  - MediatR, command handler y validation behavior registrados en Application/API.

## Ola 2 — Persistence base

- [x] **TASK-PER-001 Crear `ApplicationDbContext`**
  - DbContext central con entidades de dominio y configuración EF Core.

- [x] **TASK-PER-002 Implementar repositorio genérico y Unit of Work**
  - Puertos `IRepository<T>` e `IUnitOfWork` ubicados en Application.
  - `GenericRepository<T>` y `UnitOfWork` implementados en Persistence.
  - Integration tests verifican operaciones y confirmación de cambios en memoria.

- [x] **TASK-PER-003 Preparar provider dual**
  - `Database:Provider` selecciona PostgreSQL o SQL Server en el composition root.

## Ola 3 — Reconciliación arquitectónica

- [x] **TASK-ARC-001 Corregir dirección de dependencias**
  - Eliminada la referencia Application → Persistence.
  - Añadida la referencia Persistence → Application.
  - Architecture tests: 8 aprobados.

- [x] **TASK-ARC-002 Eliminar duplicados y placeholders**
  - Result duplicado y Pagination sin consumo retirados de Shared.
  - Repositorios específicos y métodos sin implementación retirados del contrato actual.

- [x] **TASK-API-001 Conservar operación vertical mínima**
  - La creación de tickets valida API → MediatR → Application → Persistence.
  - El resto del ciclo funcional queda para specs posteriores.

## Ola 4 — Validación y evidencia

- [x] **TASK-VAL-001 Ejecutar checks equivalentes a CI**
  - `dotnet tool restore`: correcto.
  - `dotnet restore InternalOperations.slnx --locked-mode`: correcto.
  - `dotnet format InternalOperations.slnx --verify-no-changes --no-restore`: correcto.
  - `dotnet build InternalOperations.slnx --configuration Release --no-restore -p:ContinuousIntegrationBuild=true`: 0 warnings, 0 errores.
  - `dotnet test InternalOperations.slnx --configuration Release --no-build --no-restore`: 17 aprobados, 0 fallidos.

## Salida alcanzada

- Application contiene los contratos y componentes transversales de fase 1.
- Persistence implementa los puertos sin invertir dependencias.
- La composición vertical mínima está operativa.
- El repositorio supera los checks locales equivalentes a Backend CI.
- La siguiente feature debe comenzar con una spec nueva en estado `Proposed`.
