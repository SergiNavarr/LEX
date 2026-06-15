using Lex.Api.DTOs;

namespace Lex.Api.Services;

public interface IPostulacionService
{
    Task<PostulacionResponse> CrearAsync(int estudianteId, int idSolicitud, CrearPostulacionRequest request);
    Task<IReadOnlyList<PostulacionRecibidaResponse>> ListarRecibidasAsync(int clienteId, int idSolicitud);
    Task<IReadOnlyList<PostulacionResponse>> ListarMiasAsync(int estudianteId);
    Task<TrabajoResponse> AceptarAsync(int clienteId, int idPostulacion);
}
