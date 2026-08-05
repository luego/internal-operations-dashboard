# 005 — Application and Persistence Foundation: Design

**Estado:** Proposed  
**Requisitos:** `requirements.md`  
**Fecha:** 4 de agosto de 2026

## 1. Resumen

Esta spec crea la base transversal del backend antes de la identidad. La solución introduce resultados tipados, pipeline mínimo de MediatR, abstracciones de tiempo y usuario actual, además de la persistencia base con DbContext, repositorio genérico y Unit of Work.

## 2. Contexto

La fase 0 ya dejó resueltos los límites de proyecto y la configuración del repositorio. Ahora se necesita una capa ejecutable que permita construir casos de uso y acceso a datos sin introducir la identidad ni feature business.

## 3. Decisiones de diseño

### DES-APP-001 Result pattern

Se implementará un `Result` base con `Result<T>` para casos exitosos y fallidos. Los errores tendrán un `ErrorType` y una estructura con `Code`, `Message` y `Type`.

Esto permite que los handlers y servicios regresen errores esperados de forma consistente antes de mapearse a HTTP.

### DES-APP-002 MediatR y pipeline mínimo

MediatR se añadirá a la capa `Application` como dependencia principal. La implementación mínima incluye:

- `IRequest<TResponse>` / `IRequest` para comandos y consultas;
- un behavior base para validación preliminar;
- una separación clara entre handler y service.

La capa `Application` sigue sin depender de `Api`, `Infrastructure` ni `Persistence`.

### DES-APP-003 Contexto transversal

Se introducirán interfaces pequeñas para:

- `IClock` para obtener la hora actual;
- `ICurrentUser` para obtener el actor activo.

Estas interfaces serán manejadas por la composición root en una fase posterior, pero ya se dejarán en la capa correcta para preparar la base del siguiente alcance.

### DES-PER-001 DbContext y repositorio

La capa de persistencia incluirá:

- `ApplicationDbContext` como punto de entrada a EF Core;
- `IRepository<T>` y `GenericRepository<T>`;
- `IUnitOfWork` y `UnitOfWork`.

La implementación será mínima, portable y preparada para que cada caso de uso comparta la misma abstracción sin acoplarse a EF Core.

### DES-PER-002 Provider strategy

La infraestructura de persistencia seleccionará PostgreSQL o SQL Server por configuración, sin cambiar los contratos de aplicación. El patrón se deja preparado para que el provider se resuelva en el composition root de la API.

### DES-PER-003 Testability

La misión de estas pruebas será validar que:

- `Result` y `Error` responden correctamente a casos ok/failure;
- el `DbContext` salva cambios con un `UnitOfWork` real;
- la integración de la persistencia base funciona en memoria sin infraestructura adicional.

## 4. Límite de capas

```text
Api -> Application -> Domain
          \-> Shared
Persistence -> Application + Domain + Shared
Infrastructure -> Application + Shared
```

La nueva spec no introduce dependencias hacia `Api` desde `Application`, ni `Application` hacia `Persistence` a nivel de implementación directa. La infraestructura puede resolver el provider y el repositorio real desde la composición.

## 5. Riesgos y mitigaciones

- **Sobreabstracción:** solo se introduce lo necesario para la base transversal.
- **Acoplamiento accidental:** las interfaces de contexto se mantienen mínimas y neutralizadas frente a ASP.NET Core.
- **Provider-specific drift:** se deja el punto de extensión por configuración, no por tipos concretos en Application.
