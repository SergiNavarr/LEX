using Lex.Api.Domain.Enums;

namespace Lex.Api.Features.Disponibilidad;

// Un bloque semanal del estudiante. Las horas son locales de Argentina (UTC-3).
public record BloqueDisponibilidadResponse(
    int Id,
    DiaSemana DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFin
);

// POST /api/disponibilidad y PUT /api/disponibilidad/{id}: mismo cuerpo.
public record CrearBloqueDisponibilidadRequest(
    DiaSemana DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFin
);
