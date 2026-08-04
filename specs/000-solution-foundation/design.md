# 000 — Solution Foundation: Design

**Estado:** Completed  
**Requisitos:** `requirements.md`  
**Fecha:** 4 de agosto de 2026

## 1. Resumen

La fase 0 crea el esqueleto compilable de Internal Operations Dashboard sin implementar negocio. La solución usa .NET 10, proyectos separados por Clean Architecture, configuración central, pruebas de dependencias y un workflow de CI mínimo. El diseño prioriza límites ejecutables y evita instalar paquetes reservados para fases posteriores.

## 2. Contexto verificado

Estado del workspace al redactar esta spec:

| Elemento | Estado observado |
|---|---|
| SDK .NET | `10.0.302` (`osx-arm64`) |
| Runtime ASP.NET Core | `10.0.10` |
| Docker / Compose | `29.6.2` / `5.3.1` |
| Git | `2.50.1` |
| `global.json` | Ausente |
| Repositorio Git | No inicializado |
| Archivos de producto | Solo la especificación maestra |

Docker no participa en los checks de esta fase.

## 3. Decisiones de diseño

### DES-FND-001 Toolchain reproducible

Se añadirá `global.json` con SDK `10.0.300`, `rollForward: latestPatch` y `allowPrerelease: false`. Esto permite usar `10.0.302` y futuros parches de la misma feature band sin saltar silenciosamente a otra feature band o major.

`Directory.Build.props` definirá como mínimo:

- `TargetFramework` = `net10.0` para proyectos administrados centralmente;
- `LangVersion` = `14.0`;
- `Nullable` = `enable`;
- `ImplicitUsings` = `enable`;
- analizadores .NET habilitados con nivel `latest` compatible;
- compilación determinista;
- warnings como errores cuando `ContinuousIntegrationBuild=true`.

Los proyectos podrán sobrescribir una propiedad únicamente si existe una necesidad documentada. Las versiones NuGet vivirán en `Directory.Packages.props`; no habrá versiones dispersas en `.csproj`.

**Cubre:** REQ-FND-001, REQ-FND-004, REQ-FND-NF-001..003.

### DES-FND-002 Topología de solución y referencias

La estructura se ubicará en la raíz del repositorio:

```text
src/
├── InternalOperations.Api/
├── InternalOperations.Application/
├── InternalOperations.Domain/
├── InternalOperations.Infrastructure/
├── InternalOperations.Persistence/
└── InternalOperations.Shared/
tests/
├── InternalOperations.Domain.UnitTests/
├── InternalOperations.Application.UnitTests/
├── InternalOperations.Persistence.IntegrationTests/
├── InternalOperations.Api.IntegrationTests/
└── InternalOperations.ArchitectureTests/
```

Tipos de proyecto:

| Proyecto | Tipo | Referencias productivas permitidas en fase 0 |
|---|---|---|
| Domain | Class library | ninguna; `Shared` solo si una primitiva estable lo exige |
| Shared | Class library | ninguna |
| Application | Class library | Domain, Shared |
| Infrastructure | Class library | Application, Shared |
| Persistence | Class library | Application, Domain, Shared |
| Api | ASP.NET Core Web | Application, Infrastructure, Persistence, Shared |

Los proyectos de test referencian únicamente los proyectos bajo prueba y utilidades de test estrictamente necesarias. No se añadirán referencias circulares ni referencias de producto hacia tests.

La aplicación API contendrá solo el host mínimo necesario para compilar y permitir futuras pruebas; no se expondrán endpoints de negocio en esta fase.

**Cubre:** REQ-FND-002, REQ-FND-003.

### DES-FND-003 Estrategia de pruebas fundacionales

xUnit será el framework común. El proyecto `ArchitectureTests` verificará dos niveles:

1. **Grafo de proyectos:** inspección de referencias declaradas en los assemblies/proyectos para detectar direcciones prohibidas.
2. **Dependencias de tipos/namespaces:** reglas declarativas para confirmar que Domain y Application no dependen de frameworks o adaptadores prohibidos.

Catálogo inicial de tests:

| ID | Verificación |
|---|---|
| TEST-FND-001 | `global.json` selecciona SDK 10.0.3xx instalado |
| TEST-FND-002 | todos los proyectos productivos apuntan a `net10.0` y C# 14 |
| TEST-FND-003 | la solución restaura y compila |
| TEST-FND-004 | las cinco suites de test son descubiertas |
| TEST-FND-005 | Domain no depende de capas externas |
| TEST-FND-006 | Application no depende de adaptadores ni API |
| TEST-FND-007 | Infrastructure solo apunta hacia Application/Shared |
| TEST-FND-008 | Persistence solo apunta hacia Application/Domain/Shared |
| TEST-FND-009 | ningún proyecto productivo depende de Api |
| TEST-FND-010 | las versiones de paquetes están centralizadas y no aparecen en los `.csproj` |
| TEST-FND-011 | restore determinista completa sin paquetes prerelease |
| TEST-FND-012 | nullable, C# 14 y analizadores están habilitados |
| TEST-FND-013 | format check termina sin cambios requeridos |
| TEST-FND-014 | un warning controlado falla bajo configuración CI y luego se revierte |
| TEST-FND-015 | Domain no depende de ASP.NET Core, EF Core, MediatR o AutoMapper |
| TEST-FND-016 | el workflow tiene sintaxis, triggers, permisos, timeout y concurrency esperados |
| TEST-FND-017 | la secuencia local equivalente a CI termina en verde |
| TEST-FND-018 | CI usa `global.json`, format check y warnings-as-errors |
| TEST-FND-019 | los cinco ADRs existen y contienen las secciones obligatorias |
| TEST-FND-020 | los comandos del README funcionan en orden desde un estado limpio controlado |
| TEST-FND-021 | Git ignora outputs/secretos locales y solo muestra cambios intencionales |

Las pruebas de integración existirán como proyectos configurados, pero no levantarán bases de datos ni contendrán falsos tests de persistencia antes de la fase correspondiente.

**Cubre:** REQ-FND-003, REQ-FND-006, REQ-FND-007.

### DES-FND-004 Calidad, paquetes e higiene

`.editorconfig` será la autoridad de estilo. `dotnet format --verify-no-changes --no-restore` será el check de formato. Se habilitarán analizadores integrados y solo se añadirá un paquete externo de análisis si aporta reglas no cubiertas y queda registrado en la evidencia de tarea.

Archivos base:

```text
.config/dotnet-tools.json
.editorconfig
.gitignore
Directory.Build.props
Directory.Packages.props
global.json
InternalOperations.slnx
```

`packages.lock.json` se generará por proyecto cuando el restore bloqueado resulte estable con Central Package Management. CI usará `--locked-mode` después de que dichos locks hayan sido generados y versionados. No se fingirá restore bloqueado antes de disponer de locks.

`TASK-FND-002` seleccionará las últimas versiones estables compatibles con .NET 10 disponibles al ejecutarse y registrará las versiones elegidas en la evidencia. No se aceptarán paquetes prerelease.

**Cubre:** REQ-FND-004, REQ-FND-005, REQ-FND-011.

### DES-FND-005 Pipeline de CI

`.github/workflows/backend-ci.yml` se ejecutará en pull requests y pushes a la rama principal. Tendrá permisos mínimos de lectura, timeout explícito y concurrency por ref.

Orden del job fundacional:

1. checkout;
2. setup del SDK desde `global.json`;
3. `dotnet tool restore`;
4. `dotnet restore --locked-mode` cuando existan locks;
5. `dotnet format --verify-no-changes --no-restore`;
6. `dotnet build --configuration Release --no-restore -p:ContinuousIntegrationBuild=true`;
7. `dotnet test --configuration Release --no-build` con resultados TRX.

No se añaden servicios PostgreSQL/SQL Server en fase 0. La ampliación de matriz ocurre en la spec de persistencia.

Validaciones asociadas: TEST-FND-016..018.

**Cubre:** REQ-FND-005, REQ-FND-008.

### DES-FND-006 ADRs iniciales

Se crearán:

- `0001-use-dotnet-10-lts.md`;
- `0002-clean-architecture-boundaries.md`;
- `0003-mediator-service-responsibilities.md`;
- `0004-dual-database-provider-strategy.md`;
- `0005-result-and-problem-details.md`.

Aunque las implementaciones de MediatR, persistencia y errores pertenecen a fases posteriores, sus fronteras se documentan ahora porque condicionan las referencias de proyectos. Cada ADR usará: título, estado, fecha, contexto, decisión, consecuencias y alternativas.

TEST-FND-019 verificará presencia, secciones obligatorias y ausencia de contradicciones evidentes con el baseline.

**Cubre:** REQ-FND-009.

### DES-FND-007 Documentación de onboarding

El `README.md` describirá:

- propósito y alcance backend-only;
- prerrequisitos exactos de la fase 0;
- comandos de restore, format, build y test;
- estructura de solución y dirección de dependencias;
- enlace a la especificación maestra y a `specs/000-solution-foundation/`;
- aclaración de que bases de datos y Docker todavía no son necesarios.

TEST-FND-020 será una ejecución manual reproducible de los comandos desde un checkout limpio o un directorio temporal controlado. TEST-FND-021 revisará que el estado Git solo incluya archivos intencionales y que outputs generados estén ignorados.

**Cubre:** REQ-FND-010, REQ-FND-011.

## 4. Flujo de dependencias

```text
Api ───────────────> Application ─────────> Domain
 │                         │
 ├──> Infrastructure ──────┘
 └──> Persistence ─────────> Domain
             └─────────────> Application

Shared: solo primitivas técnicas estables; no contiene negocio ni depende de otras capas.
```

Las referencias de API hacia adaptadores existen únicamente para composition root. Los tipos de adaptadores no deben filtrarse a contratos de Application.

## 5. Manejo de errores en fase 0

No se implementa el Result Pattern ni ProblemDetails. Los fallos de toolchain, restore, compilación o pruebas usan códigos de salida estándar del CLI. La semántica de errores de aplicación se define en ADR `0005` y se implementará después de aprobar su spec correspondiente.

## 6. Seguridad

- GitHub Actions tendrá `contents: read` como permiso por defecto.
- No se usarán secrets en CI durante esta fase.
- `.gitignore` excluirá `.env` y archivos locales sensibles sin excluir `.env.example`.
- No habrá fuentes NuGet adicionales sin decisión documentada.
- Los proyectos vacíos no contendrán credenciales, connection strings ni datos personales.

## 7. Datos, migraciones y portabilidad

No hay modelo de datos ni migraciones en fase 0. La portabilidad se protege evitando que Domain/Application conozcan proveedores y reservando EF Core para Persistence. ADR `0004` documentará las migraciones separadas y las pruebas contractuales futuras.

## 8. Observabilidad

No se instrumenta OpenTelemetry en fase 0. CI debe producir logs de restore/build/test y artefactos TRX suficientes para diagnóstico, sin imprimir variables sensibles.

## 9. Estrategia de implementación

La implementación será secuencial porque cada paso establece entradas para el siguiente:

1. inicializar control de versiones e higiene;
2. crear solución/proyectos/referencias;
3. fijar toolchain, paquetes y calidad;
4. crear ADRs;
5. implementar pruebas arquitectónicas;
6. configurar CI;
7. documentar y verificar desde estado limpio.

No se iniciará ningún paso hasta aprobar los tres artefactos de esta spec.

## 10. Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Tests de arquitectura pasan porque aún no existen tipos | combinar inspección del grafo de proyectos con reglas de tipos y añadir fixtures mínimos solo si son necesarios |
| `global.json` demasiado estricto bloquea parches | fijar feature band y usar `latestPatch` |
| Warnings-as-errors dificulta desarrollo local | hacerlo obligatorio en CI; mantener feedback local configurable sin ocultar warnings |
| Lock files y CPM producen restore frágil | validar localmente antes de activar `--locked-mode` |
| Proyectos vacíos acumulan paquetes futuros | instalar solo dependencias necesarias para compilar y probar fase 0 |
| API mínima se interpreta como contrato funcional | no añadir rutas de negocio y documentar explícitamente su condición de placeholder técnico |

## 11. Matriz de trazabilidad

| Requisito | Diseño | Tareas | Tests/Evidencia |
|---|---|---|---|
| REQ-FND-001 | DES-FND-001 | TASK-FND-002 | TEST-FND-001..002 |
| REQ-FND-002 | DES-FND-002 | TASK-FND-001 | TEST-FND-003..004 |
| REQ-FND-003 | DES-FND-002..003 | TASK-FND-001, 004 | TEST-FND-005..009 |
| REQ-FND-004 | DES-FND-001, 004 | TASK-FND-002 | TEST-FND-010..011 |
| REQ-FND-005 | DES-FND-004..005 | TASK-FND-002, 005 | TEST-FND-012..014, 018 |
| REQ-FND-006 | DES-FND-003 | TASK-FND-004 | TEST-FND-005..009, 015 |
| REQ-FND-007 | DES-FND-003 | TASK-FND-001, 004 | TEST-FND-003..009 |
| REQ-FND-008 | DES-FND-005 | TASK-FND-005 | TEST-FND-016..018 |
| REQ-FND-009 | DES-FND-006 | TASK-FND-003 | TEST-FND-019 |
| REQ-FND-010 | DES-FND-007 | TASK-FND-006 | TEST-FND-020 |
| REQ-FND-011 | DES-FND-004, 007 | TASK-FND-000, 007 | TEST-FND-021 |
| REQ-FND-NF-001 | DES-FND-001, 003..005, 007 | TASK-FND-002, 004..007 | TEST-FND-001..021 |
| REQ-FND-NF-002 | DES-FND-001, 005, 007 | TASK-FND-002, 005..007 | TEST-FND-017, 020 |
| REQ-FND-NF-003 | DES-FND-004..005 | TASK-FND-000, 002, 005, 007 | TEST-FND-010..011, 016, 021 |
| REQ-FND-NF-004 | DES-FND-002..004, 006 | TASK-FND-001..004, 007 | TEST-FND-005..015, 019 |

## 12. Revisión de diseño

- La dirección de dependencias coincide con el baseline maestro.
- Ninguna decisión introduce API pública, datos o proveedor de persistencia.
- Los tests cubren tanto referencias de proyecto como dependencias de tipos.
- La CI de fase 0 es deliberadamente menor que la matriz final.
- El orden de tareas respeta dependencias y contiene un gate de aprobación previo.
- Los riesgos relevantes tienen mitigación verificable.
