using Lex.Api.Domain.Enums;

namespace Lex.Api.Features.Trabajos.Shared;

public interface ITrabajoService
{
    // Transiciones de estado (maquina de estados + autorizacion por rol en el trabajo).
    Task<TrabajoResponse> AceptarAsync(int usuarioId, int idTrabajo);
    Task<TrabajoResponse> IniciarAsync(int usuarioId, int idTrabajo);
    Task<TrabajoResponse> EntregarAsync(int usuarioId, int idTrabajo);
    Task<TrabajoResponse> CompletarAsync(int usuarioId, int idTrabajo);
    Task<TrabajoResponse> CancelarAsync(int usuarioId, int idTrabajo, string? motivo);
    Task<TrabajoResponse> DisputarAsync(int usuarioId, int idTrabajo, string motivo);

    // Cierre de Clase/Salud disparado por la ultima sesion marcada. No pasa por la maquina
    // de estados ni toca el escrow: la plata ya se libero fraccion a fraccion.
    Task<TrabajoResponse> CompletarPorSesionesAsync(int usuarioId, int idTrabajo);

    // Consultas unificadas (agnosticas a la vertical).
    Task<IReadOnlyList<TrabajoResponse>> ListarAsync(TipoServicio? tipo, EstadoTrabajo? estado, int? clienteId, int? estudianteId);
    Task<TrabajoDetalleResponse> ObtenerDetalleAsync(int usuarioId, int idTrabajo);
    Task<IReadOnlyList<TrabajoResponse>> ListarMiosAsync(int usuarioId);
    Task<IReadOnlyList<TrabajoHistorialResponse>> ListarHistorialAsync(int usuarioId, int idTrabajo);
}
