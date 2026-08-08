# 050 — Operations Dashboard: Requirements

**Estado:** Completed
**Fecha:** 8 de agosto de 2026
**Aprobación:** Fast-track autorizado para completar el showcase.

## Objetivo

Exponer métricas operativas simples, útiles y demostrables sin introducir un motor analítico ni almacenamiento duplicado.

## Requisitos

1. `GET /api/v1/dashboard/summary` devuelve una fotografía de entidades activas:
   - total de tickets;
   - tickets por estado;
   - tickets sin asignar;
   - tickets activos de prioridad alta o crítica;
   - departamentos activos;
   - usuarios activos.
2. `GET /api/v1/dashboard/trends?days=N` devuelve una serie diaria para tickets creados y comentarios añadidos.
3. `days` admite de 1 a 90 días y usa 30 por defecto.
4. La serie incluye días sin actividad con valor cero y orden ascendente.
5. Las métricas excluyen entidades eliminadas lógicamente mediante los filtros normales.
6. Ambos endpoints requieren `Dashboard.Read`.
7. Las consultas son read-only, provider-agnostic y no requieren tablas ni migraciones nuevas.

## Fuera de alcance

- BI, exportación, caché distribuida o actualización en tiempo real.
- SLA, tiempos promedio de resolución o métricas históricas que el modelo actual no pueda demostrar con precisión.
- Segmentación arbitraria o constructor de reportes.

## Criterios de aceptación

- Los conteos coinciden con la persistencia activa.
- Un rango inválido produce validación estable.
- PostgreSQL y SQL Server ejecutan el mismo contrato.
- Build, pruebas, formato y provider matrix quedan verdes antes de cerrar la spec.
