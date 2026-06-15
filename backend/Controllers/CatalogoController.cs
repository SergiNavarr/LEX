using Lex.Api.DTOs;
using Lex.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Controllers;

[ApiController]
[Route("api/catalogo")]
public class CatalogoController : ControllerBase
{
    private readonly IPerfilService _perfil;

    public CatalogoController(IPerfilService perfil)
    {
        _perfil = perfil;
    }

    /// <summary>Catálogo público de carreras (con su institución) para poblar los selectores del frontend.</summary>
    [HttpGet("carreras")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CarreraCatalogoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Carreras()
    {
        return Ok(await _perfil.ListarCarrerasAsync());
    }
}
