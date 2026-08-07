# 010 — Identity and Access: Tasks

**Estado:** Implementing
**Fecha:** 6 de agosto de 2026
**Aprobada:** 6 de agosto de 2026
**Requisitos:** `requirements.md`
**Diseño:** `design.md`

## Convenciones

- Ninguna tarea de implementación comienza hasta cerrar `GATE-SEC-000`.
- Cada checkbox requiere código, pruebas y evidencia ejecutable.
- Los checks estrechos se ejecutan primero; format, build estricto y suite completa cierran cada ola.
- Secretos, contraseñas y tokens reales no se escriben en archivos versionados, comandos registrados como evidencia ni logs.
- Las pruebas que afirman portabilidad, transacciones o concurrencia deben usar PostgreSQL y SQL Server reales; EF InMemory no constituye evidencia suficiente.

## Gate 0 — Aprobación

- [x] **GATE-SEC-000 Aprobar requirements, design y tasks**
  - Aprobado por el usuario sin cambios el 6 de agosto de 2026.
  - Confirmar las siete decisiones de `requirements.md` sección 7.
  - Registrar cualquier cambio de contrato antes de implementar.
  - Salida observable: los tres artefactos pasan de `Proposed` a `Approved` en un commit documental.

## Ola 1 — Modelo y contratos

- [x] **TASK-SEC-001 Agregar dependencias y opciones de autenticación**
  - Requisitos: REQ-AUTH-002, REQ-AUTH-NF-001.
  - Agregar versiones centrales y referencias mínimas para Identity EF, JWT bearer y testing requerido.
  - Crear opciones tipadas para JWT, refresh, lockout y seed con validación de arranque.
  - No incluir defaults secretos en configuración versionada.
  - Pruebas: configuración válida/inválida y ambientes Testing/Development/Production.
  - Verificación: restore locked, build estricto de proyectos afectados.

- [x] **TASK-SEC-002 Implementar cuenta Identity y refresh-session persistence model**
  - Requisitos: REQ-AUTH-003, REQ-AUTH-NF-002.
  - Integrar Identity con GUID en `ApplicationDbContext` sin exponer sus tipos a Domain/Application.
  - Agregar `RefreshTokenSession`, configuraciones, índices, concurrency token y relaciones restrictivas.
  - Correlacionar cuenta técnica y perfil Domain `User` por el mismo GUID.
  - Pruebas: model metadata, restricciones e invariantes de sesión.
  - Verificación: Persistence integration tests estrechos.

- [x] **TASK-SEC-003 Definir e implementar puertos de autenticación de cuentas**
  - Requisitos: REQ-AUTH-001, REQ-AUTH-005, REQ-AUTH-NF-001.
  - Crear modelos/puertos neutrales en Application.
  - Implementar adapter con Identity para lookup normalizado, password verification, estado activo, roles y lockout.
  - Unificar errores públicos para cuenta ausente, inactiva, bloqueada o contraseña errónea.
  - Pruebas: éxito, cada causa de fallo, contador y reset de lockout.
  - Verificación: Application unit + Persistence integration tests afectados.

- [x] **TASK-SEC-004 Implementar emisión de access tokens**
  - Requisitos: REQ-AUTH-002.
  - Implementar `IAccessTokenIssuer` en Infrastructure con reloj inyectado, algoritmo fijado y claims mínimos.
  - Validar issuer, audience, key entropy, lifetime y clock skew al iniciar.
  - Pruebas: claims, firma, expiración, issuer/audience/algoritmo inválidos y ausencia de PII.
  - Verificación: Infrastructure/Application unit tests y build estricto.

## Ola 2 — Casos de uso de sesión

- [ ] **TASK-SEC-005 Implementar generación, rotación, replay handling y logout**
  - Requisitos: REQ-AUTH-003, REQ-AUTH-004, REQ-AUTH-NF-003, REQ-AUTH-NF-004.
  - Generar token opaco criptográfico, retornar plaintext una sola vez y persistir hash.
  - Implementar repositorio de sesión y handlers de refresh/logout.
  - Rotar dentro de una Unit of Work y revocar familia ante replay.
  - Resolver doble refresh concurrente sin emitir dos sucesores.
  - Pruebas: success, unknown, expired, revoked, replay, concurrency y logout idempotente.
  - Verificación: unit tests + contrato de persistencia en ambos providers.

- [ ] **TASK-SEC-006 Implementar login**
  - Requisitos: REQ-AUTH-001, REQ-AUTH-005.
  - Crear request/command/validator/handler/result.
  - Coordinar autenticación, access token, refresh session y commit.
  - No registrar identifier completo, password ni token.
  - Pruebas: validación, éxito, generic failures, lockout, inactive y rollback si falla persistencia.
  - Verificación: Application unit + Persistence integration tests.

- [x] **TASK-SEC-007 Integrar policies e `ICurrentUser`**
  - Requisitos: REQ-AUTH-006, REQ-AUTH-007.
  - Crear constantes únicas para roles/policies y registrar matriz aprobada.
  - Configurar fallback policy de autenticación y excepciones anónimas explícitas.
  - Robustecer `CurrentUserAccessor` para `sub` GUID y nombre.
  - Aplicar `Tickets.Create` al endpoint vertical existente sin ampliar su contrato.
  - Pruebas: claims ausentes/malformados, matriz policy y no ejecución al denegar.
  - Verificación: unit + API integration + architecture tests.

## Ola 3 — API y hardening

- [ ] **TASK-SEC-008 Publicar endpoints y contrato bearer**
  - Requisitos: REQ-AUTH-001..007, REQ-AUTH-NF-005.
  - Implementar `POST /api/v1/auth/login`, `/refresh` y `/logout` con DTOs HTTP separados.
  - Configurar JwtBearer challenge/forbid con ProblemDetails seguro.
  - Documentar esquema bearer, endpoints, respuestas y ejemplos ficticios en OpenAPI.
  - Pruebas: contratos `200/204/400/401/403`, security metadata y ausencia de secretos.
  - Verificación: API integration tests y generación OpenAPI.

- [x] **TASK-SEC-009 Implementar seed seguro de Development**
  - Requisitos: REQ-AUTH-008.
  - Crear initializer idempotente de roles, cuenta Administrator y perfil Domain con GUID compartido.
  - Requerir enable flag y secretos externos; bloquear seed fuera de Development.
  - Documentar `dotnet user-secrets` usando placeholders.
  - Pruebas: disabled, missing secrets, first run, repeated run y non-Development.
  - Verificación: integration tests y revisión de archivos/configuración por secretos.

- [ ] **TASK-SEC-010 Configurar rate limiting y límites de request**
  - Requisitos: REQ-AUTH-009.
  - Agregar policies nombradas de 5/min login y 30/min refresh con queue deshabilitada.
  - Implementar partición normalizada/hasheada sin credenciales y respuesta `429` con `Retry-After`.
  - Restringir auth a JSON y body limitado.
  - Pruebas: límite por endpoint, particiones independientes, window reset y cuerpo seguro.
  - Verificación: API integration tests.

## Ola 4 — Migraciones y matriz de providers

- [ ] **TASK-SEC-011 Crear migraciones de identidad para PostgreSQL y SQL Server**
  - Requisitos: REQ-AUTH-003, REQ-AUTH-NF-002.
  - Crear migraciones separadas para Identity, perfil correlacionado y refresh sessions.
  - Revisar nombres, longitudes, índices, foreign keys, delete behavior y concurrency.
  - Probar apply desde base vacía y rollback soportado para ambos providers.
  - Evidencia: comandos, migraciones aplicadas y esquema validado sin connection strings.

- [ ] **TASK-SEC-012 Ejecutar contrato de autenticación en ambos providers**
  - Requisitos: todos los requisitos funcionales y REQ-AUTH-NF-002.
  - Ejecutar la misma suite de login/session/lockout/concurrency sobre PostgreSQL y SQL Server efímeros.
  - Confirmar que no hay tests ignorados ni branches específicos sin fallback.
  - Evidencia: conteos por provider y versión de imagen/engine usada.

## Ola 5 — Cierre y evidencia

- [x] **TASK-SEC-013 Ejecutar seguridad y calidad estática**
  - Buscar secretos y usos inseguros: plaintext refresh tokens, logging de request bodies, `AllowAnyOrigin` con credentials, algoritmos aceptados dinámicamente y claims sensibles.
  - Ejecutar auditoría de paquetes con las herramientas del repositorio y clasificar cualquier advisory.
  - Verificar que Domain/Application no contienen referencias de Identity/JWT/ASP.NET auth.

- [ ] **TASK-SEC-014 Ejecutar checks equivalentes a CI**
  - `dotnet tool restore`.
  - `dotnet restore InternalOperations.slnx --locked-mode`.
  - `dotnet format InternalOperations.slnx --verify-no-changes --no-restore`.
  - `dotnet build InternalOperations.slnx --configuration Release --no-restore -p:ContinuousIntegrationBuild=true`.
  - `dotnet test InternalOperations.slnx --configuration Release --no-build --no-restore`.
  - Ejecutar adicionalmente la matriz real PostgreSQL/SQL Server definida por esta spec si aún no forma parte del comando global.
  - Salida requerida: 0 warnings, 0 errores, 0 tests fallidos y 0 tests omitidos sin justificación.

- [ ] **TASK-SEC-015 Sincronizar spec y documentación**
  - Actualizar evidencia por requisito/tarea, README, configuración de ejemplo, OpenAPI y runbook de Development.
  - Marcar `requirements.md`, `design.md` y `tasks.md` como `Completed` solo cuando toda evidencia sea reproducible.
  - Confirmar working tree limpio, commit publicado y hosted CI observado por separado de los checks locales.

## Matriz de trazabilidad

- REQ-AUTH-001 → DES-AUTH-001/003 → TASK-SEC-003/006/008 → TEST-AUTH-001..006.
- REQ-AUTH-002 → DES-AUTH-002/007 → TASK-SEC-001/004/008 → TEST-AUTH-007..012.
- REQ-AUTH-003 → DES-AUTH-004/005 → TASK-SEC-002/005/011/012 → TEST-AUTH-013..021.
- REQ-AUTH-004 → DES-AUTH-004 → TASK-SEC-005/008 → TEST-AUTH-022..025.
- REQ-AUTH-005 → DES-AUTH-001/003 → TASK-SEC-003/006/012 → TEST-AUTH-026..031.
- REQ-AUTH-006 → DES-AUTH-006 → TASK-SEC-007/008 → TEST-AUTH-032..040.
- REQ-AUTH-007 → DES-AUTH-006 → TASK-SEC-007 → TEST-AUTH-041..043.
- REQ-AUTH-008 → DES-AUTH-008 → TASK-SEC-009 → TEST-AUTH-044..048.
- REQ-AUTH-009 → DES-AUTH-009 → TASK-SEC-010 → TEST-AUTH-049..054.

## Evidencia del checkpoint de implementación

- Restore locked: `dotnet restore InternalOperations.slnx --locked-mode` completó correctamente.
- Formato: `dotnet format InternalOperations.slnx --verify-no-changes --no-restore` completó correctamente.
- Build Release CI: compiló con `0` errores. Este host ARM reporta `NETSDK1188` para recursos localizados de paquetes de terceros; no se atribuyen al código, pero `TASK-SEC-014` permanece abierta porque el criterio exige `0` warnings.
- Suite local: `58/58` pruebas aprobadas, `0` fallos y `0` omitidas:
  - API Integration: `24`.
  - Application Unit: `16`.
  - Architecture: `10`.
  - Domain Unit: `1`.
  - Persistence Integration: `7`.
- Auditoría NuGet: ningún paquete vulnerable detectado mediante `dotnet list InternalOperations.slnx package --vulnerable --include-transitive`.
- Revisión de seguridad: sin secretos de autenticación versionados, sin refresh tokens persistidos en plaintext, algoritmo JWT fijado, límites aprobados validados y fronteras Domain/Application protegidas por los `8` architecture tests.
- Migraciones: `InitialIdentityAndAccess` existe en assemblies separados para PostgreSQL y SQL Server; ambos snapshots están sincronizados (`dotnet ef migrations has-pending-model-changes`) y generan scripts idempotentes de `321` y `354` líneas respectivamente.
- Pendiente reproducible: no hubo ejecución real sobre PostgreSQL/SQL Server. Docker CLI está presente, pero el daemon no está disponible en este host; por eso `TASK-SEC-012` y las tareas que requieren contrato real de providers siguen abiertas.

## Salida esperada

- Fase 2 protege por defecto las rutas y expone contratos de sesión verificables.
- Login, refresh, replay, logout, lockout y autorización están probados positiva y negativamente.
- Identity y JWT permanecen fuera de Domain/Application.
- Secrets y refresh tokens nunca quedan en texto claro.
- PostgreSQL y SQL Server cumplen el mismo contrato.
- La siguiente spec desbloqueada será `020-departments-and-users`.
