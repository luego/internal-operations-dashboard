# 005 — Application and Persistence Foundation: Requirements

**Estado:** Proposed  
**Fecha:** 4 de agosto de 2026  
**Basado en:** Fase 1 del documento maestro: Cross-cutting y persistencia  
**Límite:** Esta spec no incluye identidad, autenticación ni feature business.

## 1. Objetivo

Establecer la base transversal de la aplicación y la capa de persistencia para permitir casos de uso cohesivos, resultados tipados y acceso a datos portable entre proveedores.

## 2. Alcance

Incluye:

- Result pattern y catálogo de errores para errores esperados;
- MediatR y pipeline behavior mínimo para casos de uso;
- validación y configuración básica de mapping;
- abstracciones de reloj y usuario actual;
- DbContext, repositorio genérico y Unit of Work;
- selección de proveedor PostgreSQL/SQL Server por configuración;
- pruebas de contrato para la capa de aplicación y persistencia.

No incluye:

- autenticación/authorization identity;
- endpoints o controllers de negocio;
- CRUD de departamentos, usuarios o tickets;
- migraciones de dominio funcional;
- frontend.

## 3. Requisitos funcionales

### REQ-APP-001 Result y errores tipados

- El sistema debe devolver `Result` y `Result<T>` con errores tipados y códigos estables.
- Debe existir un catálogo de errores para validation, not found, conflict, unauthorized, forbidden y failure.
- Los errores esperados deben seguir una estructura consistente incluso cuando un caso de uso falla antes de llegar a la API.

### REQ-APP-002 Pipeline de aplicación

- La aplicación debe permitir la entrada de comandos y consultas a través de MediatR.
- Debe existir un pipeline base para validación y comportamiento transversal mínimo.
- Los handlers deben coordinar casos de uso sin duplicar lógica de persistencia.

### REQ-APP-003 Validación y mapping básicos

- Debe existir un patrón para validar entidades o comandos de entrada.
- Debe existir una capa simple de mapeo para DTOs, entidades o resultados de aplicación.
- La validación debe permanecer fuera de API y persistencia.

### REQ-APP-004 Cross-cutting de contexto

- Debe existir un mecanismo mínimo para obtener la hora actual y el usuario actual del contexto de ejecución.
- El código de aplicación debe poder consumir esas dependencias sin depender de ASP.NET Core directamente.

### REQ-PER-001 Persistencia base

- Debe existir un `ApplicationDbContext` central para la infraestructura de persistencia.
- Debe existir un repositorio genérico mínimo para operaciones CRUD comunes.
- Debe existir un `UnitOfWork` que encapsule la confirmación del cambio.

### REQ-PER-002 Proveedor dual

- La solución debe permitir seleccionar PostgreSQL o SQL Server por configuración sin cambiar casos de uso de aplicación.
- La capa de persistencia debe encapsular el proveedor y mantener la misma interfaz de repositorio.

### REQ-PER-003 Contratos de persistencia

- Las pruebas del proyecto de persistencia deben verificar la operación real del `DbContext` y del `UnitOfWork`.
- La persistencia debe operar con una estrategia neutral al proveedor en la capa de aplicación.

## 4. Requisitos no funcionales

### REQ-APP-NF-001 Dependencias

La capa de aplicación no debe depender de Api, Infrastructure ni Persistence.

### REQ-PER-NF-001 Portabilidad

El diseño de persistencia debe admitir PostgreSQL y SQL Server con la misma API de repositorios.

### REQ-APP-NF-002 Testabilidad

Los nuevos componentes deben poder probarse en unit tests e integration tests sin infraestructura externa completa.

### REQ-APP-NF-003 Mantenibilidad

Las abstracciones creadas deben tener un consumo real en la solución y no quedar como placeholders sin uso.

## 5. Supuestos y decisiones no bloqueantes

- La capa funcional de Identity queda posterior a esta spec.
- El Result pattern se implementa sin forzar todavía un controlador completo.
- La persistencia usa EF Core con una base de datos en memoria para verificación mínima local.
- El objetivo es dejar la base de contratos y composición transversal para las siguientes feature specs.
