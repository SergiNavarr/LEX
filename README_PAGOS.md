# Modelo de Pagos en LEX

## Estructura

- **Pago**: contrato de pago de un Trabajo. Relación 1-a-1 con Trabajo (índice UNIQUE en `pago.trabajo_id`). Guarda los montos snapshoteados al momento de contratar.
- **MovimientoPago**: cada operación contable individual. Relación N-a-1 con Pago. El monto es siempre positivo; el signo lo deriva el tipo de asiento.

El `Pago` dice *cuánto* y *en qué estado está*; el libro de `MovimientoPago` dice *qué pasó y cuándo*. Los asientos son inmutables: una corrección se hace con un asiento nuevo, no editando uno viejo.

## Ciclo de vida

1. **Contratación**: se crea `Pago(Retenido)` + `MovimientoPago(Retencion, monto_total)`.
2. **Completado**: se crean `MovimientoPago(LiberacionEstudiante, monto_a_estudiante)` + `MovimientoPago(ComisionLex, monto_comision)` → `Pago(Liberado)`.
3. **Cancelado**: si el escrow seguía sin resolverse, se crea `MovimientoPago(Reembolso, monto_total)` → `Pago(Reembolsado)`.
4. **Disputa**: `Pago(EnDisputa)`. Sin movimiento contable: la plata no se mueve, solo queda congelada hasta la resolución.

En Clase y Salud el paso 2 no ocurre de una vez: cada sesión libera su fracción (ver abajo).

Liberar acepta un pago en `Retenido`, `EnDisputa` o `ParcialmenteLiberado`. `EnDisputa` entra porque la máquina de estados habilita `Disputa → Completado`: resolver una disputa tiene que poder cerrar el escrow.

Reembolsar solo acepta `Retenido` o `EnDisputa`: devuelve el **total** al cliente, así que exige que no se le haya pagado nada al estudiante todavía. Un `ParcialmenteLiberado` no se puede reembolsar por esta vía.

### Liberación total = liberación del remanente

`LiberarPagoTotalAsync` no libera `MontoAEstudiante` a ciegas: calcula lo que falta según el libro de movimientos y libera eso. En un trabajo sin sesiones el remanente es el total y el comportamiento es el de siempre; en uno que venía liberando por sesión, cierra solo lo que quedaba.

### Atomicidad

Los métodos de negocio de `PagoService` no llaman a `SaveChanges` ni abren transacción propia: solo dejan los cambios en el `DbContext`. El llamador (contratación o máquina de estados) cierra la unidad de trabajo con un único `SaveChanges`, así el trabajo y su pago se commitean en la misma transacción implícita, o no se commitea ninguno.

## Comisión LEX

- Configurable en `appsettings.json` como `Lex:PorcentajeComision` (default 10).
- Se snapshotea al momento de contratar en `pago.porcentaje_comision_lex`: un cambio futuro del take rate no reescribe los pagos ya firmados.
- Se calcula sobre `MontoTotal` y se descuenta del monto que se libera al estudiante.

Ejemplo con trabajo de $10.000 y comisión 10%:

- Cliente paga: $10.000
- Estudiante recibe: $9.000
- LEX se queda con: $1.000

## EstadoPago.ParcialmenteLiberado (activo desde Hito 2 Parte 3)

Se activa cuando un trabajo tiene múltiples sesiones y algunas ya se liberaron pero no todas.

Ejemplo: paquete de 4 sesiones a $10.000. Después de marcar 2 sesiones como Realizada:

- `MovimientoPago(Retencion, $10.000)` — al contratar
- `MovimientoPago(LiberacionEstudiante, $2.250)` + `MovimientoPago(ComisionLex, $250)` — sesión 1
- `MovimientoPago(LiberacionEstudiante, $2.250)` + `MovimientoPago(ComisionLex, $250)` — sesión 2
- Estado del Pago: **ParcialmenteLiberado**

Cuando se marca la última sesión, se ajusta por redondeo y pasa a `Liberado`.

### El ajuste de la última sesión

Las fracciones se redondean a 2 decimales, así que sumarlas no siempre da el monto contratado. La última sesión **no divide: paga el remanente**, calculado desde el libro de movimientos.

Ejemplo real (paquete de 7 a $1.000, comisión 10% → $900 al estudiante, $100 a LEX):

| | Fracciones 1-6 | Última | Suma |
|---|---|---|---|
| Al estudiante | 6 × $128,57 | **$128,58** | $900,00 ✅ |
| Comisión LEX | 6 × $14,29 | **$14,26** | $100,00 ✅ |

Sin el ajuste, dividir daría $899,99 al estudiante (falta 1 centavo) y $100,03 de comisión (sobran 3). El libro tiene que cerrar exacto contra el monto contratado.

El divisor es `CantidadSesionesTotales` **vigente**, no el original: si se cancelan sesiones del paquete, las restantes valen proporcionalmente más (ver `README_TURNOS.md`).

## Estado actual

- Contabilidad interna funcional.
- **NO integrado con Mercado Pago** aún. La contabilidad refleja la intención de pago, no un cobro real.
- Liberación **total** al completar, en ProyectoCerrado (no tiene sesiones).
- Liberación **fraccionada por sesión** en Clase y Salud, disparada por el estudiante al marcar cada sesión.

## Reservado para futuro

- `MovimientoPago.ReferenciaExterna`: para guardar el `payment_id` de Mercado Pago (fase futura).
- `TipoMovimientoPago.Ajuste`: para correcciones manuales del admin (interfaz admin pendiente). Es lo que haría falta para revertir fracciones ya liberadas al resolver una disputa en contra del estudiante: hoy `Disputa → Cancelado` reembolsaría de más si ya se liberó algo, por eso reembolsar rechaza los pagos `ParcialmenteLiberado`.
- `MovimientoPago.TrabajoHistorialId`: traza opcional del asiento a la transición de estado que lo originó.

## Endpoints

| Endpoint | Acceso | Devuelve |
|---|---|---|
| `GET /api/pagos/mios` | usuario logueado | Pagos donde participa (como cliente o estudiante), del más nuevo al más viejo. Filtros opcionales: `?estado=` y `?tipo_trabajo=`. |
| `GET /api/pagos/{id}` | partes del trabajo | Detalle del pago con su libro de movimientos. |
| `GET /api/pagos/{id}/movimientos` | partes del trabajo | Solo el libro, en orden cronológico. |
| `GET /api/admin/ingresos` | rol Admin | Panel de ingresos con breakdown por vertical. |

Las sesiones que liberan el pago viven en `POST /api/sesiones/{id}/realizar` y `/no-asistio` (ver `README_TURNOS.md`).

Un pago al que el usuario no participa responde **404**, igual que uno inexistente: distinguirlos con un 403 filtraría que el pago existe.

Ver `README_ESTADOS_TRABAJO.md` para el efecto de cada transición sobre el pago.
