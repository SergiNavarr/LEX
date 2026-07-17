# Sistema de Turnos y Sesiones en LEX

## Alcance

Sistema que permite:

1. Estudiantes de servicios de Clase y Salud configuran su disponibilidad semanal.
2. Clientes consultan la disponibilidad y agendan turnos al contratar servicios.
3. Servicios con paquete (ej: 4 clases de matemática) generan múltiples sesiones, una por turno.
4. El estudiante marca cada sesión como realizada, liberando fraccionadamente el pago.

## Modelo

### DisponibilidadEstudiante

Bloques semanales que ofrece el estudiante. Es global (aplica a todos sus servicios de Clase/Salud), no por servicio: la duración de cada turno la define el servicio que se contrata, no el bloque.

Ejemplo: "Lunes 14:00-18:00", "Miércoles 09:00-12:00".

La baja es lógica (`activo = false`). Desactivar un bloque **no toca los turnos ya reservados** en ese horario: solo impide reservar nuevos.

`DiaSemana` se persiste como string, así que su valor numérico nunca llega a la DB. Ese valor sí ordena la semana en memoria: los listados de bloques **ordenan en C#, no en SQL**, porque un `ORDER BY` sobre la columna text daría orden alfabético ("Jueves" antes que "Martes") en lugar de cronológico.

### Turno

Instancia concreta reservada. Fecha + hora + duración + estado. Una vez creado vive por su cuenta: no cuelga del bloque de disponibilidad que lo originó.

La duración se copia al turno en lugar de leerse del servicio, para que el turno sobreviva a que el estudiante edite la duración de sus sesiones después de agendar.

Estados: Reservado, Confirmado, Realizado, Cancelado, Ausente.

### Sesion

Vive dentro de un trabajo. 1-a-1 con Turno (índice UNIQUE en `sesion.turno_id`). Un trabajo puede tener N sesiones (una por turno): 1 en Salud y en Clase suelta, N en un paquete de Clase.

Estados: Pendiente, Realizada, Cancelada, NoAsistio.

### Índices de turno

`(estudiante_id, fecha_hora_inicio)` y `(cliente_id, fecha_hora_inicio)` son índices **no únicos**, solo para performance de queries.

Un UNIQUE sobre `(estudiante_id, fecha_hora_inicio)` sería una falsa red de seguridad: detecta colisiones exactas pero no solapamientos con duración. Un turno de 14:00 (60 min) y otro de 14:30 (30 min) no violan el UNIQUE y sin embargo chocan en la realidad.

La validación real de conflictos va en el service, en la Parte 2, comparando intervalos (`t.FechaHoraInicio < nuevoFin && nuevoInicio < t.FechaHoraInicio + duracion`) y dentro de una transacción para cerrar la race condition entre dos clientes reservando el mismo hueco. En la Parte 1 el schema solo queda preparado.

### Reglas de borrado

| Relación | OnDelete | Motivo |
|---|---|---|
| `disponibilidad_estudiante` → `perfil_estudiante` | Cascade | Los bloques son una preferencia del estudiante, sin valor contractual. |
| `sesion` → `trabajo` | Cascade | La sesión es parte del trabajo. |
| `sesion` → `turno` | Restrict | No se borra un turno con sesión viva: primero se cancela la sesión. |
| `turno` → `perfil_estudiante` / `perfil_cliente` | Restrict | El turno es evidencia de un compromiso entre las partes. |

## Zona horaria

**Todos los `DateTime` se guardan en UTC** (`timestamptz`), sin excepción. Aplica a `turno.fecha_hora_inicio`, `sesion.fecha_realizada` y a las fechas del resto del modelo.

**El prototipo asume que todos los usuarios están en Argentina (UTC-3).** No hay horario de verano: el país no lo aplica desde 2009. La regla vive en un solo lugar, `Common/HorarioArgentina.cs`, con offset fijo en lugar de `TimeZoneInfo`, porque la base de datos de zonas horarias no está disponible de forma uniforme en el contenedor Linux del deploy.

**El frontend convierte de UTC a hora local AR usando el offset fijo.** La API entrega y recibe:

| Dato | Formato | Ejemplo |
|---|---|---|
| `turno.fecha_hora_inicio` en respuestas | UTC | `2026-07-22T17:00:00Z` = 14:00 ART |
| Bloques de disponibilidad | Hora local AR, sin fecha ni zona (`time without time zone`) | `"14:00:00"` = 14:00 en Corrientes |
| Parámetros `desde` / `hasta` | Fecha local AR, inclusive ambas | `desde=2026-07-22` |

### Por qué DateTime y no DateTimeOffset

`DateTimeOffset` daría la ilusión de soporte multi-timezone sin resolver el problema real, que es guardar la zona horaria **del usuario**, no la del momento en que se creó el registro. Con Argentina como única zona del prototipo, `DateTime` en UTC alcanza.

### Cuando se soporten múltiples zonas horarias

Es una decisión post-MVP. El día que LEX salga de UTC-3, el camino es:

1. Agregar `Usuario.ZonaHoraria` (IANA, ej. `America/Argentina/Cordoba`).
2. Ajustar las conversiones de `HorarioArgentina` para que tomen la zona del usuario en lugar del offset fijo.
3. Revisar `DisponibilidadEstudiante`: sus horas son locales del estudiante, así que un cliente en otra zona vería los slots corridos hasta que la conversión use ambas zonas.
4. Reconsiderar el offset fijo por `TimeZoneInfo` (requiere tzdata en la imagen de Docker), ya que otras zonas sí aplican DST.

Los `DateTime` en UTC ya guardados no necesitan migración: un instante UTC es correcto en cualquier zona.

## Reglas de negocio

- No se permiten bloques de disponibilidad superpuestos en el mismo día para el mismo estudiante. Los extremos que se tocan (14:00-18:00 seguido de 18:00-20:00) no cuentan como superposición.
- `HoraInicio < HoraFin` (se valida en el service, no hay CHECK en la DB).
- First-come-first-served para reservas.
- No hay anticipación mínima para reservar (prototipo): reservar para dentro de 10 minutos es válido. Lo único que no se puede reservar es un slot que ya empezó.
- Cancelar y volver a agendar (sin reagendar directo).
- Solo el estudiante marca sesión como Realizada.
- Sin plazo máximo entre sesiones del mismo paquete.

## Cálculo de slots disponibles

`GET /api/turnos/disponibles/estudiante/{id}` no lee una tabla de slots: los slots no existen como entidad. Se calculan al vuelo proyectando los bloques semanales sobre el rango pedido y restando los turnos ya tomados.

1. Se traen los bloques activos del estudiante (1 query).
2. Se traen sus turnos que ocupan la ventana (1 query), según `EstadosDeAgenda.Ocupan`.
3. Para cada día del rango y cada bloque de ese día de semana, se generan slots consecutivos de `duracion_minutos`, se descartan los que ya empezaron y los que solapan con un turno tomado.

El costo es de **2 queries fijas**: el número de días del rango no multiplica los viajes a la DB. El rango está topeado en **62 días** (cubre la vista de 2 meses de un calendario).

Un slot solo existe mientras nadie lo reserve: no se persiste ni se reserva temporalmente.

## Contratación con reserva de turnos (Parte 2)

Al contratar un servicio de Clase o Salud, el cliente debe elegir los slots concretos. Todo se hace en una única transacción atómica:

1. Se crea el Trabajo con snapshots del servicio.
2. Se crea el Pago(Retenido) + MovimientoPago(Retencion).
3. Se crean los N Turnos (Confirmados).
4. Se crean las N Sesiones (Pendientes).

Si algo falla, rollback total. Un trabajo sin sus turnos deja al cliente pagando por una agenda vacía, y unos turnos sin trabajo bloquean la agenda del estudiante sin que nadie los haya pagado.

Los turnos nacen **Confirmados**, sin paso intermedio por Reservado: el estudiante ya publicó esos horarios como disponibles, y esa publicación es la aceptación.

### Cuántos slots

| Servicio | Sesiones | Slots en el body |
|---|---|---|
| Clase con paquete de N | N | exactamente N |
| Clase suelta | 1 | exactamente 1 |
| Salud | 1 | exactamente 1 (`slotElegido`, no lista) |

No se aceptan paquetes a medio agendar: o se agenda el paquete completo, o no se contrata. La cantidad la fija el servicio; el cliente solo elige los horarios.

En Salud, reservar el turno **no reemplaza al consentimiento**: el trabajo sigue sin poder pasar de Aceptado a EnCurso hasta que el cliente lo firme.

### Validaciones al reservar

Cada slot elegido debe:
- Estar en el futuro (fecha > UtcNow).
- Caer dentro de un bloque de disponibilidad activo del estudiante.
- No exceder el fin del bloque considerando la duración de la sesión.
- No solapar con otros turnos existentes del estudiante (Reservado/Confirmado/Realizado).

Los slots del mismo paquete tampoco pueden solaparse entre sí: dos slots nuevos que chocan pasarían el chequeo individual, porque ninguno de los dos está todavía en la DB cuando se valida el otro.

Las reglas viven en `IValidadorTurnosService`, compartido por Clase y Salud. Devuelve el motivo del rechazo como string (o null si el slot sirve) y quien llama decide qué hacer con eso; hoy los dos lo traducen a un 400.

Qué estados ocupan la agenda es **una sola regla**, en `EstadosDeAgenda.Ocupan`, que comparten el cálculo de slots libres y la validación de reservas: si difirieran, el sistema ofrecería un horario que después rechaza. Ocupan **Reservado, Confirmado y Realizado**; Cancelado y Ausente devuelven el hueco.

`Realizado` ocupa aunque suene a pasado: una sesión se puede marcar como dada antes de la hora del turno, así que existen turnos Realizados con fecha futura.

### Cancelación de turnos

El estudiante o cliente puede cancelar un turno individual futuro. Efectos:
- Turno pasa a Cancelado.
- Sesión asociada pasa a Cancelada.
- CantidadSesionesTotales del trabajo se decrementa en 1.
- Si CantidadSesionesTotales llega a 0 → Trabajo pasa a Cancelado y se reembolsa el pago completo.

La cancelación del trabajo se delega en la máquina de estados (`ITrabajoService.CancelarAsync`), así queda registrada en el historial y el reembolso lo emite el mismo camino que cualquier otra cancelación.

**El pago NO se ajusta al cancelar sesiones individuales**. La distribución del pago se recalcula automáticamente en el momento de la liberación (Parte 3), dividiendo `MontoAEstudiante / CantidadSesionesTotales` con el valor vigente.

Sin regla de anticipación en el prototipo — cualquier turno futuro se puede cancelar, hasta un minuto antes. Lo único que no se puede es cancelar hacia atrás: un turno que ya ocurrió responde 400.

**Salud no tiene contador que decrementar**: `CantidadSesionesTotales` vive en `TrabajoClase`, no en la clase base. Una práctica es siempre una sesión, así que cancelarla es quedarse sin trabajo — el mismo resultado que el contador llegando a 0.

### Reagendar

No hay reagendar. Para cambiar la fecha de un turno, hay que cancelarlo y crear uno nuevo (esto último via contratación de un nuevo trabajo, no via endpoint aparte por ahora).

## Marcado de sesiones y liberación fraccionada (Parte 3)

Solo el estudiante puede marcar una sesión como Realizada o NoAsistio: marcar libera plata, y es él quien declara que dio la clase o la práctica.

### POST /api/sesiones/{id}/realizar

- Sesion pasa a Realizada.
- Turno asociado pasa a Realizado.
- Trabajo.SesionesCompletadas se incrementa.
- Se libera fracción del pago:
  - Monto al estudiante: MontoAEstudiante / CantidadSesionesTotales.
  - Comisión LEX: MontoComisionCalculada / CantidadSesionesTotales.
  - Redondeo a 2 decimales.
- Si es la última sesión:
  - Ajuste por redondeo para que la suma sea exacta.
  - Trabajo pasa a Completado.
  - Pago pasa a Liberado con FechaLiberacion.
- Si no es la última:
  - Pago queda en ParcialmenteLiberado.

Body opcional: `{ observaciones }` — notas del estudiante sobre la sesión.

Todo ocurre en una transacción: una sesión marcada sin su liberación (o al revés) dejaría al estudiante sin cobrar o cobrando dos veces.

### POST /api/sesiones/{id}/no-asistio

Mismo flujo que /realizar. Se libera el pago igual, porque el estudiante puso su tiempo. La sesión queda en NoAsistio y el turno en Ausente, así la ausencia queda registrada.

### Avance automático del trabajo

- **Primera sesión marcada**: si el trabajo está en Aceptado, pasa a EnCurso. Se delega en la máquina de estados, así que en Salud **iniciar sin consentimiento firmado falla y la sesión no se marca** (rollback completo).
- **Última sesión marcada**: el trabajo pasa a Completado y el pago a Liberado.

Un trabajo en Pendiente no admite marcar sesiones: el estudiante tiene que aceptarlo primero.

### Flujo de completado en Clase y Salud

Para trabajos de Clase y Salud, el estado Completado se alcanza automáticamente al marcar la última sesión como Realizada o NoAsistio. El endpoint tradicional `POST /api/trabajos/{id}/completar` está bloqueado para estos verticales — usa las sesiones. Se exceptúa el cierre de una Disputa, que es la vía de resolución del conflicto.

Para ProyectoCerrado, el completado sigue siendo manual (no tiene sesiones).

**Consecuencia**: el estado `Entregado` queda sin uso en Clase y Salud. Antes significaba "todas las sesiones realizadas"; ahora ese momento **es** el Completado, porque la plata ya se fue liberando sesión a sesión y no hay una confirmación posterior del cliente que esperar.

### Marcar antes de la hora del turno

No se valida que el turno ya haya ocurrido: el estudiante puede cerrar la sesión apenas termina, y el seed arma historia hacia atrás. Por eso existen turnos en estado Realizado con fecha futura, y por eso `Realizado` ocupa la agenda (ver `EstadosDeAgenda`).

### Cancelar las sesiones que faltaban

Si se cancelan los turnos pendientes de un paquete cuyas sesiones restantes ya se dieron (2 de 4 dadas, las otras 2 canceladas), el paquete pasa a ser de 2 y queda cumplido: el trabajo se completa y el pago se libera entero. Sin esto el pago quedaría clavado en ParcialmenteLiberado con plata sin liberar.

### Impugnación del cliente

En el prototipo, el cliente no puede impugnar una sesión marcada como Realizada. Si tiene un conflicto, debe iniciar una Disputa sobre el Trabajo (endpoint POST /api/trabajos/{id}/disputar), que congela el saldo pendiente hasta resolución por admin.

**Limitación conocida**: las fracciones ya liberadas no vuelven por esa vía. Revertirlas requiere asientos de tipo `Ajuste`, que esperan la interfaz de admin.

## Estado actual (Hito 2 completado)

- ✅ Modelo de datos completo.
- ✅ CRUD de disponibilidad.
- ✅ Consulta pública de disponibilidad.
- ✅ Consulta de turnos y sesiones.
- ✅ Contratación de Clase y Salud con reserva atómica de turnos.
- ✅ Cancelación de turno individual con ajuste de CantidadSesionesTotales.
- ✅ Marcado de sesiones (Realizada / NoAsistio) con liberación fraccionada del pago.

## Liberación fraccionada (Parte 3, pendiente)

Cada sesión marcada como Realizada liberará `MontoAEstudiante / CantidadSesionesTotales`, llevando el pago a `EstadoPago.ParcialmenteLiberado` (valor ya reservado en el enum desde Sub-hito 1.3) hasta que se completen todas.

Ver `README_PAGOS.md` para el modelo de pagos y `README_ESTADOS_TRABAJO.md` para el efecto de cada transición.

## Endpoints

### Disponibilidad

| Endpoint | Acceso | Devuelve |
|---|---|---|
| `GET /api/disponibilidad/mia` | rol Estudiante | Bloques activos del estudiante logueado. |
| `POST /api/disponibilidad` | rol Estudiante | Crea un bloque. 400 si la franja es inválida o pisa otra. |
| `PUT /api/disponibilidad/{id}` | dueño del bloque | Edita un bloque. 404 si no es tuyo. |
| `DELETE /api/disponibilidad/{id}` | dueño del bloque | Baja lógica. No afecta turnos ya reservados. |
| `GET /api/disponibilidad/estudiante/{id}` | público | Bloques activos de un estudiante. |

### Turnos

| Endpoint | Acceso | Devuelve |
|---|---|---|
| `GET /api/turnos/mios` | usuario logueado | Turnos donde participa (estudiante o cliente), los más próximos primero. Filtros: `?estado=`, `?desde=`, `?hasta=`. |
| `GET /api/turnos/{id}` | partes del turno | Detalle del turno con su sesión asociada, si tiene. |
| `GET /api/turnos/disponibles/estudiante/{id}` | público | Slots libres. Parámetros: `desde`, `hasta` (`YYYY-MM-DD`), `duracion_minutos` (default 60). |
| `POST /api/turnos/{id}/cancelar` | partes del turno | Cancela un turno futuro. Body opcional: `{ motivo }`. |
| `POST /api/turnos/{id}/confirmar` | — | Oculto en Swagger. Responde 501: los turnos ya nacen Confirmados. Reservado por si aparece un flujo de confirmación explícita. |

### Contratación (con reserva de turnos)

| Endpoint | Body |
|---|---|
| `POST /api/trabajos/clase` | `{ servicioId, slotsElegidos: [fechas UTC], notasCliente? }` |
| `POST /api/trabajos/salud` | `{ servicioId, pacienteId, slotElegido: fecha UTC, notasCliente? }` |

Ambos devuelven el trabajo con sus sesiones ya agendadas.

### Sesiones

| Endpoint | Acceso | Devuelve |
|---|---|---|
| `GET /api/trabajos/{trabajoId}/sesiones` | partes del trabajo | Sesiones del trabajo en orden de paquete (`numero_sesion`). |
| `POST /api/sesiones/{id}/realizar` | estudiante del trabajo | Marca la sesión cumplida y libera su fracción. Body opcional: `{ observaciones }`. |
| `POST /api/sesiones/{id}/no-asistio` | estudiante del trabajo | El cliente no vino. Libera igual. Body opcional: `{ observaciones }`. |

Un turno o trabajo en el que el usuario no participa responde **404**, igual que uno inexistente: distinguirlos con un 403 filtraría que existe. Misma convención que en `README_PAGOS.md`.
