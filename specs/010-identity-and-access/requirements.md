# 010 — Identity and Access: Requirements

**Estado:** Approved
**Fecha:** 6 de agosto de 2026
**Aprobada:** 6 de agosto de 2026
**Basado en:** Fase 2 del documento maestro: Identidad y seguridad
**Gate:** Aprobado por el usuario el 6 de agosto de 2026.

## 1. Objetivo

Incorporar autenticación local segura, sesiones renovables y autorización por policies para que las rutas protegidas identifiquen al actor y denieguen acceso por defecto, sin acoplar los casos de uso a ASP.NET Core Identity, JWT ni un proveedor de base de datos.

## 2. Alcance

Incluye:

- cuentas locales con nombre de usuario o email y contraseña protegida por ASP.NET Core Identity;
- inicio de sesión, rotación de refresh token y cierre/revocación de sesión;
- access tokens JWT de corta duración;
- refresh tokens aleatorios, de un solo uso, almacenados únicamente como hash;
- estado activo y lockout por intentos fallidos;
- roles iniciales y policies explícitas;
- población inicial segura y opt-in para Development;
- rate limiting para endpoints de autenticación;
- integración de `ICurrentUser`, OpenAPI y ProblemDetails;
- pruebas unitarias, arquitectónicas, de persistencia y de contrato HTTP.

No incluye:

- CRUD administrativo de usuarios o departamentos, que pertenece a la spec `020`;
- registro público, verificación de email, recuperación o cambio de contraseña;
- MFA, passkeys, cookies de sesión, OIDC/SSO o proveedores sociales;
- autorización por recurso para módulos aún no implementados;
- revocación inmediata de access tokens ya emitidos;
- frontend.

## 3. Actores y policies iniciales

Roles iniciales:

- `Administrator`;
- `Manager`;
- `Agent`;
- `Viewer`.

Policies iniciales:

- `Tickets.Read`;
- `Tickets.Create`;
- `Tickets.Assign`;
- `Tickets.ChangeStatus`;
- `Users.Manage`;
- `Dashboard.Read`.

La matriz exacta propuesta es:

- `Administrator`: todas las policies;
- `Manager`: `Tickets.Read`, `Tickets.Create`, `Tickets.Assign`, `Tickets.ChangeStatus`, `Dashboard.Read`;
- `Agent`: `Tickets.Read`, `Tickets.Create`, `Tickets.ChangeStatus`;
- `Viewer`: `Tickets.Read`, `Dashboard.Read`.

Esta matriz es un baseline global. Las specs funcionales posteriores deberán restringir además el acceso por departamento, propiedad o recurso.

## 4. Requisitos funcionales

### REQ-AUTH-001 Autenticación local

**Historia:** Como usuario interno activo, quiero autenticarme con mis credenciales para recibir una sesión de API.

#### Criterios de aceptación

1. WHEN se presentan identificador y contraseña válidos para una cuenta activa y no bloqueada, THE SYSTEM SHALL devolver un access token y un refresh token.
2. WHEN las credenciales son inválidas, la cuenta no existe, está inactiva o está bloqueada, THE SYSTEM SHALL devolver `401` con el mismo código público `auth.invalid_credentials` y sin revelar la causa.
3. WHEN el body es inválido, THE SYSTEM SHALL devolver `400` mediante `ValidationProblemDetails` sin intentar autenticar.
4. WHEN una autenticación falla, THE SYSTEM SHALL aplicar el contador de intentos de Identity sin registrar contraseñas ni tokens.
5. WHEN una autenticación tiene éxito, THE SYSTEM SHALL reiniciar el contador aplicable y registrar solo metadatos operativos no sensibles.

**Trazabilidad:** DES-AUTH-001, DES-AUTH-003; TASK-SEC-003, TASK-SEC-006; TEST-AUTH-001..006.

### REQ-AUTH-002 Access token

#### Criterios de aceptación

1. WHEN el login o refresh tiene éxito, THE SYSTEM SHALL emitir un JWT firmado con duración propuesta de 15 minutos.
2. THE SYSTEM SHALL incluir únicamente `sub`, `name`, `jti`, roles, `iat`, `nbf` y `exp`, además de issuer y audience validados.
3. THE SYSTEM SHALL usar UTC y aceptar como máximo 30 segundos de desfase de reloj.
4. IF issuer, audience, firma, vigencia o algoritmo no son válidos, THEN THE SYSTEM SHALL devolver `401` antes de ejecutar el endpoint protegido.
5. THE SYSTEM SHALL obtener clave, issuer y audience desde configuración segura y fallar al iniciar fuera de Testing si faltan o son inseguros.

**Trazabilidad:** DES-AUTH-002, DES-AUTH-007; TASK-SEC-004, TASK-SEC-008; TEST-AUTH-007..012.

### REQ-AUTH-003 Refresh token rotatorio

#### Criterios de aceptación

1. WHEN se crea una sesión, THE SYSTEM SHALL generar un refresh token criptográficamente aleatorio con duración propuesta de 7 días y persistir solo su hash.
2. WHEN se presenta un refresh token vigente, no revocado y no usado, THE SYSTEM SHALL revocarlo, crear su reemplazo y emitir un nuevo par de tokens dentro de una transacción.
3. WHEN se reutiliza un token ya reemplazado, THE SYSTEM SHALL revocar la familia de sesión activa y devolver `401`.
4. WHEN el token es desconocido, expiró, fue revocado o pertenece a una cuenta inactiva, THE SYSTEM SHALL devolver el mismo `401` con código `auth.invalid_refresh_token`.
5. THE SYSTEM SHALL conservar como metadatos máximos de sesión: identificador, usuario, hash, familia, creación, expiración, revocación, reemplazo y una descripción opcional de dispositivo limitada; no almacenará el token en texto claro.

**Trazabilidad:** DES-AUTH-004, DES-AUTH-005; TASK-SEC-002, TASK-SEC-005; TEST-AUTH-013..021.

### REQ-AUTH-004 Cierre de sesión

#### Criterios de aceptación

1. WHEN un cliente presenta un refresh token de sesión conocido, THE SYSTEM SHALL revocar esa sesión y devolver `204`.
2. WHEN el token es desconocido o ya fue revocado, THE SYSTEM SHALL devolver `204` para mantener el cierre idempotente y no revelar sesiones.
3. Logout SHALL NOT promise immediate invalidation of an already issued access token; its lifetime remains bounded by `REQ-AUTH-002`.

**Trazabilidad:** DES-AUTH-004; TASK-SEC-005; TEST-AUTH-022..025.

### REQ-AUTH-005 Estado y lockout

#### Criterios de aceptación

1. IF una cuenta está inactiva, THEN THE SYSTEM SHALL reject login and refresh without revealing account status.
2. WHEN cinco intentos fallidos ocurren dentro de la ventana de Identity, THE SYSTEM SHALL bloquear temporalmente la cuenta durante 15 minutos.
3. Administrator accounts SHALL follow the same lockout rule; el seed no deshabilitará controles de seguridad.
4. Estado inactivo y lockout SHALL NOT be encoded as authorization claims that remain valid beyond the access-token lifetime.

**Trazabilidad:** DES-AUTH-001, DES-AUTH-003; TASK-SEC-003, TASK-SEC-006; TEST-AUTH-026..031.

### REQ-AUTH-006 Autorización por policies

#### Criterios de aceptación

1. WHEN no existe una identidad autenticada, THE SYSTEM SHALL devolver `401` para toda ruta protegida.
2. WHEN existe identidad pero faltan los requisitos de la policy, THE SYSTEM SHALL devolver `403` y no ejecutar el caso de uso.
3. THE SYSTEM SHALL deny by default: únicamente login, refresh, logout, liveness/readiness y documentación habilitada explícitamente serán anónimos.
4. Controllers SHALL reference policy constants, nunca comparar nombres de rol mediante strings dentro de acciones.
5. Las reglas de visibilidad por recurso SHALL ejecutarse posteriormente en Application y podrán responder `404` para evitar divulgación.

**Trazabilidad:** DES-AUTH-006; TASK-SEC-007, TASK-SEC-008; TEST-AUTH-032..040.

### REQ-AUTH-007 Usuario actual

#### Criterios de aceptación

1. WHEN un JWT válido contiene `sub`, THE SYSTEM SHALL exponer el GUID mediante `ICurrentUser.UserId`.
2. WHEN no existe identidad válida, `ICurrentUser` SHALL exponer valores nulos y no inventar un actor.
3. Application SHALL NOT depend on `ClaimsPrincipal`, `HttpContext`, Identity or JWT types.

**Trazabilidad:** DES-AUTH-006; TASK-SEC-007; TEST-AUTH-041..043.

### REQ-AUTH-008 Seed de desarrollo

#### Criterios de aceptación

1. WHEN Development seed is explicitly enabled and required secret values are provided, THE SYSTEM SHALL create or reconcile the initial roles and one administrator account idempotently.
2. WHEN seed is disabled, THE SYSTEM SHALL NOT create credentials.
3. Passwords, signing keys and real account data SHALL NOT be committed to source control, emitted to logs or included in OpenAPI examples.
4. Outside Development, automatic account seeding SHALL be disabled.

**Trazabilidad:** DES-AUTH-008; TASK-SEC-009; TEST-AUTH-044..048.

### REQ-AUTH-009 Protección de endpoints de autenticación

#### Criterios de aceptación

1. Login SHALL apply a proposed fixed-window limit of 5 requests per minute per normalized client partition.
2. Refresh SHALL apply a proposed limit of 30 requests per minute per normalized client partition.
3. WHEN the limit is exceeded, THE SYSTEM SHALL return `429` with `Retry-After` and no account-specific information.
4. Rate-limit partition keys SHALL NOT include plaintext credentials or refresh tokens.
5. Request bodies SHALL be JSON and constrained by the API request-size baseline.

**Trazabilidad:** DES-AUTH-009; TASK-SEC-010; TEST-AUTH-049..054.

## 5. Requisitos no funcionales

### REQ-AUTH-NF-001 Límites arquitectónicos

- Application será propietaria de los puertos y modelos de autenticación que consume.
- Infrastructure implementará firma JWT y adaptadores técnicos no persistentes.
- Persistence implementará el store de Identity y sesiones sobre EF Core.
- Domain y Application no referenciarán ASP.NET Core Identity, JWT, EF Core ni proveedores de base de datos.

### REQ-AUTH-NF-002 Portabilidad

El esquema de Identity, las sesiones y sus restricciones deberán funcionar en PostgreSQL y SQL Server con migraciones separadas y el mismo contrato observable.

### REQ-AUTH-NF-003 Seguridad de secretos

Comparaciones de refresh-token hash usarán operaciones resistentes a timing cuando aplique. Tokens, contraseñas, claves y connection strings no aparecerán en logs, errores ni datos de prueba versionados.

### REQ-AUTH-NF-004 Testabilidad

Reloj, generación aleatoria, emisión de tokens y actor actual serán sustituibles en pruebas. Los casos límite de expiración y rotación no dependerán del reloj real.

### REQ-AUTH-NF-005 Compatibilidad HTTP

Los endpoints se publicarán bajo `/api/v1/auth`, usarán JSON `camelCase`, responderán ProblemDetails en errores y estarán documentados con el esquema bearer en OpenAPI.

## 6. Contrato HTTP propuesto

### `POST /api/v1/auth/login`

Request:

```json
{
  "identifier": "agent@example.test",
  "password": "<secret>"
}
```

Success `200`:

```json
{
  "accessToken": "<jwt>",
  "accessTokenExpiresAtUtc": "2026-08-06T22:15:00Z",
  "refreshToken": "<opaque-secret>",
  "refreshTokenExpiresAtUtc": "2026-08-13T22:00:00Z",
  "tokenType": "Bearer"
}
```

### `POST /api/v1/auth/refresh`

Request:

```json
{
  "refreshToken": "<opaque-secret>"
}
```

Success `200`: mismo contrato de token pair que login.

### `POST /api/v1/auth/logout`

Request:

```json
{
  "refreshToken": "<opaque-secret>"
}
```

Success: `204 No Content`.

Los refresh tokens se transportan en body porque el alcance actual es una API backend sin contrato de navegador. Una spec de frontend podrá proponer cookies `HttpOnly` con protección CSRF como cambio explícito.

## 7. Decisiones que requieren aprobación

Aprobar esta spec confirma:

1. access token de 15 minutos y refresh token de 7 días;
2. refresh token opaco en body, no cookie;
3. rotación de un solo uso con revocación de familia ante reutilización;
4. lockout después de 5 fallos durante 15 minutos;
5. matriz inicial de roles y policies;
6. rate limits propuestos de 5/min para login y 30/min para refresh;
7. separación entre cuenta técnica de Identity y perfil de dominio `User`, compartiendo el mismo GUID.

## 8. Criterio de salida

- login, refresh, reutilización, logout, lockout y cuenta inactiva tienen pruebas positivas y negativas;
- una ruta protegida demuestra `401`, `403` y éxito según policy;
- ningún token o secreto se persiste o registra en texto claro;
- migraciones y pruebas aplicables pasan en PostgreSQL y SQL Server;
- OpenAPI representa bearer auth y los contratos HTTP;
- restore locked, format, build CI y todas las suites terminan en verde;
- requirements, design, tasks, README y evidencia quedan sincronizados.
