using Lex.Api.DTOs;
using Lex.Api.Extensions;
using Lex.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Controllers;

// Las rutas cruzan /solicitudes y /postulaciones, por eso no hay [Route] a nivel de clase.
[ApiController]
public class PostulacionController : ControllerBase
{
    private readonly IPostulacionService _postulaciones;

    public PostulacionController(IPostulacionService postulaciones)
    {
        _postulaciones = postulaciones;
    }

    /// <summary>Un estudiante se postula a una solicitud abierta.</summary>
    [HttpPost("api/solicitudes/{idSolicitud:int}/postulaciones")]
    [Authorize(Roles = "Estudiante")]
    [ProducesResponseType(typeof(PostulacionResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Postularse(int idSolicitud, [FromBody] CrearPostulacionRequest request)
    {
        var postulacion = await _postulaciones.CrearAsync(User.GetUsuarioId(), idSolicitud, request);
        return StatusCode(StatusCodes.Status201Created, postulacion);
    }

    /// <summary>El cliente dueño ve las postulaciones recibidas en su solicitud.</summary>
    [HttpGet("api/solicitudes/{idSolicitud:int}/postulaciones")]
    [Authorize(Roles = "Cliente")]
    [ProducesResponseType(typeof(IEnumerable<PostulacionRecibidaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Recibidas(int idSolicitud)
    {
        return Ok(await _postulaciones.ListarRecibidasAsync(User.GetUsuarioId(), idSolicitud));
    }

    /// <summary>Las postulaciones enviadas por el estudiante autenticado.</summary>
    [HttpGet("api/postulaciones/mias")]
    [Authorize(Roles = "Estudiante")]
    [ProducesResponseType(typeof(IEnumerable<PostulacionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mias()
    {
        return Ok(await _postulaciones.ListarMiasAsync(User.GetUsuarioId()));
    }

    /// <summary>El cliente dueño acepta una postulación: cierra la solicitud y crea el trabajo.</summary>
    [HttpPost("api/postulaciones/{id:int}/aceptar")]
    [Authorize(Roles = "Cliente")]
    [ProducesResponseType(typeof(TrabajoResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Aceptar(int id)
    {
        var trabajo = await _postulaciones.AceptarAsync(User.GetUsuarioId(), id);
        return Created($"/api/trabajos/{trabajo.IdTrabajo}", trabajo);
    }
}
