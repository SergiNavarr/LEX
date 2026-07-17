namespace Lex.Api.Features.Turnos;

// Reglas de "este horario se puede reservar", compartidas por la contratacion de Clase y
// de Salud. Los metodos devuelven el motivo del rechazo como string, o null si el slot es
// valido: quien llama decide si eso es un 400, un mensaje en pantalla o un skip.
public interface IValidadorTurnosService
{
    /// <summary>Valida un slot contra la agenda del estudiante. null si es valido; si no, el motivo.</summary>
    Task<string?> ValidarSlotAsync(int estudianteId, DateTime fechaHoraInicio, int duracionMinutos);

    /// <summary>Valida que los slots de un mismo paquete no choquen entre si. null si OK; si no, el motivo.</summary>
    string? ValidarSlotsNoSolapan(IReadOnlyList<DateTime> slots, int duracionMinutos);
}
