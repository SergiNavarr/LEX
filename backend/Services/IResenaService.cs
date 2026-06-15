using Lex.Api.DTOs;

namespace Lex.Api.Services;

public interface IResenaService
{
    Task<ResenaResponse> CrearAsync(int autorId, int idTrabajo, CrearResenaRequest request);
    Task<IReadOnlyList<ResenaResponse>> ListarRecibidasAsync(int usuarioId);
    Task<IReadOnlyList<ResenaResponse>> ListarPorTrabajoAsync(int usuarioId, int idTrabajo);
}
