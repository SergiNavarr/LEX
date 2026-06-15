using Lex.Api.DTOs;

namespace Lex.Api.Services;

public interface IPerfilService
{
    /// <summary>Identidad completa del usuario autenticado (roles, tipo de cliente, estudiante, carreras).</summary>
    Task<IdentidadResponse> ObtenerIdentidadAsync(int usuarioId);

    /// <summary>Activa el perfil de estudiante. Solo para Clientes Particulares que aun no lo tienen.</summary>
    Task<IdentidadResponse> ActivarEstudianteAsync(int usuarioId, ActivarEstudianteRequest request);

    /// <summary>Catalogo de carreras (con su institucion) para poblar los selectores del frontend.</summary>
    Task<IReadOnlyList<CarreraCatalogoResponse>> ListarCarrerasAsync();
}
