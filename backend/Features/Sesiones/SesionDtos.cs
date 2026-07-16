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
