# 020 — Departments and Users: Design

**Estado:** Approved
**Fecha:** 7 de agosto de 2026
**Aprobación:** Aprobada explícitamente por el usuario el 7 de agosto de 2026.
**Requisitos:** `requirements.md`
**Dependencia:** `../010-identity-and-access/`
**Gate:** Diseño aprobado; la implementación permanece sujeta a `GATE-DU-001`.

## 1. Resumen

La fase 3 agrega cortes verticales administrativos para departamentos y usuarios. Domain encapsula invariantes y transiciones; Application define commands, queries, DTOs y puertos; Persistence implementa consultas EF, administración de Identity y transacciones en el `ApplicationDbContext` compartido; API publica controllers delgados protegidos por `Users.Manage`.

`IdentityAccount` continúa siendo la autoridad de credenciales, username/email normalizados, lockout y roles. `Domain.User` conserva perfil operativo, departamento, estado, auditoría y versión. Ambas representaciones comparten GUID y las mutaciones compuestas se confirman o revierten en una sola transacción local.

## 2. Dirección de dependencias

```text
Api -> Application -> Domain
 |         \-> Shared
 |-> Infrastructure -> Application + Shared
 \-> Persistence -> Application + Domain + Shared
      ^
      ├── Migrations.PostgreSql
      └── Migrations.SqlServer
```

Reglas:

- Domain no conoce EF Core, Identity, HTTP ni providers.
- Application no conoce `UserManager`, `IdentityAccount`, DbContext ni SQL.
- Persistence contiene adapters de escritura/lectura y traducción técnica.
- API configura autorización y transforma HTTP; no contiene reglas de negocio.
- Controllers solo envían commands/queries mediante `ISender`.
- No se devuelve `IQueryable`, entidades EF o tipos Identity desde puertos.

## 3. Modelo de dominio

### DES-DU-001 Department

`Department` deja de ser un modelo con setters públicos y expone operaciones explícitas:

```text
Department
- Id: Guid
- Name: string
- NormalizedName: string
- Description: string
- IsActive: bool
- Version: Guid
- CreatedAtUtc: DateTime
- UpdatedAtUtc: DateTime?

Create(name, description)
Update(name, description)
Activate()
Deactivate()
```

Reglas:

- nombre y descripción se validan/canonicalizan antes de construir o mutar;
- `NormalizedName` usa una función pura de Domain basada en Form KC, whitespace canónico y mayúsculas invariantes;
- cada mutación efectiva rota `Version`;
- repetir el mismo estado no rota versión ni timestamps;
- Domain no consulta tickets: el handler comprueba trabajo activo antes de llamar `Deactivate`.

### DES-DU-002 User profile

`Domain.User` representa únicamente perfil operativo:

```text
User
- Id: Guid (igual a IdentityAccount.Id)
- UserName: string
- DisplayName: string
- IsActive: bool
- DepartmentId: Guid?
- Version: Guid
- CreatedAtUtc: DateTime
- UpdatedAtUtc: DateTime?

Create(id, userName, displayName, departmentId?)
UpdateProfile(userName, displayName)
AssignDepartment(departmentId)
UnassignDepartment()
Activate()
Deactivate()
```

Email, password, roles, lockout y normalizadores permanecen fuera de Domain. Username, display name e isActive se duplican por necesidades operativas, pero solo los handlers administrativos aprobados pueden sincronizarlos con Identity dentro de la misma transacción.

## 4. Contratos de Application

### DES-DU-003 Paginación y DTOs

Contrato común:

```csharp
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
```

Los filtros y sort fields son enums/modelos cerrados, no strings convertidos a expresiones arbitrarias. Application valida página, tamaño, combinaciones y allowlists antes de llamar al puerto.

DTOs principales:

```text
DepartmentDto
- id, name, description, isActive
- createdAtUtc, updatedAtUtc, version

DepartmentSummaryDto
- id, name, isActive

UserAdministrationDto
- id, userName, email, displayName, isActive
- department: DepartmentSummaryDto?
- roles: string[]
- createdAtUtc, updatedAtUtc, version
```

Requests HTTP se mantienen en API y se convierten a commands. Password solo vive en request/command de creación y en el modelo opaco que recibe el adapter de Identity; nunca forma parte de results o DTOs.

### Puertos

```csharp
public interface IDepartmentRepository
{
    Task<Department?> GetTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> NormalizedNameExistsAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> HasActiveWorkAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Department department, CancellationToken cancellationToken);
}

public interface IDepartmentReadService
{
    Task<DepartmentDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<DepartmentDto>> ListAsync(DepartmentListFilter filter, CancellationToken cancellationToken);
}

public interface IUserProfileRepository
{
    Task<User?> GetTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
}

public interface IUserAdministrationReadService
{
    Task<UserAdministrationDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<UserAdministrationDto>> ListAsync(UserListFilter filter, CancellationToken cancellationToken);
}

public interface IIdentityAccountAdministration
{
    Task<Result> CreateAsync(AccountCreation account, CancellationToken cancellationToken);
    Task<Result> UpdateIdentifiersAsync(AccountIdentifierUpdate update, CancellationToken cancellationToken);
    Task<Result> ReplaceRolesAsync(Guid userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken);
    Task<Result> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken);
    Task<IReadOnlySet<string>?> GetRolesAsync(Guid userId, CancellationToken cancellationToken);
    Task<int> CountActiveAdministratorsAsync(CancellationToken cancellationToken);
}

public interface IAtomicOperation
{
    Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        AtomicIsolation isolation,
        CancellationToken cancellationToken);
}
```

`IRefreshTokenSessionRepository` se amplía con revocación por usuario. Los adapters no exponen `IdentityResult`; traducen a errores propios.

## 5. Vertical slices

### DES-DU-008 Departments

```text
Features/Departments/
├── CreateDepartment
├── GetDepartment
├── ListDepartments
├── UpdateDepartment
└── SetDepartmentStatus
```

Flujos:

```text
POST /departments
 -> policy Users.Manage
 -> CreateDepartmentCommand
 -> canonicalize + invariant
 -> duplicate pre-check
 -> add + SaveChanges
 -> unique violation race => departments.name_conflict
 -> read projection
 -> 201 + Location
```

```text
PATCH /departments/{id}/status (false)
 -> load tracked + compare version
 -> serializable atomic operation
 -> HasActiveWork(Open | InProgress)
 -> conflict OR Department.Deactivate
 -> SaveChanges
 -> read projection
```

### DES-DU-009 Users

```text
Features/Users/
├── CreateUser
├── GetUser
├── ListUsers
├── UpdateUser
├── AssignUserDepartment
├── SetUserStatus
└── ReplaceUserRoles
```

Crear usuario:

```text
POST /users
 -> policy Users.Manage
 -> validate canonical roles + optional active department
 -> IAtomicOperation (shared ApplicationDbContext)
    -> generate one GUID
    -> IdentityAccount + password
    -> assign roles
    -> Domain.User with same GUID
    -> SaveChanges
    -> commit only on Result success
 -> read projection
 -> 201 + Location
```

Actualizar perfil sincroniza username/display name en Identity y Domain en la misma transacción. Asignación valida usuario activo y departamento activo. Desasignación acepta `null` y es idempotente.

### DES-DU-010 Estado y roles

Desactivar usuario:

```text
PATCH /users/{id}/status
 -> load profile + account + roles
 -> compare version
 -> reject self-deactivation
 -> serializable: protect last active Administrator
 -> IdentityAccount.IsActive = false
 -> Domain.User.Deactivate()
 -> revoke active refresh sessions
 -> SaveChanges + commit
```

Reactivar exige departamento nulo o activo y al menos un rol. No modifica password, lockout ni sesiones revocadas.

Reemplazar roles:

- valida conjunto canónico no vacío;
- impide retirar `Administrator` del actor;
- protege al último administrador activo bajo aislamiento serializable;
- si se elimina algún rol, revoca refresh sessions;
- no revoca access tokens ya emitidos.

## 6. Persistencia

### DES-DU-004 EF mappings e índices

Configuraciones explícitas:

- `DepartmentConfiguration`;
- `UserConfiguration`;
- extensión de la configuración de `IdentityAccount`.

Department:

- `Name` required, max 100;
- `NormalizedName` required, max 100;
- `Description` required, max 500;
- `Version` required, concurrency token;
- índice único estable `UX_Departments_NormalizedName`;
- índice `(IsActive, NormalizedName)`.

User:

- `UserName` required, max 256;
- `DisplayName` required, max 200;
- `Version` required, concurrency token;
- índice `(DepartmentId, IsActive)`;
- índice `(IsActive, DisplayName)`;
- FK Department→User `Restrict`;
- shared PK/FK IdentityAccount→User `Restrict`.

Identity conserva `UserNameIndex` único. `EmailIndex` pasa a único filtrado para `NormalizedEmail IS NOT NULL`, con SQL/metadata específico por provider en cada migración. Los nombres de constraints e índices son estables para traducir conflictos sin inspeccionar mensajes localizados.

### DES-DU-005 Concurrencia, transacciones y auditoría

`Version` es GUID portable y se actualiza en cada mutación efectiva. EF lo marca `IsConcurrencyToken`; `DbUpdateConcurrencyException` se traduce al código específico del recurso.

`IAtomicOperation`:

1. usa `Database.CreateExecutionStrategy()`;
2. inicia transacción en el `ApplicationDbContext` scoped compartido;
3. ejecuta el callback;
4. hace commit solo si el Result es exitoso;
5. revierte en fallo o excepción;
6. limpia el tracker tras rollback cuando sea necesario;
7. no usa `TransactionScope` ni transacciones distribuidas.

El mismo DbContext respalda stores de Identity, repositorios, Unit of Work y refresh sessions. Esto incluye los `SaveChanges` internos de `UserManager` dentro de la transacción.

Auditoría se centraliza en Persistence mediante el DbContext/interceptor y usa `IClock` e `ICurrentUser`:

- nueva entidad: fija `CreatedAtUtc` una vez;
- entidad modificada: fija `UpdatedAtUtc`;
- nunca acepta timestamps del request;
- seeds/migraciones se identifican como actor de sistema donde aplique.

### DES-DU-006 Read services

Los read services usan:

- `AsNoTracking`;
- proyección SQL directa;
- `CountAsync` + página limitada;
- joins explícitos entre perfil, Identity, roles y departamento;
- orden estable con `Id` como desempate;
- ninguna carga de password hash, stamps o sesiones.

No se utiliza el `GetAllAsync` genérico para listados administrativos.

### DES-DU-007 Reglas que requieren serialización

Aislamiento serializable se reserva para:

- comprobar trabajo activo y desactivar departamento;
- asignar usuario mientras otro request desactiva el departamento;
- desactivar o retirar rol al último administrador activo.

Conflictos transitorios/deadlocks se delegan a la execution strategy. Tras agotar retries se devuelve un fallo genérico seguro o conflicto estable según la excepción clasificada.

## 7. Migraciones dual-provider

### DES-DU-011 Migración incremental

No se modifican `InitialIdentityAndAccess` ni `AddLogicalDeletion`. La implementación puede generar migraciones incrementales por corte vertical (`DepartmentsFoundation` y `UsersFoundation`) en cada assembly:

- `InternalOperations.Persistence.Migrations.PostgreSql`;
- `InternalOperations.Persistence.Migrations.SqlServer`.

La migración:

1. agrega `NormalizedName` inicialmente nullable;
2. backfill/canonicaliza datos existentes con SQL específico del provider o una precondición explícita;
3. falla de forma diagnóstica si existen duplicados incompatibles;
4. convierte `NormalizedName` en required;
5. agrega `Version` a Department/User y valores para filas existentes;
6. ajusta longitudes;
7. hace explícitas FKs restrictivas;
8. crea/reemplaza índices;
9. convierte el email normalizado en único filtrado;
10. conserva la misma semántica en ambos snapshots.

`Down` elimina solo cambios de 020 y restaura índices/columnas previos; no elimina las tablas skeleton. Los provider contracts prueban upgrade desde la migración inicial, rollback de 020 y reaplicación.

## 8. HTTP, autorización y errores

### DES-DU-012 Controllers

Endpoints aprobables:

```text
GET    /api/v1/departments
POST   /api/v1/departments
GET    /api/v1/departments/{id}
PUT    /api/v1/departments/{id}
PATCH  /api/v1/departments/{id}/status

GET    /api/v1/users
POST   /api/v1/users
GET    /api/v1/users/{id}
PUT    /api/v1/users/{id}
PATCH  /api/v1/users/{id}/department
PATCH  /api/v1/users/{id}/status
PUT    /api/v1/users/{id}/roles
```

Cada action declara `Users.Manage`, consume un request DTO explícito, envía un command/query y usa el mapping central Result→HTTP.

### Errores estables

Departments:

- `departments.not_found` → 404;
- `departments.name_conflict` → 409;
- `departments.active_work_conflict` → 409;
- `departments.version_conflict` → 409;
- validación → 400.

Users:

- `users.not_found` → 404;
- `users.identifier_conflict` → 409;
- `users.inactive` → 409;
- `users.invalid_role` → 400;
- `users.password_requirements_not_met` → 400;
- `users.version_conflict` → 409;
- `users.last_administrator` → 409;
- inconsistencia account/profile → 500 genérico + log crítico.

Violaciones PostgreSQL `23505` y SQL Server `2601`/`2627` se clasifican por constraints conocidos y se convierten al mismo error. SQL, nombres de constraints y mensajes Identity nunca llegan al cliente.

### OpenAPI

- operation IDs estables;
- bearer security;
- schemas concretos para requests/responses;
- `ProblemDetails` en 400/401/403/404/409/415/500;
- parámetros y límites de paginación;
- `version` requerida en mutaciones;
- `initialPassword` `writeOnly`, `format: password`;
- ejemplos ficticios sin secretos ni PII real.

## 9. Estrategia de pruebas

### Domain unit

- canonicalización e invariantes de Department;
- actualización/versionado e idempotencia;
- invariantes de User, asignación y estado;
- GUID compartido y token de concurrencia.

### Application unit

- cada rama de commands/queries con dobles de puertos;
- validación antes de persistencia;
- duplicados, not found, inactive y stale version;
- reglas de auto/último administrador;
- revocación por estado/roles;
- rollback solicitado ante fallos;
- cancellation token propagado;
- filtros/allowlists/paginación.

### Persistence integration

- metadata de longitudes/nullability/índices/FKs/concurrency;
- adapter Identity y redacción de errores;
- read services y no-tracking;
- auditoría con reloj fijo;
- revocación masiva de sesiones.

### Provider contracts

La misma suite Testcontainers ejecuta en PostgreSQL y SQL Server:

- apply/upgrade/rollback/reapply;
- unicidad de departamento, username y email;
- shared PK/FK y delete restrictivo;
- atomicidad cuenta/perfil/roles;
- rollback tras fallo intermedio;
- concurrencia Department/User;
- carreras de asignación/desactivación y último administrador;
- queries paginadas equivalentes.

### API integration/OpenAPI

- 201 + Location y 200;
- validación 400;
- 401 anónimo y 403 sin policy, sin ejecución del handler;
- 404 no divulgador y 409 estable;
- filtros, orden y paginación;
- DTOs sin campos Identity sensibles;
- password ausente de responses/examples;
- paths, schemas, nullability y security requirements.

### Architecture

- dependencias de capas;
- controllers solo usan `ISender`;
- tipos Identity confinados a Persistence/API composition;
- ningún puerto expone `IQueryable` o EF;
- commands/queries tienen handlers y validadores según convención.

## 10. Riesgos y mitigaciones

- **Doble representación de usuario:** una transacción explícita compartida y pruebas de rollback.
- **Collations diferentes:** valores normalizados persistidos e índices únicos.
- **UserManager guarda internamente:** transaction wrapper alrededor de todos sus SaveChanges.
- **Carreras de negocio:** serializable solo en las tres reglas críticas y provider contracts.
- **Access tokens tras baja:** duración máxima documentada, refresh revocado y sin promesa de invalidación inmediata.
- **Skeleton ya migrado:** migración incremental; nunca reescribir el historial publicado.
- **Modelo anémico actual:** encapsular invariantes antes de publicar handlers.
- **Matriz 010 aún no observada:** 020 puede aprobarse documentalmente, pero código queda bloqueado hasta publicar y verificar la matriz o aprobar un waiver explícito.

## 11. Criterio de salida

La spec puede pasar a `Completed` únicamente cuando requirements, design, tasks, implementación, OpenAPI, migraciones y evidencia estén sincronizados; la foundation suite y ambas celdas reales del provider matrix pasen sin fallos/skips injustificados; y los contratos observados coincidan en PostgreSQL y SQL Server.