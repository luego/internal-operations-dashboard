# 005 — Application and Persistence Foundation: Tasks

**Estado:** Proposed  
**Fecha:** 4 de agosto de 2026

## Convenciones

- No se debe implementar código de negocio ni identity mientras esta spec esté en estado `Proposed`.
- Cada tarea requiere verificación por prueba o compilación.
- Si una tarea cambia arquitectura, seguridad o contrato, debe revisarse antes de continuar.

## Gate 1 — Aprobación de la spec

- [ ] **GATE-APP-000 Aprobar requirements, design y tasks**
  - Confirmar que el alcance corresponde exactamente a la fase 1 del baseline.
  - Verificar que no hay identidad, policies ni feature business en el alcance.
  - Cambiar los tres artefactos a `Approved` antes de implementar código.

## Ola 1 — Cross-cutting de aplicación

- [ ] **TASK-APP-001 Implementar `Result`, `Error` y tipos de error**
  - Crear tipos con `Code`, `Message` y `Type`.
  - Añadir factories para validation, not found, conflict, unauthorized, forbidden y failure.
  - Verificar con pruebas unitarias.

- [ ] **TASK-APP-002 Definir `IClock` e `ICurrentUser`**
  - Crear interfaces pequeñas y neutralizadas sobre ASP.NET Core.
  - Preparar el punto de inyección para fases posteriores.

- [ ] **TASK-APP-003 Configurar MediatR mínimo**
  - Añadir la dependencia a Application.
  - Preparar el pipeline base se usa en handlers sin acoplarse a API.

## Ola 2 — Persistencia base

- [ ] **TASK-PER-001 Crear `ApplicationDbContext`**
  - Configurar `DbContext` mínimo con extensibilidad para futuras entidades.
  - Mantener la dependencia de EF Core solo en Persistence.

- [ ] **TASK-PER-002 Implementar repositorio genérico y Unit of Work**
  - Crear `IRepository<T>`, `GenericRepository<T>`, `IUnitOfWork` y `UnitOfWork`.
  - Verificar que el `UnitOfWork` confirma cambios en memoria.

- [ ] **TASK-PER-003 Preparar provider dual**
  - Asegurar que la capa de persistencia puede resolver PostgreSQL/SQL Server por configuración.
  - Mantener la same API para application.

## Ola 3 — Validación

- [ ] **TASK-VAL-001 Ejecutar pruebas de aplicación y persistencia**
  - Verificar que `dotnet test` para los proyectos de fase 1 pasa.
  - Revisar que no haya regresiones de arquitectura.

## Salida esperada

- `Application` tiene la base transversal para futuras features.
- `Persistence` ofrece la base mínima para repositorios y Unit of Work.
- La próxima spec de Identity queda bloqueada hasta cerrar esta fase.
