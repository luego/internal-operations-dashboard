# 100 — Frontend MVP: Requirements

**Estado:** Approved
**Fecha:** 8 de agosto de 2026
**Aprobación:** Fast-track autorizado para el showcase Fiverr.

## Objetivo

Entregar una interfaz Next.js responsive que consuma la API .NET terminada y permita demostrar autenticación, dashboard, tickets, colaboración y administración sin duplicar reglas de negocio.

## Requisitos

- **REQ-FE-001 — Arranque sencillo:** el repositorio debe ofrecer un flujo Docker Compose que levante frontend, API y PostgreSQL. Las contraseñas se solicitan interactivamente y nunca se versionan ni vienen preconfiguradas; una configuración local ignorada y con permisos restringidos permite reanudar el stack sin volver a introducirlas.
- **REQ-FE-002 — Sesión segura:** Next.js actúa como BFF; access y refresh tokens se guardan en cookies HttpOnly, se rotan mediante el backend y se eliminan al cerrar o invalidar la sesión.
- **REQ-FE-003 — Login:** el usuario puede iniciar y cerrar sesión con mensajes seguros para 400, 401, 415 y 429.
- **REQ-FE-004 — Autorización visual:** la navegación y las acciones reflejan `Administrator`, `Manager`, `Agent` y `Viewer`; la API .NET continúa siendo la autoridad final.
- **REQ-FE-005 — Dashboard:** summary y trends se muestran con métricas, gráfica, loading, empty y error states.
- **REQ-FE-006 — Tickets:** listar, buscar, filtrar, crear, ver, actualizar, asignar y cambiar estado usando paginación y concurrencia del backend.
- **REQ-FE-007 — Colaboración:** detalle de ticket permite comentarios paginados e historial inmutable.
- **REQ-FE-008 — Administración:** Administrator puede gestionar departamentos, usuarios, estado, departamento y roles.
- **REQ-FE-009 — Contrato:** los tipos se generan desde OpenAPI; ProblemDetails se conserva y se transforma en errores de UI seguros.
- **REQ-FE-010 — Calidad:** TypeScript estricto, lint sin warnings, pruebas unitarias/componentes y smoke E2E del flujo principal.
- **REQ-FE-011 — Responsive y accesible:** escritorio y móvil, navegación por teclado, labels, foco visible y contraste suficiente.
- **REQ-FE-012 — Showcase proporcional:** evitar Redux, CMS, micro-frontends, websockets, SSR complejo y reglas de negocio duplicadas.

## Criterio de cierre

Frontend lint/test/build y backend regression verdes; arranque full-stack verificado con Docker cuando exista daemon; documentación reproducible y sin secretos versionados.
