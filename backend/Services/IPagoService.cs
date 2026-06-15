using Lex.Api.DTOs;

namespace Lex.Api.Services;

public interface IPagoService
{
    Task<PagoResponse> ObtenerPorTrabajoAsync(int usuarioId, int idTrabajo);
    Task<MisPagosResponse> ListarMiosAsync(int estudianteId);
    Task<IngresosLexResponse> ObtenerIngresosLexAsync();
}
