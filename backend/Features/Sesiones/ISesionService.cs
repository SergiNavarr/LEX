namespace Lex.Api.Features.Sesiones;

public interface ISesionService
{
    Task<IReadOnlyList<SesionResponse>> ListarDeTrabajoAsync(int usuarioId, int idTrabajo);
}
