# Internal Operations Dashboard

## Especificación técnica maestra para desarrollo backend-first con Codex

**Estado:** Baseline v1.0  
**Fecha:** 4 de agosto de 2026  
**Destino:** Codex y revisión técnica humana  
**Alcance actual:** Backend únicamente; el frontend se especificará después  
**Método:** Spec-Driven Development inspirado en Kiro

---

## 1. Propósito del documento

Este documento es la fuente de verdad inicial para construir el backend de **Internal Operations Dashboard**, una aplicación interna para gestionar tickets operativos, usuarios, departamentos, comentarios, historial de cambios y métricas de operación.

Codex deberá trabajar por especificaciones versionadas, no por una implementación monolítica basada únicamente en este documento. Cada incremento debe producir y mantener tres artefactos: `requirements.md`, `design.md` y `tasks.md`. El código se implementará después de que los artefactos correspondientes sean coherentes y aprobados.

El objetivo arquitectónico es una base empresarial reutilizable y comprensible, con separación de responsabilidades, pruebas automatizadas y capacidad real de cambiar entre PostgreSQL y SQL Server mediante configuración y migraciones específicas, sin reescribir los casos de uso ni el dominio.

### 1.1 Resultado esperado

Al terminar la fase backend deberá existir:

- Una API REST en ASP.NET Core sobre .NET 10 LTS.
- Una solución organizada mediante Clean Architecture con puertos y adaptadores.
- CRUD y flujos operativos de tickets, departamentos, usuarios, comentarios e historial.
- Autenticación y autorización basada en políticas.
- Persistencia intercambiable entre PostgreSQL y SQL Server.
- Contenedores y Docker Compose para desarrollo local y pruebas.
- Contrato OpenAPI, manejo global de errores y respuestas consistentes.
- Observabilidad, health checks y logs estructurados.
- Pruebas unitarias, de integración, arquitectura y contrato HTTP.
- Specs y decisiones arquitectónicas versionadas junto al código.

### 1.2 Fuera de alcance por ahora

- Aplicación web o móvil.
- Diseño visual, componentes UI y estado del frontend.
- Integraciones empresariales no confirmadas, como Slack, Teams, correo o SSO externo.
- Microservicios, event bus distribuido, Kubernetes y multi-tenancy.
- Analítica avanzada, IA, SLA predictivo y reportes programados.

Estas capacidades podrán añadirse mediante nuevas specs sin alterar el núcleo del backend.

---

## 2. Decisiones ejecutivas

### 2.1 Versión de .NET

**Decisión: usar .NET 10 LTS, ASP.NET Core 10, C# 14 y Entity Framework Core 10.**

Razones:

1. .NET 10 es la versión LTS activa y tiene soporte de Microsoft hasta el 14 de noviembre de 2028.
2. .NET 8 y .NET 9 finalizan soporte el 10 de noviembre de 2026. Empezar un proyecto nuevo con cualquiera de ellas obligaría a una migración casi inmediata.
3. .NET 10 ofrece una ventana de mantenimiento adecuada para una base reutilizable y permite usar el SDK, ASP.NET Core y EF Core de la misma generación.
4. No se usarán previews de .NET 11 ni dependencias prerelease en la rama principal.

Reglas de versión:

- `TargetFramework`: `net10.0`.
- Fijar el SDK mediante `global.json`, permitiendo solo el roll-forward de parches compatible.
- Mantener el runtime en el último parche de seguridad de .NET 10.
- Centralizar versiones NuGet con `Directory.Packages.props`.
- Activar `Nullable`, `ImplicitUsings`, análisis estático y warnings como errores en CI; las excepciones justificadas se documentarán.

### 2.2 Estilo arquitectónico

**Decisión: Clean Architecture pragmática con Ports and Adapters**, complementada con Repository, GenericRepository, Unit of Work, Service Layer, MediatR y Result Pattern.

La regla principal es que las dependencias apunten hacia el núcleo:

```text
API -> Application -> Domain
API -> Infrastructure
API -> Persistence
Infrastructure -> Application
Persistence -> Application + Domain
Shared <- solo primitivas transversales estables
```

`Domain` no referencia ASP.NET Core, Entity Framework, MediatR, AutoMapper ni proveedores de base de datos.

### 2.3 Límite entre MediatR, handlers y services

Para evitar duplicidad se establece esta responsabilidad:

- **Controllers:** traducen HTTP a requests, envían el request a MediatR y convierten `Result` a HTTP. No contienen lógica de negocio.
- **MediatR handlers:** son el punto de entrada de cada caso de uso. Coordinan autorización contextual, validación, servicios, repositorios y transacciones.
- **Application services:** encapsulan operaciones de negocio reutilizables por más de un handler o suficientemente complejas para tener una interfaz estable. No se creará un service que solo copie cada método del repositorio.
- **Domain:** mantiene invariantes y transiciones de estado dentro de entidades/value objects.
- **Repositories:** expresan acceso a persistencia. El repositorio genérico cubre operaciones comunes y los repositorios específicos contienen consultas o comandos propios del agregado.

Todos los repositorios y servicios públicos tendrán interfaz. No se crearán interfaces vacías o sin un consumidor sustituible.

### 2.4 Principios no negociables

- Ninguna respuesta esperada del negocio se modela como excepción.
- Ningún controller contiene `try/catch` general.
- Ningún caso de uso depende de `DbContext`, Npgsql o SQL Server directamente.
- Ninguna API devuelve entidades de EF Core.
- Ningún repositorio expone `IQueryable` fuera de Persistence.
- Ningún secreto se almacena en Git, imágenes Docker o archivos de configuración versionados.
- Las migraciones de PostgreSQL y SQL Server se mantienen separadas.
- Toda tarea implementada debe rastrearse a requisitos y criterios de aceptación.

---

## 3. Método Spec-Driven Development

El proceso se inspira en el modelo de Kiro: requisitos estructurados, diseño técnico y tareas ejecutables. Las specs vivirán junto al código y se tratarán como artefactos versionados.

### 3.1 Estructura de specs

```text
specs/
├── 000-solution-foundation/
│   ├── requirements.md
│   ├── design.md
│   └── tasks.md
├── 010-identity-and-access/
│   ├── requirements.md
│   ├── design.md
│   └── tasks.md
├── 020-departments-and-users/
├── 030-ticket-management/
├── 040-comments-and-history/
├── 050-dashboard-queries/
├── 060-observability-and-hardening/
└── 070-release-readiness/
```

Las decisiones transversales se registrarán en:

```text
docs/adr/
├── 0001-use-dotnet-10-lts.md
├── 0002-clean-architecture-boundaries.md
├── 0003-mediator-service-responsibilities.md
├── 0004-dual-database-provider-strategy.md
└── 0005-result-and-problem-details.md
```

### 3.2 Flujo obligatorio por spec

1. **Requirements:** definir comportamiento, restricciones, casos límite y criterios de aceptación.
2. **Requirements review:** detectar ambigüedades, contradicciones, requisitos no verificables y dependencias no declaradas.
3. **Design:** establecer componentes, contratos, modelo de datos, seguridad, errores, observabilidad y pruebas.
4. **Design review:** comprobar límites arquitectónicos, portabilidad de base de datos y trazabilidad.
5. **Tasks:** dividir el diseño en unidades pequeñas, ordenadas y comprobables, indicando dependencias.
6. **Implementation:** ejecutar una tarea a la vez o por olas de tareas independientes.
7. **Verification:** correr los checks definidos y adjuntar evidencia resumida.
8. **Sync:** actualizar checkboxes, decisiones y cualquier desviación antes de cerrar la spec.

Codex no debe cambiar silenciosamente un requisito para acomodar una implementación. Si descubre un conflicto, detendrá esa tarea, propondrá el cambio en la spec y esperará aprobación cuando la decisión altere alcance o comportamiento.

### 3.3 Formato de requisitos

Cada requisito tendrá un identificador estable y criterios en notación EARS adaptada:

```markdown
### REQ-TKT-004 Cambiar el estado de un ticket

**Historia:** Como agente autorizado, quiero cambiar el estado de un ticket
para reflejar su progreso operativo.

#### Criterios de aceptación

1. WHEN un agente autorizado solicita una transición válida
   THE SYSTEM SHALL guardar el nuevo estado y registrar el cambio en el historial.
2. WHEN la transición no está permitida por la máquina de estados
   THE SYSTEM SHALL devolver un Result de conflicto sin modificar datos.
3. IF otro proceso modificó el ticket desde su lectura
   THEN THE SYSTEM SHALL rechazar la escritura con un conflicto de concurrencia.

**Trazabilidad:** DES-TKT-003; TASK-TKT-012..015; TEST-TKT-021..027
```

### 3.4 Definition of Ready para una spec

Una spec está lista para implementar cuando:

- El alcance y lo que queda fuera están explícitos.
- Cada requisito tiene identificador y al menos un criterio verificable.
- Las reglas de autorización están descritas.
- Los errores esperados y su semántica HTTP están definidos.
- El diseño contiene contratos, datos, migraciones y estrategia de pruebas.
- Las tareas tienen resultado observable, dependencias y referencias a requisitos.
- No hay preguntas abiertas que puedan cambiar el modelo público o los datos.

### 3.5 Definition of Done por tarea

- Código compilado sin warnings nuevos.
- Tests asociados en verde para PostgreSQL y, cuando aplique, SQL Server.
- Sin violaciones de arquitectura.
- OpenAPI actualizado si cambió el contrato.
- Logs sin datos sensibles.
- Migración creada y verificada si cambió el esquema.
- Documentación y spec sincronizadas.
- Checklist de la tarea actualizado con evidencia breve.

---

## 4. Alcance funcional del backend

### 4.1 Actores y roles iniciales

- **Administrator:** administra usuarios, roles, departamentos y configuración operativa.
- **Manager:** consulta métricas, administra asignaciones y supervisa tickets de los departamentos permitidos.
- **Agent:** crea, consulta y actualiza tickets según sus permisos; agrega comentarios.
- **Viewer:** consulta información autorizada sin modificarla.

La autorización se implementará mediante policies, no mediante comparaciones de strings dentro de controllers.

### 4.2 Módulos

#### Identidad y acceso

- Inicio de sesión.
- Emisión y renovación segura de tokens.
- Cierre/revocación de sesión.
- Administración de usuarios activos/inactivos.
- Roles y policies.
- Cambio de contraseña y bloqueo básico por intentos fallidos.

#### Departamentos y usuarios

- CRUD de departamentos.
- Asignación de usuarios a departamento.
- Activación/desactivación lógica.
- Listados paginados con filtros.

#### Tickets

- Creación y consulta.
- Actualización de título, descripción, prioridad y asignación según permisos.
- Máquina de estados validada por dominio.
- Filtros, ordenación y paginación.
- Concurrencia optimista.
- Soft delete solo si el requisito funcional lo justifica; por defecto, los tickets se cierran o archivan y no se eliminan físicamente.

#### Comentarios e historial

- Comentarios asociados a tickets.
- Edición limitada y auditada según política.
- Historial inmutable de transiciones y cambios relevantes.
- No exponer notas internas a consumidores no autorizados.

#### Dashboard

- Conteos por estado, prioridad, departamento y agente.
- Tickets creados, resueltos y vencidos por rango de fechas.
- Tiempo medio de resolución cuando existan datos suficientes.
- Consultas de solo lectura optimizadas y paginadas cuando devuelvan detalle.

### 4.3 Requisitos funcionales baseline

- **REQ-AUTH-001:** WHEN se presentan credenciales válidas, THE SYSTEM SHALL emitir un access token de corta duración y un refresh token rotatorio protegido.
- **REQ-AUTH-002:** WHEN un usuario está inactivo o bloqueado, THE SYSTEM SHALL rechazar la autenticación sin revelar si el identificador existe.
- **REQ-AUTH-003:** WHEN un token no contiene la policy requerida, THE SYSTEM SHALL devolver 403 y no ejecutar el caso de uso.
- **REQ-DEP-001:** WHEN un administrador crea un departamento con nombre único válido, THE SYSTEM SHALL persistirlo y devolver su representación DTO.
- **REQ-DEP-002:** WHEN se intenta desactivar un departamento con trabajo activo, THE SYSTEM SHALL aplicar la regla definida en la spec del módulo y nunca dejar referencias inválidas.
- **REQ-USR-001:** WHEN un administrador asigna un usuario a un departamento existente, THE SYSTEM SHALL guardar la relación de forma transaccional.
- **REQ-TKT-001:** WHEN un usuario autorizado envía datos válidos, THE SYSTEM SHALL crear un ticket con identificador, estado inicial, auditoría y versión de concurrencia.
- **REQ-TKT-002:** WHEN se consulta una colección, THE SYSTEM SHALL aplicar paginación limitada, filtros permitidos y orden determinista.
- **REQ-TKT-003:** WHEN un ticket no existe o no es visible para el actor, THE SYSTEM SHALL responder de acuerdo con la política de no divulgación definida, preferiblemente 404.
- **REQ-TKT-004:** WHEN se solicita una transición válida, THE SYSTEM SHALL actualizar el estado y crear una entrada de historial en la misma transacción.
- **REQ-TKT-005:** WHEN una escritura usa una versión obsoleta, THE SYSTEM SHALL devolver 409 y no sobrescribir cambios ajenos.
- **REQ-CMT-001:** WHEN un actor autorizado agrega un comentario válido, THE SYSTEM SHALL persistirlo, auditar autor y fecha, y devolver 201.
- **REQ-HIS-001:** WHEN ocurre un cambio auditable, THE SYSTEM SHALL registrar actor, fecha UTC, tipo de evento y datos mínimos sin guardar secretos.
- **REQ-DSH-001:** WHEN un manager solicita métricas con un rango válido, THE SYSTEM SHALL calcularlas solo sobre datos que pueda consultar.

Los detalles de cada requisito se ampliarán en la spec de su módulo antes de implementar.

---

## 5. Requisitos no funcionales

### 5.1 Rendimiento y capacidad

- Las operaciones CRUD normales deberán tener un objetivo inicial de p95 inferior a 500 ms en el entorno de referencia, excluyendo latencia de red externa.
- Las consultas de dashboard tendrán un objetivo inicial de p95 inferior a 1.5 s con el conjunto de datos de prueba acordado.
- Toda colección será paginada. Tamaño por defecto: 25; máximo: 100.
- No se permitirán consultas N+1 en rutas críticas.
- Se usarán proyecciones a DTO y `AsNoTracking` para lecturas sin modificación.
- Los límites se validarán con una prueba de rendimiento reproducible antes del release, no como garantía abstracta.

### 5.2 Fiabilidad y datos

- Fechas almacenadas en UTC; la presentación en zona horaria pertenece al consumidor.
- Escrituras multientidad ejecutadas dentro de Unit of Work.
- Concurrencia optimista en agregados modificables.
- Índices definidos a partir de patrones reales de consulta.
- Migraciones reversibles cuando sea viable; todo cambio destructivo exige plan de respaldo y despliegue.
- El proveedor seleccionado deberá arrancar desde una base vacía y superar el mismo conjunto de pruebas de contrato de persistencia.

### 5.3 Compatibilidad de API

- Rutas versionadas bajo `/api/v1`.
- OpenAPI es el contrato consumible.
- Cambios incompatibles requieren nueva versión o estrategia de transición documentada.
- JSON en `camelCase`; fechas ISO 8601 UTC; identificadores GUID.
- Nulos y campos opcionales deben estar reflejados correctamente en el esquema.

### 5.4 Mantenibilidad

- Métodos y tipos con nombres de dominio claros.
- Sin dependencia cíclica entre proyectos.
- Sin acceso directo de API a Persistence.
- Complejidad accidental justificada mediante ADR.
- Paquetes actualizados y auditados de forma periódica.

---

## 6. Estructura de la solución

```text
InternalOperationsDashboard/
├── .config/
│   └── dotnet-tools.json
├── .github/workflows/
│   └── backend-ci.yml
├── docs/
│   ├── adr/
│   ├── api/
│   └── runbooks/
├── specs/
├── src/
│   ├── InternalOperations.Api/
│   ├── InternalOperations.Application/
│   ├── InternalOperations.Domain/
│   ├── InternalOperations.Infrastructure/
│   ├── InternalOperations.Persistence/
│   └── InternalOperations.Shared/
├── tests/
│   ├── InternalOperations.Domain.UnitTests/
│   ├── InternalOperations.Application.UnitTests/
│   ├── InternalOperations.Persistence.IntegrationTests/
│   ├── InternalOperations.Api.IntegrationTests/
│   └── InternalOperations.ArchitectureTests/
├── .dockerignore
├── .editorconfig
├── .env.example
├── Directory.Build.props
├── Directory.Packages.props
├── docker-compose.yml
├── docker-compose.override.yml
├── global.json
├── InternalOperations.slnx
└── README.md
```

### 6.1 InternalOperations.Domain

Contiene entidades, value objects, domain events estrictamente útiles, excepciones de invariantes imposibles y reglas del negocio.

```text
Domain/
├── Common/
│   ├── Entity.cs
│   ├── AggregateRoot.cs
│   └── IHasDomainEvents.cs
├── Tickets/
│   ├── Ticket.cs
│   ├── TicketComment.cs
│   ├── TicketHistoryEntry.cs
│   ├── TicketStatus.cs
│   ├── TicketPriority.cs
│   └── Events/
├── Departments/
└── Users/
```

Los enums que expresan negocio, como `TicketStatus`, permanecen en Domain. Shared se reserva para primitivas técnicas estables; mover todos los enums a Shared debilitaría el lenguaje del dominio.

### 6.2 InternalOperations.Application

Contiene casos de uso, puertos, DTOs, mappings, behaviors y resultados.

```text
Application/
├── Abstractions/
│   ├── Persistence/
│   │   ├── IRepository.cs
│   │   ├── ITicketRepository.cs
│   │   ├── IDepartmentRepository.cs
│   │   └── IUnitOfWork.cs
│   ├── Security/
│   ├── Services/
│   ├── Time/IClock.cs
│   └── Observability/
├── Common/
│   ├── Behaviors/
│   ├── Errors/
│   ├── Mappings/
│   ├── Models/
│   └── Pagination/
└── Features/
    ├── Auth/
    ├── Departments/
    ├── Users/
    ├── Tickets/
    └── Dashboard/
```

Cada feature se organiza verticalmente por caso de uso dentro de Application, por ejemplo `Tickets/CreateTicket`, sin abandonar las capas de la solución.

### 6.3 InternalOperations.Persistence

Contiene EF Core, configuración de entidades, implementaciones de repositories, Unit of Work, migraciones y factories de proveedor.

```text
Persistence/
├── Context/
├── Configurations/
├── Repositories/
├── Interceptors/
├── Migrations/
│   ├── PostgreSql/
│   └── SqlServer/
├── Providers/
└── DependencyInjection.cs
```

### 6.4 InternalOperations.Infrastructure

Implementa identidad, tokens, hashing, clock, almacenamiento externo futuro, correo futuro, telemetry exporters y otras adaptaciones ajenas a EF Core.

### 6.5 InternalOperations.Shared

Incluye componentes pequeños y estables compartidos por varias capas, por ejemplo constantes técnicas, guard clauses sin dominio, `Result`, `Error`, `ErrorType` y metadatos de paginación si su ubicación evita dependencias circulares.

No será un cajón de utilidades. Domain no deberá depender de Shared si Shared incorpora conceptos de Application o infraestructura.

### 6.6 InternalOperations.Api

Contiene controllers, middleware/handlers HTTP, autenticación ASP.NET Core, versionado, OpenAPI, composición de dependencias y configuración del host.

---

## 7. Diseño de componentes

### 7.1 Result Pattern

Los casos esperados retornan `Result` o `Result<T>` con un error tipado:

```csharp
public sealed record Error(string Code, string Description, ErrorType Type);

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Failure
}
```

Mapeo HTTP baseline:

| ErrorType | HTTP | Uso |
|---|---:|---|
| Validation | 400 | Entrada semánticamente inválida |
| Unauthorized | 401 | Autenticación ausente o inválida |
| Forbidden | 403 | Actor autenticado sin permiso |
| NotFound | 404 | Recurso no encontrado o no visible |
| Conflict | 409 | Estado, unicidad o concurrencia |
| Failure | 422 o 400 | Regla de negocio esperada, según contrato |

Los errores HTTP se serializan como `ProblemDetails`/`ValidationProblemDetails` con `type`, `title`, `status`, `detail`, `instance`, `traceId`, `errorCode` y, cuando aplique, errores por campo.

### 7.2 Global exception handler

Se implementará `IExceptionHandler` de ASP.NET Core para:

- Registrar excepciones inesperadas una sola vez con `traceId`.
- Traducir cancelación del cliente sin reportarla como error del servidor.
- Traducir concurrencia de EF Core a 409 cuando no haya sido convertida antes.
- Ocultar stack traces, SQL, secretos y detalles internos en respuestas.
- Retornar 500 genérico para fallos no contemplados.

Los controllers y handlers solo usarán `try/catch` si pueden recuperarse, compensar o añadir contexto útil; no para repetir logging y rethrow.

### 7.3 MediatR y pipeline behaviors

Orden lógico recomendado:

1. Correlation/telemetry behavior.
2. Authorization behavior cuando la policy sea parte del caso de uso.
3. Validation behavior.
4. Performance behavior para detectar requests lentos.
5. Transaction behavior solo para comandos marcados como transaccionales.
6. Handler.

No envolver queries de lectura en transacciones innecesarias. Los domain events se despacharán después de `SaveChanges` o mediante outbox si en el futuro existen efectos externos que exijan entrega confiable.

### 7.4 GenericRepository

Contrato orientativo:

```csharp
public interface IRepository<TEntity> where TEntity : Entity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
```

Reglas:

- `IRepository<TEntity>` vive en Application; la implementación EF vive en Persistence.
- No incluye `SaveChanges`; esa responsabilidad pertenece a Unit of Work.
- No devuelve `IQueryable`.
- `GetAllAsync` solo se usará en catálogos pequeños; endpoints públicos usan paginación.
- Los includes se resuelven mediante métodos específicos, specifications tipadas o parámetros `Expression`, nunca strings mágicos.
- Consultas complejas como dashboard o ticket detail pertenecen a repositorios/read services específicos.

### 7.5 Unit of Work

Contrato mínimo:

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
```

El `DbContext` implementa el commit. La abstracción de transacción debe ser pequeña y solo exponerse a casos de uso que realmente coordinan múltiples escrituras. No se creará otro change tracker ni un Unit of Work que replique toda la API de EF Core.

### 7.6 Services

Ejemplos válidos:

- `ITicketAssignmentService`: reglas de asignación compartidas.
- `ITicketTransitionService`: coordinación compleja si la lógica no cabe completamente en el agregado.
- `ITokenService`: emisión/validación de tokens como puerto.
- `ICurrentUser`: actor actual.
- `IDashboardReadService`: consultas optimizadas de lectura.

Ejemplo no válido: `TicketService.GetById()` que solo llama a `TicketRepository.GetById()` y es utilizado por un único handler sin añadir comportamiento.

### 7.7 DTOs y AutoMapper

- Request DTOs representan el contrato HTTP.
- Command/query models representan el caso de uso.
- Response DTOs son modelos de salida estables.
- Las entidades nunca se reciben ni devuelven directamente.
- AutoMapper se usa para mappings mecánicos simples y proyecciones comprobables.
- Las transiciones de dominio, creación de value objects y reglas condicionales se escriben explícitamente.
- Toda configuración de AutoMapper se valida en tests.

### 7.8 Validación

Se usará validación en dos niveles:

- **Aplicación:** formato, rangos, campos requeridos y reglas que necesitan puertos; preferentemente FluentValidation integrado al pipeline.
- **Dominio:** invariantes que deben sostenerse sin importar el punto de entrada.

No se duplicarán mensajes y reglas sin necesidad. Los códigos de error serán estables; el texto puede evolucionar o localizarse después.

---

## 8. Persistencia intercambiable

### 8.1 Selección de proveedor

Configuración:

```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionStringName": "MainDatabase"
  }
}
```

Valores permitidos: `PostgreSql` y `SqlServer`. El host selecciona el adaptador en `AddPersistence(configuration)`.

La aplicación, dominio, controllers y handlers no cambian al alternar el proveedor. Solo cambian configuración, paquete/registro EF y assembly de migraciones.

### 8.2 Migraciones por proveedor

EF Core genera SQL distinto por proveedor; por lo tanto se mantienen historiales separados:

```text
Migrations/PostgreSql/
Migrations/SqlServer/
```

Cada proveedor tendrá su `IDesignTimeDbContextFactory`. La CI comprobará que una base vacía puede aplicar todas las migraciones de su proveedor.

### 8.3 Convenciones portables

- GUID como claves primarias.
- `DateTimeOffset` o `DateTime` UTC con conversión consistente y tests.
- Precisión decimal explícita.
- Longitudes de string explícitas.
- Índices, claves únicas y restricciones definidos con Fluent API.
- Evitar tipos exclusivos (`jsonb`, temporal tables, arrays) en el baseline.
- Si se aprueba una optimización exclusiva, encapsularla detrás de un adaptador y proporcionar alternativa funcional para el otro proveedor.
- Comparaciones case-insensitive deben tener semántica probada en ambos motores.
- Nombres de tablas/columnas compatibles y límites de longitud controlados.

### 8.4 Semántica de transacción y resiliencia

- Una petición no mantiene transacciones abiertas mientras llama servicios externos.
- Reintentos se aplican solo a errores transitorios y operaciones seguras.
- Nunca se reintenta ciegamente una operación no idempotente.
- Los comandos que acepten reintentos de cliente podrán usar una idempotency key en una spec posterior.

---

## 9. API y OpenAPI

### 9.1 Convenciones

- Controllers sencillos y finos.
- `CancellationToken` propagado hasta EF Core y adaptadores.
- `201 Created` con `Location` para creación.
- `204 No Content` para actualizaciones sin body cuando el contrato lo establezca.
- `ETag` o token de versión DTO para concurrencia; la spec de tickets elegirá un mecanismo y lo mantendrá uniforme.
- Paginación mediante `page`, `pageSize`; filtros explícitos y allowlist de ordenación.

### 9.2 Endpoints baseline

```text
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh
POST   /api/v1/auth/logout

GET    /api/v1/users
POST   /api/v1/users
GET    /api/v1/users/{id}
PUT    /api/v1/users/{id}
PATCH  /api/v1/users/{id}/status

GET    /api/v1/departments
POST   /api/v1/departments
GET    /api/v1/departments/{id}
PUT    /api/v1/departments/{id}
PATCH  /api/v1/departments/{id}/status

GET    /api/v1/tickets
POST   /api/v1/tickets
GET    /api/v1/tickets/{id}
PUT    /api/v1/tickets/{id}
PATCH  /api/v1/tickets/{id}/status
PATCH  /api/v1/tickets/{id}/assignee
GET    /api/v1/tickets/{id}/comments
POST   /api/v1/tickets/{id}/comments
GET    /api/v1/tickets/{id}/history

GET    /api/v1/dashboard/summary
GET    /api/v1/dashboard/trends
```

Los verbos, bodies y códigos definitivos se fijarán en cada feature spec.

### 9.3 Swagger/OpenAPI

- Documento OpenAPI para v1.
- JWT bearer security scheme.
- Ejemplos de requests, responses y ProblemDetails.
- Esquemas con nullability correcta.
- XML docs o metadata equivalente para operaciones públicas.
- Swagger UI habilitado en Development y protegido/deshabilitado por defecto en Production.
- Test que genere el documento y detecte colisiones o esquemas inválidos.

---

## 10. Seguridad

### 10.1 Autenticación

Baseline recomendado:

- ASP.NET Core Identity para usuarios y hashing de contraseñas.
- Access tokens JWT de corta duración.
- Refresh tokens aleatorios, rotatorios, revocables y almacenados de forma hasheada.
- Claims mínimos: subject, roles/policies necesarias, token id y timestamps.
- Claves y emisor/audiencia por entorno.

La abstracción permitirá incorporar OIDC corporativo más adelante sin cambiar los casos de uso principales.

### 10.2 Autorización

- Policies explícitas como `Tickets.Read`, `Tickets.Create`, `Tickets.Assign`, `Tickets.ChangeStatus`, `Users.Manage` y `Dashboard.Read`.
- Reglas por recurso dentro de handlers/services, después de cargar el mínimo dato requerido.
- Denegación por defecto.
- 404 en lugar de 403 cuando revelar la existencia del recurso sea sensible.

### 10.3 Controles técnicos

- HTTPS obligatorio fuera de desarrollo local.
- CORS con orígenes explícitos; nunca `AllowAnyOrigin` con credenciales.
- Rate limiting en autenticación y endpoints de alto costo.
- Límite de tamaño de body y validación de content type.
- Password policy y lockout razonables.
- Secretos mediante variables/secret store; `.env.example` solo contiene nombres y valores ficticios.
- Redacción de tokens, cookies, contraseñas, connection strings y PII en logs.
- Dependencias auditadas y actualizadas.
- Imágenes Docker sin root cuando la imagen base lo soporte, filesystem de solo lectura cuando sea viable y capabilities mínimas.

### 10.4 Criterios OWASP relevantes

Las specs deberán comprobar al menos control de acceso, autenticación, inyección, configuración insegura, componentes vulnerables, logging de seguridad y SSRF si se agregan integraciones externas.

---

## 11. Observabilidad y operaciones

### 11.1 Logs

- Logs estructurados con categorías y niveles coherentes.
- `traceId`, `spanId`, request path, status code, duración y user id no sensible.
- No duplicar el mismo error en middleware, handler y controller.
- Eventos de auditoría separados conceptualmente de logs operativos.

### 11.2 OpenTelemetry

- Trazas de ASP.NET Core, HttpClient y EF Core.
- Métricas HTTP, duración de casos de uso, errores y pool/conexiones cuando estén disponibles.
- Exportador OTLP configurable; consola solo para desarrollo.
- Instrumentación propia alrededor de casos de uso críticos, no spans por cada método trivial.

### 11.3 Health checks

- `/health/live`: proceso vivo, sin dependencia de base de datos.
- `/health/ready`: conectividad y readiness de base de datos.
- Respuestas sin secretos ni topología interna.
- Docker Compose usa health checks antes de considerar servicios listos.

### 11.4 Auditoría

Registrar en almacenamiento persistente los cambios relevantes:

- actor y fecha UTC;
- entidad e identificador;
- acción;
- valores relevantes permitidos o resumen seguro;
- correlation id.

No almacenar contraseñas, tokens, connection strings ni bodies completos por defecto.

---

## 12. Docker y Docker Compose

### 12.1 Imágenes

`Dockerfile` multi-stage:

1. Restore con archivos de solución y paquetes.
2. Build y test en CI, no necesariamente dentro de la imagen final.
3. Publish Release.
4. Runtime ASP.NET Core 10, usuario no root, puerto documentado y health check.

La imagen deberá ser reproducible, sin SDK ni secretos en la capa final.

### 12.2 Compose y perfiles

```text
services:
  api
  postgres     # profile: postgres
  sqlserver    # profile: sqlserver
  otel-collector (opcional en desarrollo)
```

Comportamiento esperado:

- PostgreSQL será el proveedor local por defecto por ligereza y facilidad de ejecución.
- SQL Server se inicia con el profile correspondiente y su licencia aceptada mediante configuración local.
- No se arrancan ambos motores salvo para pruebas de compatibilidad.
- Volúmenes persistentes con nombres del proyecto.
- Credenciales de desarrollo definidas fuera del compose versionado; `.env.example` documenta variables.
- La API espera readiness de base de datos con health checks y reintentos acotados, no con sleeps fijos.

Comandos previstos en README:

```bash
docker compose --profile postgres up --build
docker compose --profile sqlserver up --build
docker compose down
```

Eliminar volúmenes será una operación separada y explícitamente destructiva; no formará parte del comando normal de apagado.

### 12.3 Migraciones al iniciar

En desarrollo podrá existir un migrator/command explícito. En producción, la aplicación de migraciones debe ser una etapa controlada del despliegue, no un efecto automático de cada réplica de API.

---

## 13. Estrategia de pruebas

### 13.1 Pirámide

#### Domain unit tests

- Invariantes de entidades y value objects.
- Transiciones válidas e inválidas.
- Domain events cuando existan.
- Sin mocks de EF Core.

#### Application unit tests

- Handlers y services.
- Result codes y autorización contextual.
- Validadores y mappings.
- Repositorios/puertos sustituidos mediante doubles.

#### Persistence integration tests

- Repositories reales contra contenedores PostgreSQL y SQL Server.
- Mappings EF, constraints, índices relevantes y transacciones.
- Migración desde base vacía.
- Semántica case-insensitive, fechas, decimales y concurrencia en ambos proveedores.

#### API integration tests

- `WebApplicationFactory` con autenticación controlada.
- Contrato HTTP, ProblemDetails, policies, paginación y OpenAPI.
- Base de datos efímera mediante Testcontainers.

#### Architecture tests

- Domain no referencia capas externas.
- Application no referencia API, Infrastructure ni Persistence.
- Controllers no acceden a repositories/DbContext directamente.
- Implementaciones de interfaces residen en adaptadores permitidos.
- Convenciones de nombres de handlers, requests y validators.

### 13.2 Matriz de CI

| Suite | PostgreSQL | SQL Server | Cada PR | Main/nocturna |
|---|---:|---:|---:|---:|
| Unit tests | N/A | N/A | Sí | Sí |
| Architecture tests | N/A | N/A | Sí | Sí |
| Persistence integration | Sí | Sí | Sí, paralelizable | Sí |
| API integration | Sí | Sí | Smoke en ambos | Completa |
| Performance baseline | Primario | Smoke | No | Sí o pre-release |

### 13.3 Calidad

- Objetivo inicial: al menos 80% de cobertura de líneas en Domain y Application, interpretado junto con calidad de escenarios.
- Toda corrección de bug incluye test de regresión.
- Tests deterministas; reloj y actor actual se inyectan.
- No compartir una base mutable entre tests paralelos.
- Snapshots solo para contratos estables y revisables.
- No se acepta una tarea con tests ignorados sin issue, motivo y fecha de revisión.

---

## 14. Modelo de datos inicial

### 14.1 Entidades principales

**User**

- Id
- UserName / Email normalizados según Identity
- DisplayName
- DepartmentId opcional
- Status
- Audit fields

**Department**

- Id
- Name único
- Description
- IsActive
- Audit fields
- Concurrency token

**Ticket**

- Id
- Number legible y único si se aprueba en la spec
- Title
- Description
- Status
- Priority
- DepartmentId
- ReporterId
- AssigneeId opcional
- CreatedAtUtc / UpdatedAtUtc / ResolvedAtUtc
- Concurrency token

**TicketComment**

- Id
- TicketId
- AuthorId
- Body
- Visibility
- CreatedAtUtc / UpdatedAtUtc

**TicketHistoryEntry**

- Id
- TicketId
- ActorId
- EventType
- Summary seguro
- OccurredAtUtc
- CorrelationId

**RefreshTokenSession**

- Id
- UserId
- TokenHash
- CreatedAtUtc / ExpiresAtUtc / RevokedAtUtc
- ReplacedByTokenId opcional
- Device/session metadata mínima y segura

### 14.2 Reglas de modelado

- Relaciones y delete behavior explícitos.
- Historial no usa cascade delete desde Ticket.
- Índices en filtros principales: estado, prioridad, departamento, asignado y fechas.
- Unicidad normalizada de departamento y número de ticket.
- Concurrency token portable; el diseño debe probarse en ambos motores.
- Campos de auditoría llenados mediante interceptor/servicio central, no manualmente en cada handler.

---

## 15. Fases de implementación

### Fase 0 - Baseline y decisiones

Entregables:

- Specs, ADRs, solución vacía y reglas de dependencias.
- .NET 10 fijado, paquetes centralizados y formateo/análisis.
- CI mínima con build y unit tests.

Criterio de salida: la solución compila, las dependencias entre proyectos son correctas y la spec `000` está aprobada.

### Fase 1 - Cross-cutting y persistencia

Entregables:

- Result/Error, ProblemDetails y exception handler.
- MediatR, behaviors, AutoMapper y validación.
- DbContext, GenericRepository, UnitOfWork y selección de proveedor.
- Docker Compose con PostgreSQL y SQL Server.
- Migración inicial y tests de compatibilidad.

Criterio de salida: una operación vertical de prueba atraviesa API a base de datos en ambos proveedores.

### Fase 2 - Identidad y seguridad

Entregables:

- Identity, login/refresh/logout, policies y current user.
- Seed seguro y documentado para desarrollo.
- Rate limiting y hardening básico.

Criterio de salida: flujos exitosos y fallidos están probados, y ninguna ruta protegida es accesible sin policy.

### Fase 3 - Departamentos y usuarios

Entregables:

- CRUD, asignaciones, estado activo e índices.
- OpenAPI y pruebas de integración.

Criterio de salida: requisitos `DEP` y `USR` aprobados en ambos motores.

### Fase 4 - Tickets

Entregables:

- Agregado Ticket, CRUD, filtros, asignación, transiciones y concurrencia.
- Repositories específicos y auditoría.

Criterio de salida: transición + historial son atómicos y los conflictos concurrentes devuelven 409.

### Fase 5 - Comentarios, historial y dashboard

Entregables:

- Comentarios con visibilidad y autorización.
- Historial consultable e inmutable.
- Métricas y tendencias con consultas optimizadas.

Criterio de salida: métricas respetan el ámbito autorizado y cumplen baseline de rendimiento.

### Fase 6 - Observabilidad y release readiness

Entregables:

- OpenTelemetry, health checks, logs y runbooks.
- CI completa, SBOM/scan si la plataforma lo permite, pruebas de imagen y migraciones.
- README y colección de ejemplos HTTP.

Criterio de salida: checklist de release completo y ejecución limpia desde un clone nuevo.

### Fase 7 - Frontend

No comienza dentro de esta especificación. Antes de iniciarlo se congelará un contrato OpenAPI v1 usable, se generará una spec separada y se acordará el stack de UI.

---

## 16. Backlog de tareas maestro

Las tareas siguientes se descompondrán en `tasks.md`. Cada checkbox requiere evidencia de verificación.

### 16.1 Fundación

- [ ] TASK-FND-001 Crear solución y proyectos con referencias permitidas.
- [ ] TASK-FND-002 Configurar `global.json`, props, paquetes centrales, editorconfig y analyzers.
- [ ] TASK-FND-003 Crear specs y ADRs baseline.
- [ ] TASK-FND-004 Implementar tests de arquitectura.
- [ ] TASK-FND-005 Configurar CI de build, format, unit y architecture tests.

### 16.2 Application cross-cutting

- [ ] TASK-APP-001 Implementar `Result<T>`, `Error` y catálogo de códigos.
- [ ] TASK-APP-002 Configurar MediatR y behaviors.
- [ ] TASK-APP-003 Configurar validación y mapping profiles.
- [ ] TASK-APP-004 Definir paginación, sorting allowlist y reloj/usuario actual.
- [ ] TASK-APP-005 Probar orden y comportamiento del pipeline.

### 16.3 API

- [ ] TASK-API-001 Configurar versionado, controllers base y mapeo Result->HTTP.
- [ ] TASK-API-002 Implementar global exception handler con ProblemDetails.
- [ ] TASK-API-003 Configurar OpenAPI y esquema bearer.
- [ ] TASK-API-004 Configurar CORS, rate limiting, HTTPS y límites de request.
- [ ] TASK-API-005 Agregar health endpoints y contrato de errores.

### 16.4 Persistence y Docker

- [ ] TASK-PER-001 Crear DbContext, configuraciones y auditoría.
- [ ] TASK-PER-002 Implementar GenericRepository y UnitOfWork.
- [ ] TASK-PER-003 Agregar repositorios específicos iniciales.
- [ ] TASK-PER-004 Implementar selector PostgreSQL/SQL Server.
- [ ] TASK-PER-005 Crear migraciones y factories separadas.
- [ ] TASK-PER-006 Crear Dockerfile y Compose con profiles y health checks.
- [ ] TASK-PER-007 Crear tests de contrato para ambos proveedores.

### 16.5 Seguridad

- [ ] TASK-SEC-001 Integrar Identity y esquema inicial.
- [ ] TASK-SEC-002 Implementar emisión, rotación y revocación de tokens.
- [ ] TASK-SEC-003 Definir roles, policies y handlers de autorización.
- [ ] TASK-SEC-004 Añadir lockout, seed de desarrollo y redacción de secretos.
- [ ] TASK-SEC-005 Ejecutar pruebas negativas de acceso.

### 16.6 Features

- [ ] TASK-DEP-001..N Implementar departamentos según spec `020`.
- [ ] TASK-USR-001..N Implementar usuarios según spec `020`.
- [ ] TASK-TKT-001..N Implementar tickets según spec `030`.
- [ ] TASK-CMT-001..N Implementar comentarios e historial según spec `040`.
- [ ] TASK-DSH-001..N Implementar dashboard según spec `050`.

### 16.7 Operación y entrega

- [ ] TASK-OPS-001 Instrumentar trazas, métricas y logs.
- [ ] TASK-OPS-002 Crear runbooks de migración, rollback y troubleshooting.
- [ ] TASK-OPS-003 Validar imagen como usuario no root.
- [ ] TASK-OPS-004 Ejecutar matriz completa de CI y baseline de rendimiento.
- [ ] TASK-OPS-005 Verificar clone limpio -> configuración -> ejecución -> tests.

---

## 17. Criterios de aceptación globales

El backend se acepta cuando todos los puntos siguientes se cumplen:

1. **Build:** `dotnet build` termina sin errores ni warnings no aprobados.
2. **Tests:** todas las suites requeridas pasan; no hay tests ignorados sin justificación.
3. **Arquitectura:** las pruebas confirman las dependencias establecidas.
4. **Base de datos:** una base PostgreSQL y una SQL Server vacías reciben sus migraciones y superan los tests de persistencia/API aplicables.
5. **Configuración:** cambiar `Database:Provider` y connection string no exige modificar Domain, Application ni controllers.
6. **API:** OpenAPI se genera sin errores; los endpoints usan DTOs y respuestas ProblemDetails consistentes.
7. **Errores:** los fallos esperados usan Result; las excepciones inesperadas son manejadas globalmente sin filtrar detalles.
8. **Seguridad:** autenticación, refresh rotation, revocación y policies tienen pruebas positivas y negativas.
9. **Concurrencia:** actualizaciones obsoletas se rechazan con 409 y no pierden datos.
10. **Transacciones:** transición de ticket e historial se confirman o revierten juntos.
11. **Observabilidad:** una petición puede rastrearse mediante trace/correlation id; health checks distinguen liveness y readiness.
12. **Docker:** ambos profiles arrancan de forma documentada; la API llega a estado ready y la imagen final no contiene secretos.
13. **SDD:** cada requisito implementado apunta a diseño, tareas y tests; todas las specs están sincronizadas.
14. **Documentación:** README permite a una persona nueva ejecutar el backend y sus tests desde un clone limpio.
15. **Scope:** no se ha iniciado el frontend ni introducido infraestructura distribuida fuera de alcance.

---

## 18. Protocolo operativo para Codex

Codex debe usar las reglas siguientes al recibir este documento:

1. Inspeccionar primero el repositorio, instrucciones locales y cambios existentes.
2. Crear o actualizar la spec de la fase activa antes de cambiar código.
3. Presentar ambigüedades que cambien contrato, seguridad o datos; para detalles reversibles puede adoptar la opción más simple y documentarla.
4. Implementar vertical slices pequeños con tests, manteniendo los límites de proyectos.
5. No agregar paquetes, patrones o servicios sin una necesidad trazable.
6. No sustituir el GenericRepository o UnitOfWork acordados; sí puede proponer ajustes concretos mediante ADR cuando EF Core o una consulta compleja lo requiera.
7. Mantener PostgreSQL y SQL Server en igualdad funcional; las optimizaciones específicas necesitan fallback y tests.
8. Verificar cada tarea con el check más estrecho primero y luego con la suite relevante.
9. No declarar completada una tarea si faltan migraciones, pruebas, OpenAPI o documentación exigida por esa tarea.
10. Al finalizar cada incremento, informar: archivos relevantes, requisitos cubiertos, pruebas ejecutadas, resultados, riesgos y siguiente tarea desbloqueada.

### Prompt de inicio sugerido

```text
Usa este documento como baseline arquitectónico y de producto. Comienza solo con
la spec 000-solution-foundation. Inspecciona el repositorio, crea o refina
requirements.md, design.md y tasks.md con trazabilidad, señala decisiones que
requieran aprobación y no implementes fases posteriores. Una vez aprobada la
spec, ejecuta sus tareas en orden, verifica cada una y mantén actualizados los
checkboxes y ADRs.
```

---

## 19. Riesgos y mitigaciones

### Duplicación entre handlers y services

**Riesgo:** capas que solo se delegan llamadas.  
**Mitigación:** handler como caso de uso; service solo para comportamiento reusable/complex; tests de arquitectura y revisión.

### GenericRepository demasiado amplio

**Riesgo:** API genérica que filtra detalles EF o produce consultas ineficientes.  
**Mitigación:** contrato mínimo, sin IQueryable, repositorios/read services específicos para detalle y dashboard.

### Falsa portabilidad de base de datos

**Riesgo:** la solución compila con ambos providers pero cambia semántica en runtime.  
**Mitigación:** migraciones separadas y la misma suite de contrato en contenedores reales.

### Sobrearquitectura

**Riesgo:** tiempo invertido en abstracciones sin uso.  
**Mitigación:** cada abstracción debe tener una frontera o sustitución real; ADR para nuevas capas transversales.

### Result Pattern inconsistente

**Riesgo:** algunos handlers lanzan excepciones para errores esperados y otros devuelven resultados.  
**Mitigación:** catálogo de errores, mapping central y tests de convenciones.

### Migraciones automáticas inseguras

**Riesgo:** varias réplicas alteran el esquema al mismo tiempo.  
**Mitigación:** etapa de migración controlada en entornos compartidos; auto-migration solo en desarrollo explícito.

---

## 20. Preguntas que cada feature spec debe cerrar

- ¿Cuál es la máquina exacta de estados del ticket?
- ¿Quién puede ver o modificar tickets de otros departamentos?
- ¿Cómo se define un ticket vencido y existe SLA por prioridad?
- ¿Los comentarios pueden editarse/eliminarse y durante cuánto tiempo?
- ¿Qué campos exactos forman parte del historial auditable?
- ¿Se necesita número de ticket legible además del GUID?
- ¿Qué política aplica al desactivar usuarios/departamentos con trabajo activo?
- ¿Cuál será la duración de access/refresh tokens por entorno?
- ¿Qué volumen de datos define el baseline de rendimiento?
- ¿Qué exporter de OpenTelemetry usará cada entorno?

Estas preguntas no bloquean la fase 0, pero deben resolverse antes de implementar la feature afectada.

---

## 21. Fuentes y referencias

- Microsoft, **.NET Support Policy**: https://dotnet.microsoft.com/en-us/platform/support/policy
- Microsoft Learn, **What's new in .NET 10**: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview
- Kiro Docs, **Specs**: https://kiro.dev/docs/specs/
- Kiro Docs, **Feature Specs**: https://kiro.dev/docs/specs/feature-specs/
- Kiro Docs, **Best practices**: https://kiro.dev/docs/specs/best-practices/
- OWASP, **Application Security Verification Standard**: https://owasp.org/www-project-application-security-verification-standard/
- OpenTelemetry, **.NET documentation**: https://opentelemetry.io/docs/languages/dotnet/

---

## 22. Registro de decisiones de esta versión

| Decisión | Estado | Resultado |
|---|---|---|
| Runtime | Aceptada | .NET 10 LTS / C# 14 / EF Core 10 |
| Método | Aceptada | Specs versionadas: requirements, design, tasks |
| Arquitectura | Aceptada | Clean Architecture + Ports and Adapters |
| Persistencia | Aceptada | GenericRepository + repos específicos + UnitOfWork |
| Aplicación | Aceptada | MediatR handlers + services sin duplicación |
| Errores | Aceptada | Result para esperados; handler global para inesperados |
| Contratos | Aceptada | DTOs + AutoMapper selectivo + OpenAPI v1 |
| Proveedores | Aceptada | PostgreSQL y SQL Server por configuración |
| Contenedores | Aceptada | Dockerfile multi-stage + Compose profiles |
| Entrega | Aceptada | Backend-first; frontend en especificación posterior |

**Fin de la especificación maestra v1.0.**
