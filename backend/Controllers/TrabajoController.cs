using Lex.Api.DTOs;
using Lex.Api.Extensions;
using Lex.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Controllers;

[ApiController]
[Route("api/trabajos")]
[Authorize]
public class TrabajoController : ControllerBase
{
    private readonly ITrabajoService _trabajos;
    private readonly IPagoService _pagos;

    public TrabajoController(ITrabajoService trabajos, IPagoService pagos)
    {
        _trabajos = trabajos;
        _pagos = pagos;
    }

    /// <summary>Contratacion directa (Flujo 1): un cliente contrata un servicio existente.</summary>
    [HttpPost("contratar-servicio")]
    [ProducesResponseType(typeof(TrabajoResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> ContratarServicio([FromBody] ContratarServicioRequest request)
    {
        var trabajo = await _trabajos.ContratarServicioAsync(User.GetUsuarioId(), request);
        return CreatedAtAction(nameof(Obtener), new { id = trabajo.IdTrabajo }, trabajo);
    }

    /// <summary>Contratacion de Salud (Flujo 3): un cliente contrata un servicio que requiere supervision, para un paciente, aceptando el consentimiento.</summary>
    [HttpPost("contratar-servicio-salud")]
    [Authorize(Roles = "Cliente")]
    [ProducesResponseType(typeof(TrabajoResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> ContratarServicioSalud([FromBody] ContratarServicioSaludRequest request)
    {
        var trabajo = await _trabajos.ContratarServicioSaludAsync(User.GetUsuarioId(), request);
        return CreatedAtAction(nameof(Obtener), new { id = trabajo.IdTrabajo }, trabajo);
    }

    /// <summary>Trabajos donde participa el usuario autenticado (como estudiante o cliente).</summary>
    [HttpGet("mios")]
    [ProducesResponseType(typeof(IEnumerable<TrabajoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mios()
    {
        return Ok(await _trabajos.ListarMiosAsync(User.GetUsuarioId()));
    }

    /// <summary>Detalle de un trabajo, solo para su estudiante o su cliente.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TrabajoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(int id)
    {
        return Ok(await _trabajos.ObtenerAsync(User.GetUsuarioId(), id));
    }

    /// <summary>Transicion de estado del trabajo, validada por la maquina de estados y el rol.</summary>
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(typeof(TrabajoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request)
    {
        var trabajo = await _trabajos.CambiarEstadoAsync(User.GetUsuarioId(), id, request.NuevoEstado!.Value, request.SupervisorResponsable);
        return Ok(trabajo);
    }

    /// <summary>Detalle del pago (escrow) del trabajo. Solo para sus partes (estudiante o cliente).</summary>
    [HttpGet("{id:int}/pago")]
    [ProducesResponseType(typeof(PagoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Pago(int id)
    {
        return Ok(await _pagos.ObtenerPorTrabajoAsync(User.GetUsuarioId(), id));
    }

    /// <summary>Historial de cambios de estado del trabajo (solo para sus partes).</summary>
    [HttpGet("{id:int}/historial")]
    [ProducesResponseType(typeof(IEnumerable<TrabajoHistorialResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Historial(int id)
    {
        return Ok(await _trabajos.ListarHistorialAsync(User.GetUsuarioId(), id));
    }
}
