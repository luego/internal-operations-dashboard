# 020 — Departments and Users: Tasks

**Estado:** Proposed
**Fecha:** 7 de agosto de 2026
**Requisitos:** `requirements.md`
**Diseño:** `design.md`
**Dependencia:** `../010-identity-and-access/`
**Gate de implementación:** pendiente de aprobación explícita

## Convenciones

- Ninguna tarea de código comienza antes de cerrar `GATE-DU-000` y `GATE-DU-001`.
- Cada corte sigue RED → GREEN → REFACTOR y registra evidencia ejecutada.
- Un checkbox requiere tests asociados, format y build estricto; existir o compilar no es evidencia suficiente.
- EF InMemory no demuestra constraints, transacciones, concurrencia, migraciones ni portabilidad.
- Toda afirmación dual-provider requiere PostgreSQL y SQL Server reales.
- La suite foundation de 58 tests es baseline de no regresión, no evidencia de esta spec.
- Commits y push son gates separados; se acumularán commits locales hasta autorización de publicación.

## Gate 0 — Aprobación y dependencias

- [ ] **GATE-DU-000 Aprobar requirements, design y tasks**
  - Resolver o aceptar las decisiones funcionales propuestas.
  - Reconciliar IDs `REQ-*`, `DES-*`, `TASK-*` y `TEST-*`.
  - Registrar aprobación explícita y fecha.
  - Cambiar los tres documentos de `Proposed` a `Approved` en un commit documental.

- [ ] **GATE-DU-001 Cerrar dependencia 010**
  - Publicar los commits locales de migraciones/provider matrix cuando el usuario lo autorice.
  - Observar foundation, PostgreSQL y SQL Server exitosos en hosted CI.
  - Reconciliar y cerrar honestamente `TASK-SEC-011`, `TASK-SEC-012`, `TASK-SEC-014` y `TASK-SEC-015`.
  - `requirements.md`, `design.md` y `tasks.md` de 010 deben pasar juntos a `Completed`.
  - Alternativa: waiver explícito que delimite riesgo; no se presume.

## Ola 1 — Dominio y contratos

- [ ] **TASK-DU-001 Endurecer Department con TDD**
  - Requisitos: `REQ-DEP-001`, `REQ-DEP-003`, `REQ-DEP-004`, `REQ-DU-NF-002`.
  - Diseño: `DES-DU-001`.
  - Encapsular creación, edición, estado, normalización y versión.
  - Tests `TEST-DEP-001..006`: creación válida; límites; Form KC/whitespace/case; update; estado idempotente; rotación de versión.
  - Verificación: Domain unit tests + architecture tests.

- [ ] **TASK-DU-002 Endurecer Domain.User con TDD**
  - Requisitos: `REQ-USR-001`, `REQ-USR-003..005`, `REQ-USR-007`.
  - Diseño: `DES-DU-002`.
  - Encapsular perfil, asignación, estado y versión sin introducir Identity en Domain.
  - Tests `TEST-USR-001..006`: creación/GUID; límites; update; asignar/quitar; estado idempotente; versión.
  - Verificación: Domain unit tests + architecture tests.

- [ ] **TASK-DU-003 Definir paginación, DTOs y puertos neutrales**
  - Requisitos: `REQ-DEP-002`, `REQ-USR-002`, `REQ-DU-NF-001`, `REQ-DU-NF-004`.
  - Diseño: `DES-DU-003`, `DES-DU-006`.
  - Crear filtros/allowlists, envelope y puertos específicos sin `IQueryable`/EF/Identity.
  - Tests `TEST-DU-001..006`: defaults 25/100; límites; combinaciones; allowlists; DTO safety; cancellation.
  - Verificación: Application unit + architecture tests.

## Ola 2 — Persistencia base

- [ ] **TASK-DU-004 Configurar mappings explícitos**
  - Requisitos: `REQ-DEP-001..004`, `REQ-USR-001..007`, `REQ-DU-NF-002..003`.
  - Diseño: `DES-DU-004`, `DES-DU-005`.
  - Configurar longitudes, nullability, índices, FKs restrictivas y concurrency tokens.
  - Tests `TEST-PER-DU-001..007`: metadata, unicidad, índices, relaciones, shared PK/FK y concurrencia.
  - No cerrar basándose solo en snapshots.

- [ ] **TASK-DU-005 Implementar auditoría central**
  - Requisitos: `REQ-DU-NF-002`.
  - Diseño: `DES-DU-005`.
  - Usar `IClock`/`ICurrentUser`; preservar creación y actualizar modificación en UTC.
  - Tests `TEST-AUD-DU-001..005`: create, update, reloj, actor sistema/autenticado y rollback.

- [ ] **TASK-DU-006 Implementar transacción explícita compartida**
  - Requisitos: `REQ-USR-001`, `REQ-USR-003`, `REQ-USR-005..007`.
  - Diseño: `DES-DU-005`, `DES-DU-007`, `DES-DU-009`.
  - Implementar `IAtomicOperation` sobre execution strategy y DbContext scoped compartido.
  - Tests `TEST-TXN-DU-001..006`: commit, Result failure rollback, exception rollback, UserManager SaveChanges, retry y tracker cleanup.
  - Atomicidad real queda pendiente de provider matrix.

- [ ] **TASK-DU-007 Implementar adapters/read services**
  - Requisitos: `REQ-DEP-002`, `REQ-USR-002`, `REQ-DU-NF-004`.
  - Diseño: `DES-DU-003`, `DES-DU-006`, `DES-DU-009`.
  - Implementar repositorios tracked, proyecciones no-tracking, Identity admin y revocación por usuario.
  - Tests `TEST-PER-DU-008..016`: filtros, orden estable, joins, no sensitive fields, error translation y revocación.

- [ ] **TASK-DU-008 Generar migraciones incrementales dual-provider**
  - Requisitos: `REQ-DU-NF-003`, `REQ-DU-NF-006`.
  - Diseño: `DES-DU-004`, `DES-DU-011`.
  - Crear `DepartmentsAndUsers` en ambos assemblies sin editar `InitialIdentityAndAccess`.
  - Revisar backfill, duplicados, longitudes, índices, FKs, versiones, email único filtrado y `Down`.
  - `TEST-MIG-DU-001..006`: apply vacío, upgrade desde 010 y rollback/reapply en cada provider.
  - No cerrar con scripts/snapshots sin bases reales.

## Ola 3 — Departments vertical slices

- [ ] **TASK-DU-009 Implementar create/get Department**
  - Requisitos: `REQ-DEP-001`, `REQ-DEP-002`.
  - Diseño: `DES-DU-001`, `DES-DU-008`.
  - Tests Application `TEST-DEP-007..012`: éxito, duplicate pre-check/race mapping, validación, get, not found y cancellation.
  - Implementar command/query, handlers y adapters mínimos.

- [ ] **TASK-DU-010 Implementar list/update Department**
  - Requisitos: `REQ-DEP-002`, `REQ-DEP-003`.
  - Diseño: `DES-DU-003`, `DES-DU-006`, `DES-DU-008`.
  - Tests `TEST-DEP-013..020`: página, filtros, search, sort, update, duplicate y stale version.

- [ ] **TASK-DU-011 Implementar estado Department**
  - Requisitos: `REQ-DEP-004`.
  - Diseño: `DES-DU-005`, `DES-DU-007`, `DES-DU-008`.
  - Tests `TEST-DEP-021..026`: deactivate, idempotencia, not found, active work, carrera y no cascade.

## Ola 4 — Users vertical slices

- [ ] **TASK-DU-012 Implementar adapter administrativo Identity**
  - Requisitos: `REQ-USR-001`, `REQ-USR-003`, `REQ-USR-005..007`.
  - Diseño: `DES-DU-009`, `DES-DU-010`.
  - Encapsular username/email/password/roles/state y traducir errores a códigos estables.
  - Tests `TEST-USR-007..012`: normalización, conflicto uniforme, password, roles, redacción y cancellation.

- [ ] **TASK-DU-013 Implementar create/get User atómico**
  - Requisitos: `REQ-USR-001`, `REQ-USR-002`, `REQ-USR-007`.
  - Diseño: `DES-DU-005`, `DES-DU-009`.
  - Tests `TEST-USR-013..020`: éxito, departamento ausente/inactivo, identificador, fallo de rol/perfil, GUID compartido, DTO seguro y not found.
  - No cerrar atomicidad con EF InMemory.

- [ ] **TASK-DU-014 Implementar list/update/asignación/roles User**
  - Requisitos: `REQ-USR-002..004`, `REQ-USR-006`.
  - Diseño: `DES-DU-003`, `DES-DU-006`, `DES-DU-007`, `DES-DU-009`, `DES-DU-010`.
  - Tests `TEST-USR-021..029`: list/filter/search, update sincronizado, assignment, inactive department, roles, rollback y stale version.

- [ ] **TASK-DU-015 Implementar estado User y revocación**
  - Requisitos: `REQ-USR-005`, `REQ-USR-007`.
  - Diseño: `DES-DU-005`, `DES-DU-007`, `DES-DU-010`.
  - Tests `TEST-USR-030..038`: sincronización, revoke sessions, login/refresh denied, reactivation, idempotencia, self/last admin, rollback y carrera.
  - Ejecutar regresión completa de autenticación.

## Ola 5 — HTTP, seguridad y OpenAPI

- [ ] **TASK-DU-016 Publicar endpoints Department**
  - Requisitos: `REQ-DEP-001..004`, `REQ-DU-SEC-001..002`, `REQ-DU-NF-005`.
  - Diseño: `DES-DU-008`, `DES-DU-012`.
  - Publicar cinco endpoints y `201 + Location`.
  - Tests `TEST-HTTP-DEP-001..011`: happy paths, validation, auth, 404/409, filters, limits y DTO/nullability.

- [ ] **TASK-DU-017 Publicar endpoints User**
  - Requisitos: `REQ-USR-001..007`, `REQ-DU-SEC-001..002`, `REQ-DU-NF-005`.
  - Diseño: `DES-DU-009`, `DES-DU-010`, `DES-DU-012`.
  - Publicar siete endpoints con requests explícitos.
  - Tests `TEST-HTTP-USR-001..014`: happy paths, validation, auth, 404/409, filters, no leakage, rollback, mass assignment y password redaction.

- [ ] **TASK-DU-018 Completar seguridad y OpenAPI**
  - Requisitos: `REQ-DU-SEC-001..002`, `REQ-DU-NF-005`.
  - Diseño: `DES-DU-012`.
  - Verificar policy constante, denegación antes del handler y schemas/responses/security.
  - Tests `TEST-SEC-DU-001..009` y `TEST-OAS-DU-001..007`.
  - OpenAPI nunca contendrá password en response/example ni campos técnicos Identity.

## Ola 6 — Provider matrix

- [ ] **TASK-DU-019 Ampliar contrato real de Departments**
  - Requisitos: `REQ-DEP-*`, `REQ-DU-NF-006`.
  - Misma prueba en PostgreSQL y SQL Server.
  - `TEST-PROV-DEP-001..006`: unicidad, longitudes, queries, concurrency, restrict y UTC.

- [ ] **TASK-DU-020 Ampliar contrato real de Users**
  - Requisitos: `REQ-USR-*`, `REQ-DU-NF-006`.
  - Misma prueba en PostgreSQL y SQL Server.
  - `TEST-PROV-USR-001..008`: atomic create, rollback, shared PK, assignment, status/revoke, concurrency, queries y last-admin race.
  - Evidencia por provider: imagen, run exacto, número ejecutado, cero fallos y cero skips.

## Ola 7 — Calidad y cierre

- [ ] **TASK-DU-021 Auditoría de seguridad y arquitectura**
  - Buscar mass assignment, exposición de Identity/PII, passwords/tokens/stamps en logs, strings de roles, bypass de policy y puertos contaminados.
  - Auditar dependencias y paquetes.
  - Hallazgos aceptados requieren riesgo, owner y fecha.

- [ ] **TASK-DU-022 Ejecutar gates equivalentes a CI**
  - tool restore;
  - restore locked;
  - format verify;
  - build Release `ContinuousIntegrationBuild=true`, cero warnings/errores;
  - foundation suite, cero fallos/skips;
  - PostgreSQL provider contracts;
  - SQL Server provider contracts.
  - El baseline exige los 58 tests actuales más toda prueba 020.

- [ ] **TASK-DU-023 Sincronizar documentación y evidencia**
  - Actualizar README, OpenAPI, specs y runbook de migración.
  - Registrar resultados reales sin confundir local, commit, push y hosted CI.
  - Confirmar diff limpio y trazabilidad bidireccional.

- [ ] **GATE-DU-999 Completar la spec**
  - Todos los requisitos/tareas tienen evidencia reproducible.
  - Ambos providers cumplen el mismo contrato.
  - OpenAPI coincide con HTTP observado.
  - Requirements, design y tasks pasan juntos a `Completed`.
  - No cerrar mientras queden migraciones, provider matrix, tests o documentación requeridos.

## Matriz resumida de trazabilidad

- `REQ-DEP-001` → `DES-DU-001/004/008` → `TASK-DU-001/004/009/016/019`.
- `REQ-DEP-002` → `DES-DU-003/006/008` → `TASK-DU-003/007/009/010/016`.
- `REQ-DEP-003` → `DES-DU-001/005/008` → `TASK-DU-001/010/016/019`.
- `REQ-DEP-004` → `DES-DU-005/007/008` → `TASK-DU-011/016/019`.
- `REQ-USR-001` → `DES-DU-002/005/009` → `TASK-DU-002/006/012/013/017/020`.
- `REQ-USR-002` → `DES-DU-003/006/009` → `TASK-DU-003/007/013/014/017`.
- `REQ-USR-003..004` → `DES-DU-002/005/007/009` → `TASK-DU-002/006/014/017/020`.
- `REQ-USR-005..006` → `DES-DU-007/009/010` → `TASK-DU-012/014/015/017/020`.
- `REQ-USR-007` → `DES-DU-005/009` → `TASK-DU-006/012/013/020`.
- `REQ-DU-NF-003/006` → `DES-DU-004/011` → `TASK-DU-004/008/019/020/022`.
- `REQ-DU-SEC-*`, `REQ-DU-NF-005` → `DES-DU-012` → `TASK-DU-016..018/021`.

## Salida esperada

Departamentos y usuarios disponen de administración segura y paginada; cuenta Identity y perfil Domain permanecen atómicos; la baja lógica revoca sesiones según contrato; y migraciones, restricciones, concurrencia, consultas, HTTP y OpenAPI se prueban igual en PostgreSQL y SQL Server.