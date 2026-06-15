using Lex.Api.DTOs;
using Lex.Api.Extensions;
using Lex.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Controllers;

[ApiController]
[Route("api/servicios")]
[Authorize(Roles = "Estudiante")] // por defecto: solo estudiantes gestionan su oferta
public class ServicioController : ControllerBase
{
    private readonly IServicioService _servicios;

    public ServicioController(IServicioService servicios)
    {
        _servicios = servicios;
    }

    /// <summary>El estudiante autenticado publica un servicio.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ServicioResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear([FromBody] CrearServicioRequest request)
    {
        var servicio = await _servicios.CrearAsync(User.GetUsuarioId(), request);
        return CreatedAtAction(nameof(Obtener), new { id = servicio.IdServicio }, servicio);
    }

    /// <summary>Edita un servicio propio.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ServicioResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarServicioRequest request)
    {
        var servicio = await _servicios.ActualizarAsync(User.GetUsuarioId(), id, request);
        return Ok(servicio);
    }

    /// <summary>Baja logica de un servicio propio (activo = false).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicios.EliminarAsync(User.GetUsuarioId(), id);
        return NoContent();
    }

    /// <summary>Listado publico de servicios activos, con filtros opcionales.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ServicioResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] int? tipoServicioId, [FromQuery] string? texto)
    {
        return Ok(await _servicios.ListarAsync(tipoServicioId, texto));
    }

    /// <summary>Detalle publico de un servicio.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServicioResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(int id)
    {
        return Ok(await _servicios.ObtenerAsync(id));
    }
}
