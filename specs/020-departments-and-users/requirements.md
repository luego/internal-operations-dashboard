# 020 — Departments and Users: Requirements

**Estado:** Approved
**Fecha:** 7 de agosto de 2026
**Aprobación:** Aprobada explícitamente por el usuario el 7 de agosto de 2026.
**Basado en:** Fase 3 del documento maestro: Departamentos y usuarios
**Dependencias:** `../010-identity-and-access/` y `../015-logical-deletion/`
**Gate:** Requisitos aprobados y `GATE-DU-001` cerrado; implementación desbloqueada.

## 1. Objetivo

Incorporar administración segura de departamentos y usuarios internos, manteniendo consistentes la cuenta técnica de Identity y el perfil de dominio, con listados paginados, asignación a un único departamento, activación/desactivación administrativa, concurrencia optimista y el mismo contrato observable en PostgreSQL y SQL Server.

## 2. Alcance

Incluye:

- creación, consulta, edición, activación y desactivación de departamentos;
- creación y administración de cuentas/perfiles internos;
- asignación opcional de un usuario a un único departamento;
- reemplazo del conjunto de roles canónicos de un usuario;
- activación y desactivación administrativa de usuarios mediante `IsActive`;
- revocación de sesiones refresh cuando se desactiva un usuario o se reducen sus roles;
- paginación, búsqueda, filtros y ordenación allowlisted;
- concurrencia optimista portable;
- migraciones incrementales y contratos relacionales para ambos providers;
- autorización, OpenAPI, ProblemDetails, auditoría y pruebas.

No incluye:

- eliminación física de departamentos, cuentas o perfiles;
- registro público, invitaciones, recuperación o cambio de contraseña;
- creación, edición o eliminación de roles/policies;
- múltiples departamentos por usuario, equipos o jerarquías;
- autorización departamental para managers ni administración de asignaciones de tickets;
- CRUD o máquina de estados de tickets, salvo consultar el estado baseline para proteger la desactivación de departamentos;
- revocación inmediata de access tokens ya emitidos;
- frontend.

Esta spec no publica endpoints `DELETE`. Activar/desactivar modifica exclusivamente `IsActive`: los registros inactivos continúan visibles y administrables. `IsDeleted` y su filtro global pertenecen a la spec 015 y no se activan desde los endpoints de estado de esta fase.

## 3. Decisiones funcionales propuestas

La aprobación de esta spec acepta las decisiones siguientes:

1. Un departamento conserva su nombre reservado aunque esté inactivo; para reutilizarlo se renombra o reactiva el registro existente.
2. Un departamento no puede desactivarse mientras tenga tickets `Open` o `InProgress`. Los usuarios asignados, por sí solos, no cuentan como trabajo activo.
3. Desactivar un departamento no desasigna usuarios ni tickets y no ejecuta cascadas.
4. Un usuario puede existir sin departamento y solo puede pertenecer a uno.
5. Un usuario puede desactivarse aunque tenga tickets activos; retirar el acceso tiene prioridad y el trabajo se reasignará posteriormente.
6. Desactivar un usuario conserva departamento, roles e historial, pero revoca todas sus sesiones refresh activas.
7. Los access tokens emitidos conservan como máximo la vida de 15 minutos aprobada en la spec 010; no se consultará la base de datos en cada request.
8. `POST /users` recibe una contraseña inicial que cumple la policy vigente. La API nunca la devuelve, registra ni persiste fuera del hash de Identity; su entrega ocurre fuera de esta API.
9. Todos los endpoints de esta spec reutilizan `Users.Manage`, actualmente concedida solo a `Administrator`. La administración de asignaciones mencionada para `Manager` en el master se interpreta como asignación de tickets y se resolverá en la spec 030.
10. La administración de usuarios incluye reemplazar un conjunto no vacío de los roles canónicos `Administrator`, `Manager`, `Agent` y `Viewer`.
11. Un administrador no puede desactivarse a sí mismo ni retirar de sí mismo el rol `Administrator`.
12. Nunca puede desactivarse ni perder su rol el último administrador activo.

## 4. Requisitos de departamentos

### REQ-DEP-001 Crear departamento

**Historia:** Como administrador, quiero crear un departamento para organizar usuarios y trabajo operativo.

#### Criterios de aceptación

1. WHEN un actor con `Users.Manage` envía un nombre válido y único, THE SYSTEM SHALL crear un departamento activo y devolver `201 Created`, su DTO y `Location`.
2. `name` SHALL ser obligatorio y contener entre 1 y 100 caracteres después de canonicalizar whitespace.
3. `description` SHALL ser opcional y tener un máximo de 500 caracteres; el valor omitido se representará como cadena vacía en persistencia.
4. THE SYSTEM SHALL almacenar un nombre visible sin espacios exteriores y con secuencias internas de whitespace reducidas a un espacio.
5. THE SYSTEM SHALL calcular `NormalizedName` mediante Unicode Form KC, canonicalización de whitespace y mayúsculas invariantes, en ese orden.
6. THE SYSTEM SHALL imponer unicidad sobre `NormalizedName`, incluidos departamentos inactivos.
7. WHEN dos escrituras compiten por el mismo nombre normalizado, THE SYSTEM SHALL devolver `409` con `departments.name_conflict` sin filtrar detalles del provider.
8. THE SYSTEM SHALL generar GUID, timestamps UTC y una versión de concurrencia opaca.
9. La respuesta SHALL NOT exponer `NormalizedName` ni metadatos de persistencia.

**Trazabilidad:** DES-DU-001, DES-DU-004, DES-DU-008; TASK-DU-001, TASK-DU-004, TASK-DU-007, TASK-DU-014; TEST-DEP-001..012, TEST-PROV-DEP-001.

### REQ-DEP-002 Consultar y listar departamentos

#### Criterios de aceptación

1. `GET /api/v1/departments/{id}` SHALL devolver `200` con `id`, `name`, `description`, `isActive`, auditoría UTC y `version`.
2. Un identificador inexistente o no visible SHALL devolver el mismo `404 departments.not_found`.
3. `GET /api/v1/departments` SHALL aceptar `page`, `pageSize`, `search`, `isActive`, `sortBy` y `sortDirection`.
4. `page` SHALL tener mínimo 1; `pageSize` SHALL usar 25 por defecto y 100 como máximo.
5. `sortBy` SHALL aceptar únicamente `name`, `createdAtUtc` y `updatedAtUtc`; `sortDirection`, `asc` o `desc`.
6. Parámetros inválidos SHALL devolver `400` antes de consultar persistencia.
7. La ordenación por defecto SHALL ser `NormalizedName ASC, Id ASC`; cualquier otro orden agregará `Id` como desempate.
8. La respuesta SHALL contener `items`, `page`, `pageSize`, `totalCount` y `totalPages`.
9. Una página vacía SHALL devolver `200` con `items: []`.
10. Las lecturas SHALL proyectar a DTO, usar no-tracking y ejecutar filtro, orden y paginación en la base de datos.

**Trazabilidad:** DES-DU-003, DES-DU-006, DES-DU-008; TASK-DU-003, TASK-DU-008, TASK-DU-014; TEST-DEP-013..016, TEST-HTTP-DEP-001..011.

### REQ-DEP-003 Actualizar departamento

#### Criterios de aceptación

1. `PUT /api/v1/departments/{id}` SHALL aceptar `name`, `description` y la `version` opaca actual.
2. THE SYSTEM SHALL aplicar las mismas reglas de canonicalización, longitud y unicidad que en creación.
3. WHEN la versión es actual, THE SYSTEM SHALL guardar el cambio y devolver `200` con una nueva versión.
4. WHEN la versión está obsoleta, THE SYSTEM SHALL devolver `409 departments.version_conflict` sin sobrescribir cambios.
5. WHEN el nombre colisiona, THE SYSTEM SHALL devolver `409 departments.name_conflict`.
6. Este endpoint SHALL NOT cambiar el estado activo.
7. Un departamento inactivo podrá corregir nombre y descripción.

**Trazabilidad:** DES-DU-001, DES-DU-005, DES-DU-008; TASK-DU-001, TASK-DU-008, TASK-DU-014; TEST-DEP-017..020.

### REQ-DEP-004 Activar o desactivar departamento

#### Criterios de aceptación

1. `PATCH /api/v1/departments/{id}/status` SHALL aceptar `isActive` y `version`.
2. WHEN se solicita desactivar, THE SYSTEM SHALL considerar trabajo activo todo ticket `Open` o `InProgress` relacionado con el departamento.
3. WHEN existe trabajo activo, THE SYSTEM SHALL devolver `409 departments.active_work_conflict` sin cambiar departamentos, usuarios o tickets.
4. WHEN no existe trabajo activo, THE SYSTEM SHALL marcar el departamento inactivo sin eliminar ni anular referencias.
5. Usuarios asignados no bloquearán la desactivación y conservarán su `DepartmentId`.
6. Un departamento inactivo SHALL NOT aceptar nuevas asignaciones de usuarios.
7. Reactivar un departamento existente SHALL marcarlo activo sin cambiar asignaciones.
8. Solicitar el estado actual SHALL ser idempotente y devolver `200`.
9. Una versión obsoleta SHALL devolver `409 departments.version_conflict`.
10. La comprobación de trabajo y el cambio de estado SHALL resistir carreras concurrentes en ambos providers.

**Trazabilidad:** DES-DU-005, DES-DU-007; TASK-DU-009, TASK-DU-014, TASK-DU-018; TEST-DEP-021..026, TEST-PROV-DEP-004..005.

## 5. Requisitos de usuarios

### REQ-USR-001 Crear usuario

**Historia:** Como administrador, quiero crear una cuenta y su perfil operativo de forma atómica.

#### Criterios de aceptación

1. `POST /api/v1/users` SHALL aceptar `userName`, `email`, `displayName`, `initialPassword`, un conjunto no vacío de `roles` y `departmentId` opcional.
2. WHEN los datos son válidos, THE SYSTEM SHALL crear `IdentityAccount` y `Domain.User` con el mismo GUID dentro de una única transacción.
3. `userName` y `email` SHALL respetar las reglas de Identity y un máximo de 256 caracteres; `displayName`, entre 1 y 200 caracteres.
4. La contraseña SHALL cumplir la policy de Identity vigente y nunca aparecer en responses, logs, auditoría, OpenAPI examples ni persistencia fuera del hash de Identity.
5. Username y email SHALL ser únicos por sus valores normalizados. Toda colisión pública SHALL usar `409 users.identifier_conflict` sin revelar cuál campo existe.
6. Los roles SHALL pertenecer al conjunto canónico, ser distintos y no estar vacíos.
7. IF se incluye `departmentId`, THEN el departamento SHALL existir y estar activo.
8. WHEN falla cuenta, contraseña, perfil, roles o asignación, THE SYSTEM SHALL revertir toda la operación.
9. El éxito SHALL devolver `201 Created`, `Location` y el DTO administrativo sin hashes, stamps, intentos fallidos, lockout interno ni tokens.

**Trazabilidad:** DES-DU-002, DES-DU-007, DES-DU-009; TASK-DU-002, TASK-DU-010, TASK-DU-011, TASK-DU-015, TASK-DU-019; TEST-USR-001..020, TEST-PROV-USR-001..003.

### REQ-USR-002 Consultar y listar usuarios

#### Criterios de aceptación

1. `GET /api/v1/users/{id}` SHALL devolver `id`, `userName`, `email`, `displayName`, `isActive`, resumen de departamento o `null`, roles, auditoría UTC y `version`.
2. La respuesta SHALL NOT incluir password hash, security/concurrency stamps de Identity, tokens, sesiones, intentos fallidos ni lockout interno.
3. Un usuario inexistente o no visible SHALL devolver el mismo `404 users.not_found`.
4. `GET /api/v1/users` SHALL aceptar `page`, `pageSize`, `search`, `isActive`, `departmentId`, `hasDepartment`, `role`, `sortBy` y `sortDirection`.
5. `sortBy` SHALL aceptar únicamente `userName`, `displayName`, `email`, `createdAtUtc` y `updatedAtUtc`.
6. `role` SHALL ser canónico; combinaciones contradictorias entre `departmentId` y `hasDepartment` SHALL devolver `400`.
7. `search` SHALL aplicar una semántica normalizada equivalente sobre username, email y display name.
8. Se aplicarán los mismos límites 25/100, envelope y desempate por `Id` definidos para departamentos.
9. Lecturas SHALL proyectarse en SQL y no exponer `IQueryable` fuera de Persistence.

**Trazabilidad:** DES-DU-003, DES-DU-006; TASK-DU-003, TASK-DU-011, TASK-DU-012, TASK-DU-015; TEST-USR-019..023, TEST-HTTP-USR-001..011.

### REQ-USR-003 Actualizar perfil administrativo

#### Criterios de aceptación

1. `PUT /api/v1/users/{id}` SHALL modificar únicamente `userName`, `email`, `displayName` y requerir `version`.
2. Estado, departamento, roles y contraseña SHALL modificarse solamente mediante casos de uso separados.
3. THE SYSTEM SHALL mantener sincronizados username, display name y estado compartidos entre Identity y Domain dentro de una transacción.
4. Colisiones de username/email SHALL devolver `409 users.identifier_conflict`.
5. Una versión obsoleta SHALL devolver `409 users.version_conflict`.
6. Un fallo parcial SHALL revertir ambas representaciones.

**Trazabilidad:** DES-DU-002, DES-DU-005, DES-DU-009; TASK-DU-002, TASK-DU-012, TASK-DU-015, TASK-DU-019; TEST-USR-024..029.

### REQ-USR-004 Asignar departamento

#### Criterios de aceptación

1. `PATCH /api/v1/users/{id}/department` SHALL aceptar `departmentId` nullable y `version`.
2. WHEN el usuario está activo y el departamento existe y está activo, THE SYSTEM SHALL guardar la relación transaccionalmente y devolver `200` con nueva versión.
3. `departmentId: null` SHALL desasignar al usuario, incluso si el usuario o su departamento actual están inactivos.
4. Usuario o departamento inexistente/no visible SHALL devolver `404 users.not_found` o `404 departments.not_found`.
5. Usuario inactivo SHALL devolver `409 users.inactive`; departamento inactivo, `409 departments.inactive`.
6. Una asignación idéntica SHALL ser idempotente.
7. Una versión obsoleta SHALL devolver `409 users.version_conflict`.
8. La operación SHALL NOT trasladar ni reasignar tickets.
9. Asignación y desactivación de departamento concurrentes SHALL consolidar un único resultado válido.

**Trazabilidad:** DES-DU-005, DES-DU-007, DES-DU-009; TASK-DU-012, TASK-DU-015, TASK-DU-019; TEST-USR-025..029, TEST-PROV-USR-004.

### REQ-USR-005 Activar o desactivar usuario

#### Criterios de aceptación

1. `PATCH /api/v1/users/{id}/status` SHALL aceptar `isActive` y `version`.
2. Desactivar SHALL actualizar `IdentityAccount.IsActive` y `Domain.User.IsActive` dentro de una transacción y revocar todas las sesiones refresh activas.
3. Tickets `Open` o `InProgress` no bloquearán la desactivación; departamento, roles y tickets se conservarán.
4. Access tokens ya emitidos conservarán validez hasta su expiración aprobada; login y refresh posteriores serán rechazados.
5. Reactivar SHALL requerir que el departamento sea nulo o activo y conservar al menos un rol válido.
6. Reactivar SHALL NOT restablecer passwords, eliminar lockout temporal ni reactivar sesiones revocadas.
7. THE SYSTEM SHALL impedir la auto-desactivación del actor.
8. THE SYSTEM SHALL impedir desactivar al último administrador activo con `409 users.last_administrator`.
9. Solicitar el estado actual SHALL ser idempotente.
10. Una versión obsoleta SHALL devolver `409 users.version_conflict`.
11. Reglas de último administrador y cambio de estado SHALL resistir carreras concurrentes en ambos providers.

**Trazabilidad:** DES-DU-005, DES-DU-007, DES-DU-010; TASK-DU-013, TASK-DU-015, TASK-DU-019; TEST-USR-030..038, TEST-PROV-USR-005..006.

### REQ-USR-006 Administrar roles

#### Criterios de aceptación

1. `PUT /api/v1/users/{id}/roles` SHALL reemplazar el conjunto completo por una lista no vacía de roles canónicos y requerir `version`.
2. Roles desconocidos, vacíos o repetidos SHALL devolver `400`.
3. THE SYSTEM SHALL impedir que el actor retire de sí mismo `Administrator`.
4. THE SYSTEM SHALL impedir retirar `Administrator` del último administrador activo con `409 users.last_administrator`.
5. WHEN se retire cualquier rol, THE SYSTEM SHALL revocar las sesiones refresh activas del usuario.
6. Access tokens previos conservarán sus claims hasta expirar; esta spec no promete revocación inmediata.
7. La operación SHALL ejecutarse transaccionalmente y devolver `200` con nueva versión.
8. Una versión obsoleta SHALL devolver `409 users.version_conflict`.
9. Esta spec SHALL NOT crear, renombrar ni eliminar roles o policies.

**Trazabilidad:** DES-DU-007, DES-DU-009, DES-DU-010; TASK-DU-010, TASK-DU-012, TASK-DU-015, TASK-DU-019; TEST-USR-027..029, TEST-USR-035..038.

### REQ-USR-007 Consistencia Identity/Domain

#### Criterios de aceptación

1. Ningún caso de uso SHALL crear solo cuenta o solo perfil.
2. Una cuenta sin perfil o un perfil sin cuenta SHALL tratarse como inconsistencia inesperada y registrarse sin divulgar detalles internos.
3. El GUID correlacionado SHALL ser inmutable.
4. Las operaciones que modifican cuenta, perfil, roles o sesiones SHALL usar el mismo `ApplicationDbContext` y una transacción local explícita.
5. Las pruebas relacionales SHALL demostrar rollback ante fallo después de un `SaveChanges` interno de `UserManager`.

**Trazabilidad:** DES-DU-007, DES-DU-009; TASK-DU-010, TASK-DU-011, TASK-DU-019; TEST-PROV-USR-001..003.

## 6. Seguridad y autorización

### REQ-DU-SEC-001 Policy administrativa

1. Todos los endpoints de esta spec SHALL declarar `[Authorize(Policy = AuthorizationPolicies.UsersManage)]`.
2. Sin identidad válida SHALL responder `401`; con identidad pero sin policy, `403`, sin ejecutar el handler.
3. Controllers SHALL usar constantes y no comparar strings de roles.
4. No se introducirá una nueva policy hasta existir una necesidad de acceso distinta aprobada.

### REQ-DU-SEC-002 No divulgación y protección de datos

1. IDs inexistentes y no visibles SHALL compartir `404`, código y forma pública.
2. Conflictos de username/email SHALL usar un solo código público.
3. Responses, logs y ProblemDetails SHALL excluir passwords, hashes, tokens, stamps, claims completos, SQL y constraints.
4. Los DTOs administrativos podrán incluir email solo para actores con `Users.Manage`.
5. OpenAPI y tests SHALL usar exclusivamente datos ficticios.
6. Requests SHALL usar DTOs explícitos para evitar mass assignment.

## 7. Requisitos no funcionales

### REQ-DU-NF-001 Arquitectura

- Domain no dependerá de EF Core, Identity, ASP.NET Core ni providers.
- Application será propietaria de commands, queries, DTOs y puertos neutrales.
- Persistence implementará EF Core, Identity administrativo, transacciones y traducción de errores relacionales.
- API será composition root; controllers dependerán de `ISender`, no de DbContext/repositorios.
- No se expondrán `IQueryable`, `IdentityAccount`, `IdentityResult` ni tipos EF fuera de Persistence.

### REQ-DU-NF-002 Concurrencia y auditoría

- `Department` y `Domain.User` usarán un token GUID opaco, marcado como concurrency token y regenerado en cada mutación.
- Los clientes enviarán la versión actual en operaciones mutables.
- Conflictos se mapearán a `409` sin lost updates.
- `CreatedAtUtc` y `UpdatedAtUtc` serán gestionados centralmente mediante `IClock`; el cliente no los controlará.

### REQ-DU-NF-003 Persistencia portable

- Longitudes, nullability, índices y delete behaviors serán explícitos.
- La unicidad no dependerá de collations implícitas.
- Foreign keys de Identity→User, Department→User y referencias existentes serán restrictivas.
- Índices mínimos: `Department.NormalizedName` único; estado de departamento; `(User.DepartmentId, User.IsActive)`; estado de usuario; username/email normalizados según Identity.
- Se crearán migraciones incrementales separadas sin editar la migración inicial ya publicada.

### REQ-DU-NF-004 Rendimiento

- Listados usarán `AsNoTracking`, proyección y paginación en SQL.
- No habrá N+1.
- Toda ordenación será allowlisted y determinista.
- Se mantendrá el objetivo baseline p95 inferior a 500 ms en el entorno de referencia; la validación reproducible queda para release readiness.

### REQ-DU-NF-005 Contrato HTTP

- Rutas bajo `/api/v1/departments` y `/api/v1/users`.
- JSON `camelCase`, GUID, fechas UTC ISO 8601 y versión opaca.
- Éxitos: `200` o `201`; errores esperados: `400`, `401`, `403`, `404`, `409`, `415` mediante ProblemDetails/ValidationProblemDetails.
- Password será `writeOnly`, `format: password` en OpenAPI y no existirá en responses.
- Creaciones devolverán `Location`.

### REQ-DU-NF-006 Verificación dual-provider

La misma suite contractual SHALL ejecutarse sobre PostgreSQL y SQL Server reales para demostrar:

- apply/upgrade/rollback/reapply de migraciones;
- unicidad normalizada;
- FKs restrictivas;
- transacción cuenta/perfil/roles;
- rollback ante fallo intermedio;
- concurrencia de Department y User;
- carreras de asignación/desactivación y último administrador;
- consultas paginadas equivalentes.

Compilar o usar EF InMemory no constituirá evidencia relacional.

## 8. Contrato HTTP propuesto

### Departamentos

- `GET /api/v1/departments`
- `POST /api/v1/departments`
- `GET /api/v1/departments/{id}`
- `PUT /api/v1/departments/{id}`
- `PATCH /api/v1/departments/{id}/status`

### Usuarios

- `GET /api/v1/users`
- `POST /api/v1/users`
- `GET /api/v1/users/{id}`
- `PUT /api/v1/users/{id}`
- `PATCH /api/v1/users/{id}/department`
- `PATCH /api/v1/users/{id}/status`
- `PUT /api/v1/users/{id}/roles`

## 9. Salida esperada

- Departamentos y usuarios tienen administración autorizada, paginada y sin borrado físico.
- Identity y Domain permanecen consistentes y sus escrituras compuestas son atómicas.
- Desactivación y reducción de privilegios revocan sesiones refresh sin prometer revocación inmediata de JWT.
- Unicidad, concurrencia, restricciones, migraciones y consultas se verifican en ambos motores.
- OpenAPI, tests, tareas y documentación describen el mismo contrato.