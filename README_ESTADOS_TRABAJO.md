# Estados de Trabajo por vertical

## Comunes a todos los verticales

- **Pendiente**: recién contratado, esperando aceptación del estudiante.
- **Aceptado**: estudiante confirmó. Aún no arrancó el trabajo.
- **Cancelado**: alguna de las partes canceló antes de completar. Puede requerir reembolso.
- **Disputa**: conflicto no resuelto. Requiere intervención de admin.

## Por vertical

### ProyectoCerrado

- **EnCurso**: estudiante trabajando activamente.
- **Entregado**: estudiante subió entregables y notificó. Cliente debe revisar.
- **Completado**: cliente aprobó. Pago liberado al estudiante.

### Clase

- **EnCurso**: al menos una sesión realizada. Lo dispara el estudiante al marcar la primera.
- **Entregado**: sin uso en esta vertical (ver abajo).
- **Completado**: todas las sesiones fueron realizadas. Lo dispara la última sesión marcada, que libera la última fracción del pago.

Nota: en paquetes, cada sesión libera una fracción del pago (Hito 2 Parte 3). El pago queda en `ParcialmenteLiberado` hasta que se marque la última.

### Salud

- **EnCurso**: consentimiento firmado, práctica en ejecución.
- **Entregado**: sin uso en esta vertical (ver abajo).
- **Completado**: práctica realizada. Una práctica es una sesión, así que marcarla completa el trabajo y libera el pago.

**Importante**: Salud NO puede pasar de `Aceptado` a `EnCurso` sin consentimiento firmado. Esto vale también para el avance automático al marcar la sesión: si el consentimiento falta, la marca falla y no se libera nada.

## State machine

```
Pendiente → Aceptado, Cancelado
Aceptado → EnCurso, Cancelado
EnCurso → Entregado, Disputa, Cancelado
Entregado → Completado, Disputa
Disputa → Completado, Cancelado
Completado → (final)
Cancelado → (final)
```

Cualquier transición no listada por la state machine se rechaza con HTTP 400.

## Permisos por transición

| Transición | Actor permitido |
|---|---|
| aceptar | estudiante |
| iniciar | estudiante |
| entregar | estudiante |
| completar | cliente |
| cancelar | cliente o estudiante |
| disputar | cliente o estudiante |

## Efecto en Pago y en la agenda por cada transición

| Transición | Efecto en Pago | Efecto en Turnos/Sesiones |
|---|---|---|
| contratar ProyectoCerrado | Crea Pago(Retenido) + MovimientoPago(Retencion) | Sin efecto (PC no tiene turnos) |
| contratar Clase/Salud | Crea Pago(Retenido) + MovimientoPago(Retencion) | Crea N Turnos(Confirmados) + N Sesiones(Pendientes), en la misma transacción |
| aceptar | Sin efecto | Sin efecto |
| iniciar | Sin efecto | Sin efecto |
| entregar | Sin efecto | Sin efecto |
| completar | Crea 2 MovimientoPago (LiberacionEstudiante + ComisionLex) → Pago(Liberado) | Sin efecto |
| cancelar | Si el escrow seguía sin resolverse (Retenido o EnDisputa) → MovimientoPago(Reembolso) → Pago(Reembolsado). Si el trabajo no tenía pago, la cancelación procede igual. | Sin efecto sobre los turnos ya agendados |
| disputar | Pago pasa a EnDisputa (sin movimiento contable, dinero congelado) | Sin efecto |
| cancelar turno | Sin efecto inmediato en el pago | Turno(Cancelado) + Sesión(Cancelada) + decrementa CantidadSesionesTotales. Si llega a 0, se cancela el trabajo entero por esta misma tabla (fila `cancelar`), con su reembolso. Si las sesiones que quedan ya se dieron, el trabajo se completa y libera el remanente. |
| marcar sesión como Realizada o NoAsistio | Crea 2 MovimientoPago (LiberacionEstudiante + ComisionLex) por fracción. Última sesión → Pago(Liberado). Antes → Pago(ParcialmenteLiberado) | Sesión pasa a Realizada o NoAsistio. Turno pasa a Realizado o Ausente. Trabajo.SesionesCompletadas +1. Primera sesión → Trabajo.Estado = EnCurso. Última sesión → Trabajo.Estado = Completado. |

`cancelar turno` y `marcar sesión` no son transiciones del trabajo: son operaciones de la agenda que pueden *provocar* una. Viven en `POST /api/turnos/{id}/cancelar` y `POST /api/sesiones/{id}/realizar`, no en la máquina de estados.

## Completado: manual en ProyectoCerrado, automático en Clase y Salud

Desde Hito 2 Parte 3, `POST /api/trabajos/{id}/completar` **está bloqueado para Clase y Salud** (400): esos trabajos se completan solos al marcar la última sesión, que es también la que libera la última fracción del pago. Completar por afuera dejaría el escrow y las sesiones diciendo cosas distintas. La excepción es cerrar una Disputa, que es la vía de resolución del conflicto.

Esto deja **`Entregado` sin uso en Clase y Salud**. La descripción de arriba ("Entregado: todas las sesiones del paquete fueron realizadas") describe un momento que ahora **es** el Completado: con liberación fraccionada, la plata ya se fue liberando sesión a sesión y no hay una confirmación posterior del cliente que esperar. `Entregado` sigue vivo y es obligatorio en ProyectoCerrado.

`completar` y `cancelar` aceptan un pago en Retenido o en EnDisputa, porque ambas transiciones son alcanzables desde Disputa.

Ver `README_PAGOS.md` para detalles del modelo de pagos y `README_TURNOS.md` para el sistema de turnos y sesiones.
