namespace Lex.Api.Features.Turnos;

// GET /api/turnos/mios: una fila por turno del usuario logueado.
// Las fechas van en UTC; la conversion a hora argentina la hace el cliente.
public record TurnoResponse(
    int Id,
    int EstudianteId,
    string EstudianteNombre,
    int ClienteId,
    string ClienteNombre,
    string RolUsuario,   // "Estudiante" | "Cliente" (segun el logueado)
    DateTime FechaHoraInicio,
    DateTime FechaHoraFin,
    int DuracionMinutos,
    string Estado,
    string? LinkVideollamada,
    DateTime FechaCreacion
);

// GET /api/turnos/{id}: el turno con sus notas y la sesion que lo consume, si tiene.
public record TurnoDetalleResponse(
    int Id,
    int EstudianteId,
    string EstudianteNombre,
    int ClienteId,
    string ClienteNombre,
    string RolUsuario,
    DateTime FechaHoraInicio,
    DateTime FechaHoraFin,
    int DuracionMinutos,
    string Estado,
    string? LinkVideollamada,
    string? NotasEstudiante,
    string? NotasCliente,
    DateTime FechaCreacion,
    SesionDeTurnoResponse? Sesion   // null en turnos sin trabajo asociado
);

// La sesion vista desde su turno: lo justo para saltar al trabajo.
public record SesionDeTurnoResponse(
    int Id,
    int TrabajoId,
    string TituloTrabajo,
    int NumeroSesion,
    string Estado
);

// Hueco libre en la agenda del estudiante. No es una entidad: se calcula al vuelo
// proyectando los bloques de disponibilidad sobre un rango y restando los turnos tomados.
public record SlotDisponibleResponse(
    DateTime FechaHoraInicio,
    DateTime FechaHoraFin,
    int DuracionMinutos
);

// POST /api/turnos/{id}/cancelar. El motivo es opcional y queda en el historial del
// trabajo si la cancelacion termina cancelandolo entero.
public record CancelarTurnoRequest(string? Motivo);
