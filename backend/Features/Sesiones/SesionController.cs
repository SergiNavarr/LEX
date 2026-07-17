using Lex.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Features.Sesiones;

// Las sesiones de un trabajo de Clase o Salud. El listado cuelga de la ruta del trabajo
// porque una sesion no tiene sentido fuera de el; las acciones sobre una sesion concreta
// usan su propia ruta (~/api/sesiones/{id}/...), que es la que conoce quien ya la tiene.
[ApiController]
[Route("api/trabajos/{trabajoId:int}/sesiones")]
[Authorize]
public class SesionController : ControllerBase
{
    private readonly ISesionService _sesiones;

    public SesionController(ISesionService sesiones)
    {
        _sesiones = sesiones;
    }

    /// <summary>Sesiones del trabajo, en orden de paquete. 404 si el trabajo no existe o no participas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SesionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeTrabajo(int trabajoId) =>
        Ok(await _sesiones.ListarDeTrabajoAsync(User.GetUsuarioId(), trabajoId));

    /// <summary>El estudiante da la sesión por cumplida. Libera su fracción del pago; la última completa el trabajo.</summary>
    [HttpPost("~/api/sesiones/{id:int}/realizar")]
    [Authorize(Roles = "Estudiante")]
    [ProducesResponseType(typeof(SesionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Realizar(int id, [FromBody] MarcarSesionRequest? request) =>
        Ok(await _sesiones.MarcarRealizadaAsync(User.GetUsuarioId(), id, request?.Observaciones));

    /// <summary>El cliente no se presentó. Libera la fracción igual: el estudiante reservó el horario y estuvo.</summary>
    [HttpPost("~/api/sesiones/{id:int}/no-asistio")]
    [Authorize(Roles = "Estudiante")]
    [ProducesResponseType(typeof(SesionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NoAsistio(int id, [FromBody] MarcarSesionRequest? request) =>
        Ok(await _sesiones.MarcarNoAsistioAsync(User.GetUsuarioId(), id, request?.Observaciones));
}
