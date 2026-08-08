# 050 — Operations Dashboard: Design

**Estado:** Implementing
**Fecha:** 8 de agosto de 2026

## Corte vertical

- Application define DTOs, `GetDashboardSummaryQuery`, `GetDashboardTrendsQuery`, validator y `IDashboardQueryService`.
- Persistence implementa agregaciones EF Core sobre `Tickets`, `Departments`, `DomainUsers` y `TicketComments`.
- La tendencia agrupa en base de datos y rellena días faltantes en memoria usando `IClock`.
- API publica `DashboardController` con `Dashboard.Read`.

## Contratos

- `DashboardSummaryDto`: timestamp, total, conteos por estado, sin asignar, prioridad alta/crítica activa, departamentos y usuarios activos.
- `DashboardTrendsDto`: rango UTC y puntos diarios con tickets creados y comentarios añadidos.

## Decisiones

- No se añade tabla de métricas ni migración: el volumen de showcase permite agregación directa.
- No se presenta una métrica de resolución inexacta; el modelo histórico actual no guarda el estado destino como columna analítica.
- Fechas se calculan en UTC y cada día se representa con `DateOnly` en el contrato.
- Los filtros globales de baja lógica son la fuente de verdad para excluir registros eliminados.

## Verificación

- TDD de validator/handlers, agregaciones y contrato HTTP.
- Provider contract con datos conocidos.
- Regresión completa, build estricto, format y matriz alojada.
