using Lex.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Features.Sesiones;

// Las sesiones de un trabajo de Clase o Salud. Cuelgan de la ruta del trabajo porque no
// tienen identidad fuera de el. Marcar una sesion como realizada llega en Hito 2 Parte 3.
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
}
