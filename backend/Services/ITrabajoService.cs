using Lex.Api.DTOs;
using Lex.Api.Enums;

namespace Lex.Api.Services;

public interface ITrabajoService
{
    Task<TrabajoResponse> ContratarServicioAsync(int clienteId, ContratarServicioRequest request);
    Task<TrabajoResponse> ContratarServicioSaludAsync(int clienteId, ContratarServicioSaludRequest request);
    Task<IReadOnlyList<TrabajoResponse>> ListarMiosAsync(int usuarioId);
    Task<TrabajoResponse> ObtenerAsync(int usuarioId, int idTrabajo);
    Task<TrabajoResponse> CambiarEstadoAsync(int usuarioId, int idTrabajo, EstadoTrabajo nuevoEstado, string? supervisorResponsable = null);
    Task<IReadOnlyList<TrabajoHistorialResponse>> ListarHistorialAsync(int usuarioId, int idTrabajo);
}
