# 100 — Frontend MVP: Design

**Estado:** Approved
**Fecha:** 8 de agosto de 2026

## Arquitectura

```text
Browser -> Next.js :3000 -> ASP.NET Core :8080 -> PostgreSQL
```

- Next.js App Router, TypeScript y `output: standalone`.
- Next Route Handlers forman un BFF delgado: login/refresh/logout y proxy autenticado hacia `/api/v1`.
- Tokens solo en cookies HttpOnly; ninguna credencial en `localStorage` o `sessionStorage`.
- TanStack Query para server state interactivo; React Hook Form + Zod para formularios.
- Tipos generados desde `/openapi/v1.json` y cliente fetch pequeño.
- Diseño oscuro inspirado en Linear: `#08090a`, superficies translúcidas, bordes blancos sutiles, acento índigo `#5e6ad2`, Inter y foco visible.
- Next no contiene autorización de negocio; oculta acciones por rol y maneja 403, pero la API decide.

## Rutas

```text
/login
/dashboard
/tickets
/tickets/new
/tickets/[ticketId]
/departments
/users
/forbidden
```

## Cookies BFF

- `ops_access`: HttpOnly, SameSite=Lax, Secure en producción, expira con access token.
- `ops_refresh`: HttpOnly, SameSite=Strict, Secure en producción, restringida a handlers BFF.
- Login y refresh nunca retornan tokens al JavaScript del navegador.
- Las rotaciones concurrentes del mismo refresh token se deduplican mientras están pendientes y durante una ventana breve después de completarse; la llamada al backend tiene timeout.
- Login, logout y mutaciones del proxy exigen `Origin` o `Referer` con coincidencia exacta para impedir CSRF desde orígenes hermanos.

## Docker de desarrollo/showcase

- `scripts/start-showcase.sh` solicita de forma oculta PostgreSQL y administrator passwords.
- Genera una JWT signing key local si no fue suministrada.
- Guarda la configuración únicamente en `.env.showcase`, ignorado por Git y con modo `600`, para que `stop`/`start` funcionen sin volver a pedir credenciales.
- Publica PostgreSQL, API y frontend exclusivamente en `127.0.0.1`; las cookies pueden usar HTTP no seguro solo en este entorno local.
- PostgreSQL usa volumen nombrado y health check.
- Un servicio de migración aplica el historial antes de iniciar API.
- API Development ejecuta seed opt-in con el administrador ingresado.
- Next espera readiness de API.

## Testing

- Vitest + Testing Library para componentes y handlers aislables.
- MSW para contratos HTTP de UI.
- Playwright contra el stack PostgreSQL real para login/dashboard/ticket.
- Backend conserva su suite .NET y provider matrix.

## Exclusiones

SSR avanzado, Server Actions como capa de negocio, Auth.js, base de datos en Next, internacionalización, offline, realtime y CORS permisivo.
