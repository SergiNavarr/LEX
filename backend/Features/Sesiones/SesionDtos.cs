namespace Lex.Api.Features.Sesiones;

// GET /api/trabajos/{trabajoId}/sesiones: una fila por sesion del trabajo.
// Los datos de agenda (fecha, duracion, link) salen del turno que consume la sesion.
public record SesionResponse(
    int Id,
    int NumeroSesion,
    string Estado,
    DateTime FechaHoraInicio,
    int DuracionMinutos,
    DateTime? FechaRealizada,
    string? Observaciones,
    string? LinkVideollamada
);

// POST /api/sesiones/{id}/realizar y /no-asistio: notas del estudiante sobre la sesión.
public record MarcarSesionRequest(string? Observaciones);
