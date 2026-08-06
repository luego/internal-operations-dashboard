# 005 — Application and Persistence Foundation: Design

**Estado:** Completed
**Requisitos:** `requirements.md`
**Fecha:** 4 de agosto de 2026
**Completada:** 6 de agosto de 2026

## 1. Resumen

La fase 1 establece resultados tipados, MediatR, validación, mapping y puertos de persistencia en Application. Persistence implementa esos puertos con EF Core, proveedor configurable y una unidad de trabajo mínima. Una creación de ticket valida la composición completa sin ampliar el alcance a CRUD.

## 2. Dirección de dependencias

```text
Api -> Application -> Domain
 |         \-> Shared
 |-> Infrastructure -> Application + Shared
 \-> Persistence -> Application + Domain + Shared
```

Reglas:

- Domain no depende de frameworks ni capas externas.
- Application no referencia Api, Infrastructure ni Persistence.
- Application es propietaria de los puertos que necesita.
- Persistence conoce Application únicamente para implementar esos puertos.
- Api actúa como composition root.

Las pruebas de arquitectura verifican tanto referencias de proyectos como dependencias de ensamblados.

## 3. Decisiones de diseño

### DES-APP-001 Result pattern

`src/InternalOperations.Application/Result.cs` es la única fuente de verdad para `Result`, `Result<T>`, `Error` y `ErrorType`. Un fallo conserva todos sus errores y expone el primero en `Error` para el mapeo HTTP.

### DES-APP-002 MediatR y validación

Los comandos implementan `IRequest<TResponse>`. Los handlers coordinan el servicio de aplicación y `ValidationBehavior` ejecuta validadores antes del handler. La API no contiene reglas de aplicación.

### DES-APP-003 Contexto transversal

`IClock` e `ICurrentUser` son interfaces pequeñas de Application, sin dependencias de ASP.NET Core.

### DES-PER-001 Puertos y adaptadores

Puertos en Application:

- `Abstractions/Persistence/IRepository.cs`;
- `Abstractions/Persistence/IUnitOfWork.cs`.

Adaptadores en Persistence:

- `ApplicationDbContext`;
- `GenericRepository<T>`;
- `UnitOfWork`.

`IUnitOfWork` solo confirma cambios. Los repositorios se inyectan directamente en los servicios que los consumen, evitando contratos específicos sin comportamiento real.

### DES-PER-002 Estrategia de proveedor

La composición lee `Database:Provider` y configura `UseNpgsql` o `UseSqlServer`. El proveedor y connection string permanecen fuera de Application.

### DES-PER-003 Testabilidad

- Application unit tests verifican Result y pipeline.
- Architecture tests verifican límites de capas.
- Persistence integration tests usan EF Core InMemory para comprobar repositorio y Unit of Work.
- API integration tests validan el arranque y la composición HTTP.

## 4. Operación vertical mínima

```text
POST /api/v1/tickets
  -> TicketsController
  -> CreateTicketCommand
  -> CreateTicketCommandHandler
  -> ITicketService / TicketService
  -> IRepository<Ticket> + IUnitOfWork
  -> GenericRepository<Ticket> + ApplicationDbContext
```

Solo `CreateAsync` forma parte de esta fase. Consultar, asignar, cerrar y eliminar tickets requieren specs funcionales posteriores.

## 5. Riesgos y mitigaciones

- **Acoplamiento accidental:** tests arquitectónicos protegen las referencias exactas.
- **Sobreabstracción:** se eliminaron contratos específicos y placeholders sin consumo.
- **Duplicación transversal:** Result tiene un único propietario.
- **Drift por proveedor:** Application solo consume puertos neutrales.
- **Drift documental:** requirements, design, tasks y README se sincronizan al cerrar la fase.
