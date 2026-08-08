# 100 — Frontend MVP: Tasks

**Estado:** Implementing
**Fecha:** 8 de agosto de 2026

- [x] **TASK-FE-001** Aprobar requisitos y diseño Next.js/BFF/Docker interactivo.
- [x] **TASK-FE-002** Scaffold Next.js estricto y baseline de lint/test/build.
- [x] **TASK-FE-003** Implementar shell responsive y design system mínimo.
- [x] **TASK-FE-004** Implementar BFF, cookies HttpOnly y cliente ProblemDetails.
- [x] **TASK-FE-005** Implementar login/logout y protección de rutas.
- [x] **TASK-FE-006** Implementar dashboard summary/trends.
- [ ] **TASK-FE-007** Implementar tickets list/create/detail/update/status.
- [ ] **TASK-FE-008** Implementar comentarios e historial.
- [ ] **TASK-FE-009** Implementar Departments y Users.
- [x] **TASK-FE-010** Añadir Docker full-stack con prompt manual y sin secretos versionados.
- [ ] **TASK-FE-011** Añadir tests Playwright y CI frontend.
- [ ] **TASK-FE-012** Completar documentación y gates finales.

## Evidencia requerida

```bash
cd frontend
npm ci
npm run lint
npm run test
npm run build

cd ..
dotnet build InternalOperations.slnx -c Release --no-restore -p:ContinuousIntegrationBuild=true
dotnet test InternalOperations.slnx -c Release --no-build --no-restore --filter "Category!=ProviderMatrix"
git diff --check
```

Docker se declara verificado únicamente tras ejecución real con un daemon disponible.
