namespace Lex.Api.Features.Sesiones;

public interface ISesionService
{
    Task<IReadOnlyList<SesionResponse>> ListarDeTrabajoAsync(int usuarioId, int idTrabajo);

    /// <summary>El estudiante da la sesión por cumplida. Libera la fracción de pago que le toca.</summary>
    Task<SesionResponse> MarcarRealizadaAsync(int usuarioId, int idSesion, string? observaciones);

    /// <summary>El cliente no se presentó. Libera la fracción igual: el estudiante puso su tiempo.</summary>
    Task<SesionResponse> MarcarNoAsistioAsync(int usuarioId, int idSesion, string? observaciones);
}
