# 015 — Logical Deletion: Design

**Estado:** Approved
**Requisitos:** `requirements.md`

## Diseño mínimo

- `BaseEntity` incorpora `bool IsDeleted` y métodos `Delete()`/`Restore()` idempotentes.
- `IdentityAccount` incorpora `bool IsDeleted`.
- `ApplicationDbContext` configura filtros globales `!IsDeleted` para entidades de negocio y cuenta Identity.
- `SaveChanges`/`SaveChangesAsync` convierten entradas `EntityState.Deleted` de `BaseEntity` o `IdentityAccount` a `Modified` y marcan `IsDeleted`.
- `GenericRepository.Remove` y `RemoveRange` llaman `Delete()`; no usan `DbSet.Remove`.
- La base de datos conserva índices únicos existentes sin filtros, por lo que los valores dados de baja continúan reservados.
- Las relaciones de negocio no introducen cascadas físicas nuevas.

## Consultas

```text
consulta normal -> query filter -> solo IsDeleted = false
consulta administrativa explícita -> IgnoreQueryFilters() -> todos los registros
```

No se crea un repositorio de papelera. Cada feature decidirá si ofrece una operación pública de baja; la infraestructura impide el borrado físico aunque una operación use accidentalmente `Remove`.

## Migraciones

Cada assembly recibe una migración `AddLogicalDeletion` que agrega `IsDeleted NOT NULL DEFAULT false/0` a:

- `Departments`;
- `Users`;
- `Tickets`;
- `TicketComments`;
- `IdentityUsers`.

## Pruebas

- unidad de `BaseEntity`: estado inicial, delete y restore idempotentes;
- integración EF: query filter y conversión de `Deleted`;
- autenticación: cuenta eliminada rechazada;
- provider matrix: migración y persistencia física en ambos motores.

## Decisiones de simplicidad

- No se agregan `DeletedAtUtc`, `DeletedBy`, papelera ni purge job para este showcase.
- No se aplican filtros a sesiones refresh ni tablas técnicas de Identity.
- `IsActive` conserva semántica distinta y no se reemplaza por `IsDeleted`.
