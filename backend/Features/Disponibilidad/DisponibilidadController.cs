using Lex.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Features.Disponibilidad;

// Agenda semanal que el estudiante ofrece para sus servicios de Clase y Salud.
// Es global por estudiante: no se configura por servicio.
[ApiController]
[Route("api/disponibilidad")]
[Authorize(Roles = "Estudiante")]
public class DisponibilidadController : ControllerBase
{
    private readonly IDisponibilidadService _disponibilidad;

    public DisponibilidadController(IDisponibilidadService disponibilidad)
    {
        _disponibilidad = disponibilidad;
    }

    /// <summary>Bloques activos del estudiante autenticado.</summary>
    [HttpGet("mia")]
    [ProducesResponseType(typeof(IEnumerable<BloqueDisponibilidadResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mia() =>
        Ok(await _disponibilidad.ListarMiosAsync(User.GetUsuarioId()));

    /// <summary>Crea un bloque semanal. 400 si la franja es invalida o pisa otro bloque del mismo dia.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BloqueDisponibilidadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear([FromBody] CrearBloqueDisponibilidadRequest request)
    {
        var bloque = await _disponibilidad.CrearAsync(User.GetUsuarioId(), request);
        return CreatedAtAction(nameof(Mia), new { }, bloque);
    }

    /// <summary>Edita un bloque propio. 404 si no existe o no es tuyo.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(BloqueDisponibilidadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] CrearBloqueDisponibilidadRequest request) =>
        Ok(await _disponibilidad.ActualizarAsync(User.GetUsuarioId(), id, request));

    /// <summary>Da de baja un bloque propio (baja logica). No afecta turnos ya reservados.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _disponibilidad.DesactivarAsync(User.GetUsuarioId(), id);
        return NoContent();
    }

    /// <summary>Bloques activos de un estudiante. Publico: el cliente lo consulta antes de contratar.</summary>
    [HttpGet("estudiante/{estudianteId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<BloqueDisponibilidadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeEstudiante(int estudianteId) =>
        Ok(await _disponibilidad.ListarDeEstudianteAsync(estudianteId));
}
