using Lex.Api.Domain.Enums;

namespace Lex.Api.Features.Turnos;

public interface ITurnoService
{
    // desde/hasta son fechas locales de Argentina (no instantes UTC).
    Task<IReadOnlyList<TurnoResponse>> ListarMiosAsync(int usuarioId, EstadoTurno? estado, DateOnly? desde, DateOnly? hasta);
    Task<TurnoDetalleResponse> ObtenerDetalleAsync(int usuarioId, int idTurno);
    Task<IReadOnlyList<SlotDisponibleResponse>> ListarSlotsDisponiblesAsync(int estudianteId, DateOnly desde, DateOnly hasta, int duracionMinutos);

    // Cancela un turno futuro y arrastra su sesion. Si el trabajo se queda sin sesiones,
    // se cancela entero y se reembolsa.
    Task<TurnoDetalleResponse> CancelarAsync(int usuarioId, int idTurno, string? motivo);
}
