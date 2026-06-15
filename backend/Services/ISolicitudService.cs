using Lex.Api.DTOs;

namespace Lex.Api.Services;

public interface ISolicitudService
{
    Task<SolicitudResponse> CrearAsync(int clienteId, CrearSolicitudRequest request);
    Task<IReadOnlyList<SolicitudResponse>> ListarAbiertasAsync(int? tipoServicioId, string? texto);
    Task<SolicitudResponse> ObtenerAsync(int idSolicitud);
    Task<IReadOnlyList<SolicitudMiaResponse>> ListarMiasAsync(int clienteId);
    Task CerrarAsync(int clienteId, int idSolicitud);
}
