namespace Lex.Api.Features.Disponibilidad;

public interface IDisponibilidadService
{
    Task<IReadOnlyList<BloqueDisponibilidadResponse>> ListarMiosAsync(int estudianteId);
    Task<IReadOnlyList<BloqueDisponibilidadResponse>> ListarDeEstudianteAsync(int estudianteId);
    Task<BloqueDisponibilidadResponse> CrearAsync(int estudianteId, CrearBloqueDisponibilidadRequest request);
    Task<BloqueDisponibilidadResponse> ActualizarAsync(int estudianteId, int idBloque, CrearBloqueDisponibilidadRequest request);
    Task DesactivarAsync(int estudianteId, int idBloque);
}
