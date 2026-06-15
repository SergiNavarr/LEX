using Lex.Api.DTOs;
using Lex.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IPagoService _pagos;

    public AdminController(IPagoService pagos)
    {
        _pagos = pagos;
    }

    /// <summary>Panel del modelo de ingresos de LEX: comision liberada (efectiva) vs retenida (potencial).</summary>
    [HttpGet("ingresos")]
    [ProducesResponseType(typeof(IngresosLexResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Ingresos()
    {
        return Ok(await _pagos.ObtenerIngresosLexAsync());
    }
}
