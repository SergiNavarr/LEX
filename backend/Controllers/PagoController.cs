using Lex.Api.DTOs;
using Lex.Api.Extensions;
using Lex.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Controllers;

[ApiController]
[Route("api/pagos")]
[Authorize(Roles = "Estudiante")]
public class PagoController : ControllerBase
{
    private readonly IPagoService _pagos;

    public PagoController(IPagoService pagos)
    {
        _pagos = pagos;
    }

    /// <summary>Pagos del estudiante autenticado, con total cobrado (liberado) y retenido (pendiente).</summary>
    [HttpGet("mios")]
    [ProducesResponseType(typeof(MisPagosResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mios()
    {
        return Ok(await _pagos.ListarMiosAsync(User.GetUsuarioId()));
    }
}
