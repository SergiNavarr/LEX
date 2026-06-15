using Lex.Api.DTOs;
using Lex.Api.Extensions;
using Lex.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Controllers;

[ApiController]
[Route("api/perfil")]
[Authorize]
public class PerfilController : ControllerBase
{
    private readonly IPerfilService _perfil;

    public PerfilController(IPerfilService perfil)
    {
        _perfil = perfil;
    }

    /// <summary>Identidad completa del usuario autenticado: datos, roles, tipo de cliente y perfil de estudiante.</summary>
    [HttpGet("yo")]
    [ProducesResponseType(typeof(IdentidadResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Yo()
    {
        return Ok(await _perfil.ObtenerIdentidadAsync(User.GetUsuarioId()));
    }

    /// <summary>
    /// Activa el perfil de estudiante del usuario autenticado.
    /// Solo para Clientes Particulares que aún no lo tienen (Empresa/Agencia -> 403; ya estudiante -> 400).
    /// </summary>
    [HttpPost("activar-estudiante")]
    [ProducesResponseType(typeof(IdentidadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ActivarEstudiante([FromBody] ActivarEstudianteRequest request)
    {
        return Ok(await _perfil.ActivarEstudianteAsync(User.GetUsuarioId(), request));
    }
}
