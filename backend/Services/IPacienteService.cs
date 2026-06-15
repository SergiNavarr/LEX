using Lex.Api.DTOs;

namespace Lex.Api.Services;

public interface IPacienteService
{
    Task<PacienteResponse> CrearAsync(int clienteId, CrearPacienteRequest request);
    Task<IReadOnlyList<PacienteResponse>> ListarMiosAsync(int clienteId);
    Task<PacienteResponse> ObtenerAsync(int clienteId, int idPaciente);
}
