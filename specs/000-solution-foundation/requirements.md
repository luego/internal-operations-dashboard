# 000 — Solution Foundation: Requirements

**Estado:** Completed  
**Baseline:** Internal Operations Dashboard Backend Spec v1.0  
**Fecha:** 4 de agosto de 2026  
**Alcance:** Fundación de la solución; no incluye features de negocio

## 1. Objetivo

Establecer una solución .NET 10 compilable, reproducible y preparada para crecer mediante Clean Architecture, con límites de dependencias comprobables, administración central de configuración y paquetes, pruebas unitarias y de arquitectura, CI mínima y decisiones arquitectónicas versionadas.

## 2. Alcance

Esta spec incluye:

- inicialización del repositorio Git y estructura base;
- fijación del SDK .NET 10 y configuración común de compilación;
- creación de la solución y proyectos productivos y de pruebas;
- referencias entre proyectos compatibles con la arquitectura acordada;
- administración central de versiones NuGet y herramientas locales;
- reglas iniciales de formato, análisis estático y compilación;
- pruebas que protejan los límites arquitectónicos;
- CI mínima para restore, format, build y pruebas fundacionales;
- specs base y ADRs `0001` a `0005`;
- documentación mínima para ejecutar los checks de la fase 0.

Esta spec no incluye:

- entidades o casos de uso de negocio;
- API REST, controllers, OpenAPI o ProblemDetails funcionales;
- MediatR, AutoMapper, FluentValidation o el Result Pattern implementados;
- EF Core, proveedores, migraciones o contenedores de base de datos;
- Identity, autenticación o autorización;
- Dockerfile, Docker Compose, observabilidad o health checks;
- frontend.

## 3. Actores

- **Developer:** clona el repositorio, restaura, compila y ejecuta pruebas.
- **CI runner:** verifica de forma no interactiva las reglas fundacionales.
- **Architect/reviewer:** revisa dependencias, ADRs y trazabilidad antes de aprobar la fase.

## 4. Requisitos funcionales

### REQ-FND-001 Fijar el toolchain

**Historia:** Como developer, quiero que la versión esperada de .NET sea explícita para obtener builds reproducibles.

#### Criterios de aceptación

1. WHEN se ejecuta un comando `dotnet` desde el repositorio con un SDK compatible instalado, THE SYSTEM SHALL seleccionar .NET SDK 10 con roll-forward limitado a parches compatibles.
2. WHEN solo existe un SDK incompatible, THE SYSTEM SHALL fallar con un mensaje del CLI sin seleccionar silenciosamente una versión mayor.
3. THE SYSTEM SHALL usar `net10.0`, C# 14, nullable reference types e implicit usings en los proyectos productivos.
4. THE SYSTEM SHALL declarar en un único lugar las opciones comunes de compilación aplicables a la solución.

**Trazabilidad:** DES-FND-001; TASK-FND-002; TEST-FND-001..002

### REQ-FND-002 Crear la estructura de solución

**Historia:** Como developer, quiero una estructura coherente de proyectos para ubicar cada responsabilidad en su capa correcta.

#### Criterios de aceptación

1. WHEN se inspecciona la solución, THE SYSTEM SHALL contener los seis proyectos productivos y los cinco proyectos de pruebas definidos en el baseline maestro.
2. THE SYSTEM SHALL usar `InternalOperations.slnx` como archivo de solución.
3. THE SYSTEM SHALL mantener el código productivo bajo `src/` y las pruebas bajo `tests/`.
4. WHEN se ejecuta `dotnet build`, THE SYSTEM SHALL compilar todos los proyectos sin código de features de fases posteriores.

**Trazabilidad:** DES-FND-002; TASK-FND-001; TEST-FND-003..004

### REQ-FND-003 Aplicar la dirección de dependencias

**Historia:** Como architect, quiero dependencias dirigidas hacia el núcleo para impedir acoplamientos accidentales.

#### Criterios de aceptación

1. THE SYSTEM SHALL permitir las referencias `Application -> Domain`, `Infrastructure -> Application`, `Persistence -> Application + Domain`, `Api -> Application + Infrastructure + Persistence` y referencias limitadas hacia `Shared`.
2. THE SYSTEM SHALL impedir que `Domain` referencie `Application`, `Infrastructure`, `Persistence` o `Api`.
3. THE SYSTEM SHALL impedir que `Application` referencie `Infrastructure`, `Persistence` o `Api`.
4. THE SYSTEM SHALL impedir que `Api` sea referenciada por otro proyecto productivo.
5. WHEN una referencia prohibida es introducida, THEN al menos una prueba de arquitectura SHALL fallar con una causa identificable.

**Trazabilidad:** DES-FND-002..003; TASK-FND-001, TASK-FND-004; TEST-FND-005..009

### REQ-FND-004 Centralizar paquetes y herramientas

**Historia:** Como maintainer, quiero versiones centralizadas para evitar divergencias entre proyectos.

#### Criterios de aceptación

1. THE SYSTEM SHALL administrar las versiones NuGet mediante `Directory.Packages.props`.
2. THE SYSTEM SHALL registrar las herramientas de repositorio mediante `.config/dotnet-tools.json` cuando una herramienta CLI sea necesaria.
3. WHEN se restaura la solución desde un clone limpio, THE SYSTEM SHALL resolver versiones deterministas sin referencias prerelease.
4. THE SYSTEM SHALL mantener el número inicial de dependencias externas al mínimo necesario para la fase 0.

**Trazabilidad:** DES-FND-001, DES-FND-004; TASK-FND-002; TEST-FND-010..011

### REQ-FND-005 Aplicar calidad estática y formato

**Historia:** Como maintainer, quiero reglas uniformes para detectar defectos y reducir diferencias de estilo.

#### Criterios de aceptación

1. THE SYSTEM SHALL versionar `.editorconfig` con convenciones para C# y archivos de texto.
2. WHEN CI ejecuta el build, THE SYSTEM SHALL tratar warnings como errores.
3. WHEN CI verifica el formato, THE SYSTEM SHALL fallar si existen cambios requeridos por el formateador.
4. THE SYSTEM SHALL habilitar analizadores .NET y un nivel de análisis compatible con .NET 10.
5. WHEN una regla se suprima, THE SYSTEM SHALL exigir una justificación localizada o documentada; no se permiten supresiones globales silenciosas.

**Trazabilidad:** DES-FND-004; TASK-FND-002, TASK-FND-005; TEST-FND-012..014

### REQ-FND-006 Proteger la arquitectura con pruebas

**Historia:** Como architect, quiero pruebas ejecutables de los límites para detectar regresiones automáticamente.

#### Criterios de aceptación

1. THE SYSTEM SHALL comprobar por prueba las restricciones de `REQ-FND-003`.
2. THE SYSTEM SHALL incluir una prueba que confirme que Domain no depende de ASP.NET Core, Entity Framework Core, MediatR ni AutoMapper.
3. WHEN se ejecutan las pruebas de arquitectura dos veces sin cambios, THE SYSTEM SHALL producir el mismo resultado.
4. THE SYSTEM SHALL nombrar las pruebas de forma que la regla violada sea reconocible en el reporte.

**Trazabilidad:** DES-FND-003; TASK-FND-004; TEST-FND-005..009, TEST-FND-015

### REQ-FND-007 Proporcionar pruebas unitarias base

**Historia:** Como developer, quiero proyectos de pruebas configurados para añadir escenarios sin rehacer la infraestructura de test.

#### Criterios de aceptación

1. THE SYSTEM SHALL usar un único framework de pruebas para las suites .NET del baseline.
2. WHEN se ejecuta `dotnet test`, THE SYSTEM SHALL descubrir y ejecutar las pruebas fundacionales.
3. THE SYSTEM SHALL permitir cobertura de Domain y Application sin imponer todavía un umbral sobre proyectos vacíos.
4. THE SYSTEM SHALL evitar tests ignorados o dependientes de red, reloj real, orden o estado compartido.

**Trazabilidad:** DES-FND-003; TASK-FND-001, TASK-FND-004; TEST-FND-003..009

### REQ-FND-008 Automatizar CI mínima

**Historia:** Como reviewer, quiero que cada cambio sea validado automáticamente antes de integrarse.

#### Criterios de aceptación

1. WHEN se abre o actualiza un pull request, THE SYSTEM SHALL restaurar herramientas y paquetes, verificar formato, compilar y ejecutar pruebas unitarias y de arquitectura.
2. WHEN se actualiza la rama principal, THE SYSTEM SHALL ejecutar los mismos checks fundacionales.
3. THE SYSTEM SHALL usar el SDK definido por `global.json` y restauración bloqueada cuando exista el lock file acordado.
4. WHEN cualquier check falla, THEN el job SHALL finalizar con estado fallido y conservar logs suficientes para localizar la etapa.
5. THE SYSTEM SHALL cancelar ejecuciones obsoletas del mismo pull request cuando sea seguro hacerlo.

**Trazabilidad:** DES-FND-005; TASK-FND-005; TEST-FND-016..018

### REQ-FND-009 Versionar decisiones arquitectónicas

**Historia:** Como reviewer, quiero conocer el contexto y consecuencias de las decisiones transversales.

#### Criterios de aceptación

1. THE SYSTEM SHALL incluir ADRs para .NET 10, límites de Clean Architecture, responsabilidades mediator/service, estrategia dual de base de datos y Result/ProblemDetails.
2. EACH ADR SHALL indicar estado, contexto, decisión, consecuencias y alternativas relevantes.
3. WHEN una decisión aceptada cambie, THE SYSTEM SHALL superseder el ADR anterior sin reescribir silenciosamente su historia.
4. THE SYSTEM SHALL mantener consistencia entre ADRs, esta spec y el baseline maestro.

**Trazabilidad:** DES-FND-006; TASK-FND-003; TEST-FND-019

### REQ-FND-010 Documentar ejecución local

**Historia:** Como developer nuevo, quiero instrucciones mínimas para validar la fundación desde un clone limpio.

#### Criterios de aceptación

1. THE SYSTEM SHALL documentar prerrequisitos, restore, build, format check y test en `README.md`.
2. THE SYSTEM SHALL documentar que Docker y las bases de datos no son necesarios para ejecutar la fase 0.
3. WHEN una persona sigue los comandos documentados en orden, THE SYSTEM SHALL poder completar los checks sin pasos implícitos ni secretos.
4. THE SYSTEM SHALL identificar la spec activa y enlazar sus artefactos.

**Trazabilidad:** DES-FND-007; TASK-FND-006; TEST-FND-020

### REQ-FND-011 Mantener higiene del repositorio

**Historia:** Como maintainer, quiero excluir artefactos generados y secretos para conservar un repositorio seguro y limpio.

#### Criterios de aceptación

1. THE SYSTEM SHALL ignorar outputs de .NET, IDE, cobertura, sistema operativo y archivos `.env` reales.
2. THE SYSTEM SHALL permitir versionar `.env.example` sin valores secretos cuando se introduzca en una fase posterior.
3. THE SYSTEM SHALL usar finales de línea y codificación consistentes.
4. WHEN finaliza la implementación de la fase 0, THEN `git status` SHALL mostrar únicamente cambios intencionados.

**Trazabilidad:** DES-FND-004; TASK-FND-007; TEST-FND-021

## 5. Requisitos no funcionales

### REQ-FND-NF-001 Reproducibilidad

El restore, build y test deben poder ejecutarse desde un clone limpio usando únicamente el SDK fijado y acceso al feed NuGet público configurado. No deben depender de estado global de IDE ni de archivos no versionados.

### REQ-FND-NF-002 Portabilidad del entorno

Los scripts y comandos documentados deben funcionar en runners Linux y ser ejecutables en el entorno macOS de desarrollo. No se introducirán scripts exclusivos de un shell cuando exista un comando `dotnet` equivalente.

### REQ-FND-NF-003 Seguridad de la cadena de suministro

No se permiten paquetes prerelease, fuentes NuGet no confiables ni secretos. Las versiones se centralizan y los paquetes transitivos vulnerables se tratarán según severidad y disponibilidad de corrección.

### REQ-FND-NF-004 Mantenibilidad

La fundación no debe incluir abstracciones sin consumidor, placeholders de negocio ni dependencias reservadas para fases futuras. Las excepciones necesitan trazabilidad a un requisito o ADR.

## 6. Supuestos adoptados

Los siguientes detalles son reversibles y se adoptan como propuesta de diseño:

- el contenido de la solución vivirá directamente en la raíz actual del repositorio, sin una carpeta contenedora adicional;
- xUnit será el framework de pruebas único;
- las pruebas arquitectónicas usarán una librería declarativa compatible con .NET 10 y comprobaciones de referencias de proyecto;
- GitHub Actions será el proveedor inicial de CI, según la ruta prescrita por el baseline;
- `InternalOperations.Api` será un proyecto Web API sin controllers o endpoints de negocio en esta fase; los demás proyectos productivos serán class libraries;
- se fijará la feature band `10.0.3xx` observada localmente, con `rollForward: latestPatch`.

## 7. Decisiones aprobadas

No se identificaron ambigüedades que impidan implementar la fase 0. El 4 de agosto de 2026 se aprobaron conjuntamente:

1. el uso de xUnit como framework de pruebas;
2. GitHub Actions como CI inicial;
3. ubicar la solución directamente en la raíz del repositorio;
4. inicializar Git durante `TASK-FND-000`;
5. fijar la feature band del SDK 10.0.3xx y permitir únicamente parches compatibles.

Estas decisiones no alteran el contrato público ni el modelo de datos, pero forman parte de la fundación que debe permanecer estable.

## 8. Revisión de requisitos

- Todos los requisitos tienen resultado verificable y trazabilidad hacia diseño, tareas y tests previstos.
- La spec no anticipa implementación de features ni persistencia.
- Seguridad, contrato HTTP y datos no cambian en esta fase.
- La ejecución dual PostgreSQL/SQL Server comienza en specs posteriores; aquí solo se preservan los límites que la harán posible.
- No hay preguntas abiertas del baseline maestro que bloqueen esta spec.

## 9. Definition of Ready

La spec estará lista para implementación cuando:

- `requirements.md`, `design.md` y `tasks.md` sean aprobados;
- las cinco decisiones de la sección 7 sean aceptadas o sustituidas explícitamente;
- requisitos, diseño, tareas y tests tengan trazabilidad consistente;
- no queden contradicciones con la especificación maestra.
