using Lex.Api.DTOs;
using Lex.Api.Extensions;
using Lex.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Controllers;

[ApiController]
[Route("api/solicitudes")]
public class SolicitudController : ControllerBase
{
    private readonly ISolicitudService _solicitudes;

    public SolicitudController(ISolicitudService solicitudes)
    {
        _solicitudes = solicitudes;
    }

    /// <summary>Un cliente publica una solicitud (nace Abierta).</summary>
    [HttpPost]
    [Authorize(Roles = "Cliente")]
    [ProducesResponseType(typeof(SolicitudResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear([FromBody] CrearSolicitudRequest request)
    {
        var solicitud = await _solicitudes.CrearAsync(User.GetUsuarioId(), request);
        return CreatedAtAction(nameof(Obtener), new { id = solicitud.IdSolicitud }, solicitud);
    }

    /// <summary>Listado publico de solicitudes Abiertas, con filtros opcionales.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<SolicitudResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] int? tipoServicioId, [FromQuery] string? texto)
    {
        return Ok(await _solicitudes.ListarAbiertasAsync(tipoServicioId, texto));
    }

    /// <summary>Las solicitudes del cliente autenticado, con su cantidad de postulaciones.</summary>
    [HttpGet("mias")]
    [Authorize(Roles = "Cliente")]
    [ProducesResponseType(typeof(IEnumerable<SolicitudMiaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mias()
    {
        return Ok(await _solicitudes.ListarMiasAsync(User.GetUsuarioId()));
    }

    /// <summary>Detalle publico de una solicitud.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SolicitudResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(int id)
    {
        return Ok(await _solicitudes.ObtenerAsync(id));
    }

    /// <summary>El dueño cierra/cancela su solicitud (estado Cancelada).</summary>
    [HttpPatch("{id:int}/cerrar")]
    [Authorize(Roles = "Cliente")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cerrar(int id)
    {
        await _solicitudes.CerrarAsync(User.GetUsuarioId(), id);
        return NoContent();
    }
}
