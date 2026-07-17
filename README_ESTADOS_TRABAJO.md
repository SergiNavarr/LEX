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

- **EnCurso**: al menos una sesión programada o realizada. Pueden quedar sesiones pendientes.
- **Entregado**: todas las sesiones del paquete fueron realizadas.
- **Completado**: cliente confirmó satisfacción. Pago total liberado.

Nota: en paquetes, cada sesión libera una fracción del pago (Sub-hito 1.3). Este flujo NO cambia el estado global hasta que se completen todas.

### Salud

- **EnCurso**: consentimiento firmado, práctica en ejecución.
- **Entregado**: práctica realizada, historial actualizado.
- **Completado**: cliente confirmó. Pago liberado.

**Importante**: Salud NO puede pasar de `Aceptado` a `EnCurso` sin consentimiento firmado.

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
| cancelar turno | Sin efecto inmediato en el pago | Turno(Cancelado) + Sesión(Cancelada) + decrementa CantidadSesionesTotales. Si llega a 0, se cancela el trabajo entero por esta misma tabla (fila `cancelar`), con su reembolso. |

`cancelar turno` no es una transición del trabajo: es una operación de la agenda que puede *provocar* una. Vive en `POST /api/turnos/{id}/cancelar`, no en la máquina de estados.

`completar` y `cancelar` aceptan un pago en Retenido o en EnDisputa, porque ambas transiciones son alcanzables desde Disputa.

Ver `README_PAGOS.md` para detalles del modelo de pagos y `README_TURNOS.md` para el sistema de turnos y sesiones.
