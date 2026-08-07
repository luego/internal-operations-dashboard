# 015 — Logical Deletion: Tasks

**Estado:** Implementing
**Requisitos:** `requirements.md`
**Diseño:** `design.md`

- [x] **GATE-DEL-000 Aprobar alcance**
  - Aprobado explícitamente por el usuario el 7 de agosto de 2026.

- [ ] **TASK-DEL-001 Implementar contrato de dominio con TDD**
  - Agregar `IsDeleted`, `Delete()` y `Restore()` a `BaseEntity`.
  - Verificar estado inicial e idempotencia.

- [ ] **TASK-DEL-002 Integrar Persistence e Identity**
  - Agregar filtro global y conversión defensiva de deletes.
  - Convertir métodos remove del repositorio genérico.
  - Rechazar cuentas Identity eliminadas.

- [ ] **TASK-DEL-003 Generar migraciones dual-provider**
  - Crear `AddLogicalDeletion` en ambos assemblies.
  - Confirmar snapshots sincronizados y scripts válidos.

- [ ] **TASK-DEL-004 Verificar contratos**
  - Probar visibilidad normal, `IgnoreQueryFilters`, persistencia física y autenticación.
  - Ampliar provider matrix para PostgreSQL y SQL Server.
  - Ejecutar format, build estricto y suite completa.

- [ ] **TASK-DEL-005 Sincronizar documentación**
  - Actualizar README, spec 020 y evidencia.
  - Marcar la spec `Completed` solo con checks verdes.
