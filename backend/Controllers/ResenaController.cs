using Lex.Api.DTOs;
using Lex.Api.Extensions;
using Lex.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Controllers;

// Las rutas cruzan /trabajos y /usuarios, por eso no hay [Route] a nivel de clase.
[ApiController]
public class ResenaController : ControllerBase
{
    private readonly IResenaService _resenas;

    public ResenaController(IResenaService resenas)
    {
        _resenas = resenas;
    }

    /// <summary>Una parte del trabajo deja una reseña a la otra. El autor sale del token.</summary>
    [HttpPost("api/trabajos/{idTrabajo:int}/resenas")]
    [Authorize]
    [ProducesResponseType(typeof(ResenaResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(int idTrabajo, [FromBody] CrearResenaRequest request)
    {
        var resena = await _resenas.CrearAsync(User.GetUsuarioId(), idTrabajo, request);
        return StatusCode(StatusCodes.Status201Created, resena);
    }

    /// <summary>Reseñas recibidas por un usuario (reputación pública).</summary>
    [HttpGet("api/usuarios/{id:int}/resenas")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ResenaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Recibidas(int id)
    {
        return Ok(await _resenas.ListarRecibidasAsync(id));
    }

    /// <summary>Reseñas de un trabajo (solo sus partes).</summary>
    [HttpGet("api/trabajos/{idTrabajo:int}/resenas")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<ResenaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeTrabajo(int idTrabajo)
    {
        return Ok(await _resenas.ListarPorTrabajoAsync(User.GetUsuarioId(), idTrabajo));
    }
}
