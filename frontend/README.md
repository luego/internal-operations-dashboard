# OpsDesk Frontend

Next.js App Router frontend and lightweight BFF for the Internal Operations Dashboard.

## Responsibilities

- responsive operations UI;
- server-side session validation;
- HttpOnly access and refresh cookies;
- authenticated proxy to the .NET API;
- one refresh rotation and retry after an API `401`;
- no duplicated backend authorization or business rules.

## Local development

Run the .NET API on `http://localhost:8080`, then configure the BFF through process environment variables:

```bash
export API_BASE_URL=http://localhost:8080
export AUTH_JWT_ISSUER=internal-operations-api
export AUTH_JWT_AUDIENCE=internal-operations-web
export AUTH_JWT_SIGNING_KEY='<same-development-signing-key-as-the-api>'
export AUTH_COOKIE_SECURE=false

npm ci
npm run dev
```

Open <http://localhost:3000>.

For the simplest integrated setup, use the root script instead:

```bash
./scripts/start-showcase.sh
```

## Quality gates

```bash
npm test
npm run typecheck
npm run lint
npm run build
npm audit --audit-level=high
```

## Security notes

- Tokens are never returned to browser JavaScript after login.
- Cookies are HttpOnly and SameSite-restricted.
- Login, logout and state-changing proxy requests require an exact same-origin `Origin` or `Referer` header.
- Concurrent refresh rotations are deduplicated so a single-use token is never replayed by parallel dashboard requests.
- `AUTH_COOKIE_SECURE=false` exists only for local HTTP Docker testing. Use secure cookies behind HTTPS in deployed environments.
- `API_BASE_URL` is server-only and points to the private API service in Docker.
