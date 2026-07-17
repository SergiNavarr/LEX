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
2. Se traen sus turnos que ocupan la ventana (1 query). Ocupan los estados **Reservado** y **Confirmado**; Cancelado y Ausente devuelven el hueco a la agenda, y Realizado describe algo que ya pasó.
3. Para cada día del rango y cada bloque de ese día de semana, se generan slots consecutivos de `duracion_minutos`, se descartan los que ya empezaron y los que solapan con un turno tomado.

El costo es de **2 queries fijas**: el número de días del rango no multiplica los viajes a la DB. El rango está topeado en **62 días** (cubre la vista de 2 meses de un calendario).

Un slot solo existe mientras nadie lo reserve: no se persiste ni se reserva temporalmente.

## Estado actual (Hito 2 Parte 1)

- ✅ Modelo de datos completo.
- ✅ CRUD de disponibilidad.
- ✅ Consulta pública de disponibilidad.
- ✅ Consulta de turnos y sesiones.
- ❌ Contratación con turnos (Parte 2).
- ❌ Marcado de sesiones y liberación fraccionada (Parte 3).

La creación de turnos llega en la Parte 2, integrada con la contratación: hoy las tablas `turno` y `sesion` existen y se consultan, pero nada las puebla.

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

### Sesiones

| Endpoint | Acceso | Devuelve |
|---|---|---|
| `GET /api/trabajos/{trabajoId}/sesiones` | partes del trabajo | Sesiones del trabajo en orden de paquete (`numero_sesion`). |

Un turno o trabajo en el que el usuario no participa responde **404**, igual que uno inexistente: distinguirlos con un 403 filtraría que existe. Misma convención que en `README_PAGOS.md`.
