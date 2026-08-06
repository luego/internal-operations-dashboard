# 005 — Application and Persistence Foundation: Requirements

**Estado:** Completed
**Fecha:** 4 de agosto de 2026
**Completada:** 6 de agosto de 2026
**Basado en:** Fase 1 del documento maestro: Cross-cutting y persistencia
**Límite:** Esta spec no incluye identidad, autenticación ni CRUD funcional; conserva una creación mínima de ticket para validar la composición vertical.

## 1. Objetivo

Establecer la base transversal de Application y Persistence para permitir casos de uso cohesivos, resultados tipados y acceso a datos portable entre proveedores, respetando los límites de Clean Architecture.

## 2. Alcance

Incluye:

- Result pattern y catálogo de errores para errores esperados;
- MediatR y pipeline behavior mínimo;
- validación y configuración básica de mapping;
- abstracciones de reloj y usuario actual;
- puertos de persistencia propiedad de Application;
- DbContext, repositorio genérico y Unit of Work implementados por Persistence;
- selección de proveedor PostgreSQL/SQL Server por configuración;
- pruebas unitarias, arquitectónicas y de integración;
- una operación mínima de creación de ticket para comprobar la composición API → Application → Persistence.

No incluye:

- autenticación, autorización o Identity;
- CRUD completo de departamentos, usuarios o tickets;
- asignación, cierre, búsqueda o eliminación de tickets;
- migraciones funcionales de dominio;
- frontend.

## 3. Requisitos funcionales

### REQ-APP-001 Result y errores tipados

- El sistema debe devolver `Result` y `Result<T>` con errores tipados y códigos estables.
- Debe existir un catálogo para validation, not found, conflict, unauthorized, forbidden y failure.
- Application debe ser la única fuente de verdad de `Result`, `Error` y `ErrorType`.
- Un resultado fallido debe exponer la colección de errores y su primer error mediante `Error`.

### REQ-APP-002 Pipeline de aplicación

- Application debe recibir comandos y consultas mediante MediatR.
- Debe existir un pipeline base para validación.
- Los handlers deben coordinar casos de uso sin duplicar acceso a datos.

### REQ-APP-003 Validación y mapping básicos

- La validación debe permanecer fuera de API y Persistence.
- Debe existir mapping explícito entre DTOs, entidades y resultados.

### REQ-APP-004 Contexto transversal

- `IClock` e `ICurrentUser` deben pertenecer a Application.
- Application no debe depender de ASP.NET Core para consumirlas.

### REQ-PER-001 Persistencia base

- Debe existir un `ApplicationDbContext` central.
- `IRepository<T>` e `IUnitOfWork` deben pertenecer a Application.
- `GenericRepository<T>` y `UnitOfWork` deben implementarse en Persistence.
- `UnitOfWork` debe encapsular la confirmación de cambios.

### REQ-PER-002 Proveedor dual

- La API debe seleccionar PostgreSQL o SQL Server por configuración.
- El proveedor no debe alterar los contratos ni casos de uso de Application.

### REQ-PER-003 Contratos y pruebas

- Las pruebas de Persistence deben verificar operaciones reales del `DbContext` y `UnitOfWork`.
- Las abstracciones públicas deben tener consumo real y no quedar como placeholders.

### REQ-API-001 Operación vertical mínima

- `POST /api/v1/tickets` debe atravesar API, MediatR, Application y Persistence.
- Esta operación valida la composición de fase 1 y no habilita CRUD completo.

## 4. Requisitos no funcionales

### REQ-APP-NF-001 Dependencias

Application no debe depender de Api, Infrastructure ni Persistence. Persistence debe depender de Application para implementar sus puertos.

### REQ-PER-NF-001 Portabilidad

Persistence debe admitir PostgreSQL y SQL Server mediante la misma API de repositorios.

### REQ-APP-NF-002 Testabilidad

Los componentes deben probarse sin infraestructura externa completa mediante unit tests, architecture tests e integración en memoria.

### REQ-APP-NF-003 Mantenibilidad

No deben existir Result patterns duplicados, contratos públicos sin consumo ni métodos públicos que lancen `NotImplementedException`.

## 5. Criterios de aceptación

- restore locked, format, build CI y tests pasan;
- el build estricto termina con 0 warnings y 0 errores;
- las pruebas arquitectónicas confirman la dirección de dependencias;
- los puertos de persistencia son propiedad de Application;
- la documentación describe la fase y estructura reales.
