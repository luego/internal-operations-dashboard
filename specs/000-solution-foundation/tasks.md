# 000 — Solution Foundation: Tasks

**Estado:** Completed  
**Fecha:** 4 de agosto de 2026

## Convenciones

- No ejecutar ninguna tarea de implementación mientras esta spec esté en estado `Proposed`.
- Una tarea solo cambia a `[x]` después de ejecutar sus checks y registrar evidencia breve.
- Los IDs `TEST-FND-*` corresponden al catálogo definido en `design.md`.
- Si una tarea descubre un conflicto que cambia arquitectura, seguridad, contrato o datos, se actualiza la spec y se solicita aprobación antes de continuar.
- Los detalles reversibles pueden resolverse con la opción más simple, documentando la decisión.

## Gate 0 — Aprobación de la spec

- [x] **GATE-FND-000 Aprobar requirements, design y tasks**
  - Aprobar o modificar las cinco decisiones de `requirements.md` §7.
  - Confirmar que no hay requisitos de fases posteriores incluidos.
  - Cambiar los tres artefactos de `Proposed` a `Approved`.
  - **Bloquea:** todas las tareas siguientes.
  - **Evidencia:** aprobación explícita del usuario recibida el 4 de agosto de 2026; estados de los tres artefactos sincronizados como `Approved`.

## Ola 1 — Repositorio y solución

- [x] **TASK-FND-000 Inicializar control de versiones e higiene mínima**
  - Inicializar Git en la raíz aprobada.
  - Crear `.gitignore` y `.gitattributes` sin eliminar archivos existentes.
  - Excluir `bin/`, `obj/`, resultados de tests, cobertura, IDE, `.DS_Store` y `.env`; permitir `.env.example`.
  - Preservar la especificación maestra como baseline versionado.
  - **Requisitos:** REQ-FND-011.
  - **Depende de:** GATE-FND-000.
  - **Verificación:** TEST-FND-021; inspección de `git status --short`.
  - **Evidencia:** `git init -b main` completado; `git check-ignore -v .DS_Store` confirma la regla; `git status --short --ignored` solo muestra archivos fuente intencionales y `.DS_Store` ignorado.

- [x] **TASK-FND-001 Crear solución, proyectos y referencias permitidas**
  - Crear `InternalOperations.slnx`.
  - Crear seis proyectos productivos y cinco proyectos de pruebas en las rutas de DES-FND-002.
  - Configurar referencias exactamente según la matriz aprobada.
  - Mantener API sin endpoints de negocio.
  - Añadir todos los proyectos a la solución y comprobar que no hay ciclos.
  - **Requisitos:** REQ-FND-002, REQ-FND-003, REQ-FND-007.
  - **Depende de:** TASK-FND-000.
  - **Verificación:** TEST-FND-003..009; `dotnet sln InternalOperations.slnx list`; `dotnet build`.
  - **Evidencia:** `InternalOperations.slnx` enumera 11 proyectos; referencias creadas según DES-FND-002; build Release CI completó con 0 warnings y 0 errores; API no expone endpoints.

## Ola 2 — Toolchain, calidad y decisiones

- [x] **TASK-FND-002 Fijar SDK, compilación, paquetes y formato**
  - Crear `global.json` conforme a DES-FND-001.
  - Crear `Directory.Build.props`, `Directory.Packages.props` y `.editorconfig`.
  - Inicializar `.config/dotnet-tools.json` y añadir solo herramientas realmente necesarias.
  - Centralizar versiones estables y evaluar/generar lock files.
  - Habilitar nullable, C# 14, analizadores, build determinista y warnings-as-errors en CI.
  - Registrar las versiones estables seleccionadas.
  - **Requisitos:** REQ-FND-001, REQ-FND-004, REQ-FND-005.
  - **Depende de:** TASK-FND-001.
  - **Verificación:** TEST-FND-001..002, TEST-FND-010..014; restore, format check y build Release.
  - **Evidencia:** SDK `10.0.302` seleccionado por `global.json`; CPM y lock files generados; paquetes estables: Test SDK 18.8.1, xUnit 2.9.3, runner 3.1.5, Coverlet 10.0.1, NetArchTest 1.3.2; format check reportó 0/51 archivos; build negativo TEST-FND-014 falló con CS1030 y el cambio controlado fue retirado.

- [x] **TASK-FND-003 Crear ADRs fundacionales**
  - Crear `docs/adr/0001` a `0005` según DES-FND-006.
  - Distinguir decisiones ya aceptadas por el baseline de detalles aplazados a feature specs.
  - Enlazar requisitos y specs relacionados cuando aplique.
  - **Requisitos:** REQ-FND-009.
  - **Depende de:** GATE-FND-000.
  - **Puede ejecutarse en paralelo con:** TASK-FND-001..002, sin editar los mismos archivos.
  - **Verificación:** TEST-FND-019; revisión contra secciones 2, 7, 8 y 22 del baseline maestro.
  - **Evidencia:** `docs/adr/0001` a `0005` creados con estado, fecha, contexto, decisión, consecuencias y alternativas; revisión cruzada realizada contra el baseline maestro.

## Ola 3 — Guardrails ejecutables

- [x] **TASK-FND-004 Implementar pruebas de arquitectura**
  - Implementar reglas de referencias y namespaces de DES-FND-003.
  - Añadir pruebas smoke deterministas a los proyectos unitarios si son necesarias para confirmar discovery.
  - Evitar tests vacíos, ignorados o que requieran infraestructura externa.
  - Confirmar que una violación controlada hace fallar la regla correspondiente y revertirla después.
  - **Requisitos:** REQ-FND-003, REQ-FND-006, REQ-FND-007.
  - **Depende de:** TASK-FND-001, TASK-FND-002.
  - **Verificación:** TEST-FND-004..009, TEST-FND-015; `dotnet test` repetido dos veces.
  - **Evidencia:** 8 pruebas arquitectónicas en verde dos veces; cubren grafo exacto y dependencias Domain/Application. Una referencia temporal `Domain -> Shared` produjo el fallo esperado y fue retirada; corrida final en verde.

- [x] **TASK-FND-005 Configurar CI fundacional**
  - Crear `.github/workflows/backend-ci.yml` según DES-FND-005.
  - Configurar permisos mínimos, timeout y cancelación de ejecuciones obsoletas.
  - Ejecutar restore, format check, build Release y tests con TRX.
  - No añadir servicios de bases de datos en esta fase.
  - **Requisitos:** REQ-FND-005, REQ-FND-008.
  - **Depende de:** TASK-FND-002, TASK-FND-004.
  - **Verificación:** TEST-FND-016..018; ejecutar localmente la secuencia equivalente.
  - **Evidencia:** workflow YAML parseado correctamente; contiene permisos `contents: read`, concurrency, timeout, SDK por `global.json`, cache por locks, restore bloqueado, format, build estricto, test TRX y upload. Secuencia local equivalente completada en verde.

## Ola 4 — Documentación y cierre

- [x] **TASK-FND-006 Crear README de onboarding para fase 0**
  - Documentar propósito, prerrequisitos, estructura, dirección de dependencias y comandos exactos.
  - Enlazar la especificación maestra y los tres artefactos de esta spec.
  - Aclarar que Docker y bases de datos comienzan en fases posteriores.
  - **Requisitos:** REQ-FND-010.
  - **Depende de:** TASK-FND-002, TASK-FND-005.
  - **Verificación:** TEST-FND-020; ejecutar instrucciones en orden desde entorno limpio controlado.
  - **Evidencia:** README creado con alcance, prerrequisitos, estructura, dependencias, specs y comandos; los cinco comandos se ejecutaron en orden desde una copia temporal limpia con build 0/0 y 12/12 tests.

- [x] **TASK-FND-007 Verificar y sincronizar el incremento**
  - Ejecutar tool restore, restore bloqueado si aplica, format check, build Release y todas las pruebas.
  - Revisar solución, referencias, ADRs, workflow, README y estado Git.
  - Actualizar cada checkbox y su evidencia sin ocultar desviaciones.
  - Registrar riesgos residuales y la siguiente spec desbloqueada, sin implementarla.
  - Cambiar el estado de esta spec a `Completed` solo si todos los criterios pasan.
  - **Requisitos:** REQ-FND-001..011 y REQ-FND-NF-001..004.
  - **Depende de:** TASK-FND-000..006.
  - **Verificación:** TEST-FND-001..021; `dotnet format`, `dotnet build`, `dotnet test`, `git status`.
  - **Evidencia:** tool restore, locked restore, format, build Release CI y tests completados; build 0 warnings/0 errores; 12/12 tests en dos corridas y en copia limpia; 11 lock files; 0 versiones NuGet en `.csproj`; estado Git contiene solo fuentes intencionales y outputs ignorados.

## Checklist de salida de fase 0

- [x] La solución compila sin warnings no aprobados.
- [x] Todas las pruebas fundacionales pasan dos veces de forma determinista.
- [x] Una violación de dependencia controlada fue detectada por los tests.
- [x] El workflow representa fielmente los checks locales.
- [x] Los cinco ADRs están presentes y son coherentes.
- [x] README funciona desde un checkout limpio controlado.
- [x] No hay secretos, outputs generados ni cambios accidentales.
- [x] Requirements, design, tasks y evidencias están sincronizados.
- [x] No se implementó ninguna feature de negocio ni fase posterior.

## Siguiente trabajo desbloqueado

La fase fundacional está cerrada. Se puede redactar y someter a aprobación la spec de cross-cutting y persistencia correspondiente a la fase 1. Su implementación no forma parte de `000-solution-foundation`.

## Riesgos residuales

- El workflow fue validado sintácticamente y mediante su secuencia local equivalente, pero no podrá observarse una ejecución real de GitHub Actions hasta publicar el repositorio.
- La especificación maestra enumera `010-identity-and-access` en la estructura de specs, mientras que sus fases sitúan cross-cutting y persistencia antes de identidad. La próxima spec debe resolver explícitamente su identificador y límite antes de implementar fase 1.
