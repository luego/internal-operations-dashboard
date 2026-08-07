# 015 — Logical Deletion

**Estado:** Approved
**Fecha:** 7 de agosto de 2026
**Aprobación:** El usuario autorizó el cambio transversal y continuar sin gates adicionales el 7 de agosto de 2026.
**Dependencias:** `../010-identity-and-access/`

## Objetivo

Evitar borrados físicos accidentales de entidades de negocio mediante una baja lógica sencilla y portable basada en `IsDeleted`.

## Alcance aprobado

### Incluye

- `IsDeleted` en todas las entidades de negocio que heredan de `BaseEntity`.
- `IsDeleted` en `IdentityAccount`, sincronizado con `Domain.User` cuando exista un caso de uso administrativo de baja.
- filtros globales EF Core para excluir registros eliminados de consultas normales;
- conversión defensiva de estados EF `Deleted` a actualizaciones lógicas;
- `GenericRepository.Remove` y `RemoveRange` como bajas lógicas;
- migraciones incrementales PostgreSQL y SQL Server;
- contratos que prueban persistencia física, invisibilidad normal y ausencia de cascadas destructivas.

### Excluye

- `RefreshTokenSession`, que usa expiración y revocación;
- tablas puente, claims, logins y tokens internos de ASP.NET Core Identity;
- historial/auditoría inmutable futuro;
- endpoints de restauración o papelera en este showcase;
- purga física programada.

## Requisitos

### REQ-DEL-001 Baja lógica común

1. WHEN una entidad de negocio se elimina mediante repositorio o EF Core, THE SYSTEM SHALL persistir `IsDeleted = true` en lugar de ejecutar `DELETE`.
2. Nuevas entidades SHALL iniciar con `IsDeleted = false`.
3. Consultas normales SHALL excluir entidades eliminadas.
4. Consultas administrativas o tests MAY usar `IgnoreQueryFilters()` explícitamente.

### REQ-DEL-002 Identidad

1. Una baja futura de usuario SHALL marcar `IdentityAccount` y `Domain.User` en la misma transacción, desactivar la cuenta y revocar sesiones activas.
2. Autenticación y refresh SHALL rechazar cuentas eliminadas con el mismo error público que una cuenta inválida.
3. El código de aplicación SHALL NOT usar `UserManager.DeleteAsync` para bajas normales.

### REQ-DEL-003 Integridad y unicidad

1. Relaciones entre entidades de negocio SHALL usar comportamiento restrictivo o no destructivo.
2. Los registros eliminados conservarán nombres, identificadores y números únicos reservados.
3. El cambio SHALL producir migraciones incrementales separadas para PostgreSQL y SQL Server.

### REQ-DEL-004 Contrato

1. La matriz relacional SHALL demostrar que el registro permanece físicamente después de eliminarlo.
2. Una consulta normal SHALL dejar de encontrarlo.
3. `IgnoreQueryFilters()` SHALL permitir verificarlo con `IsDeleted = true`.
4. El modelo SHALL comportarse igual en PostgreSQL y SQL Server.

## Salida esperada

El backend conserva los datos de negocio para demostración y auditoría sin añadir papelera, restauración, retención ni infraestructura innecesaria.
