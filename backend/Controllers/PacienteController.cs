using Lex.Api.DTOs;
using Lex.Api.Extensions;
using Lex.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Controllers;

[ApiController]
[Route("api/pacientes")]
[Authorize(Roles = "Cliente")]
public class PacienteController : ControllerBase
{
    private readonly IPacienteService _pacientes;

    public PacienteController(IPacienteService pacientes)
    {
        _pacientes = pacientes;
    }

    /// <summary>El cliente autenticado registra un paciente a su cargo.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PacienteResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear([FromBody] CrearPacienteRequest request)
    {
        var paciente = await _pacientes.CrearAsync(User.GetUsuarioId(), request);
        return CreatedAtAction(nameof(Obtener), new { id = paciente.PacienteId }, paciente);
    }

    /// <summary>Lista los pacientes del cliente autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PacienteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mios()
    {
        return Ok(await _pacientes.ListarMiosAsync(User.GetUsuarioId()));
    }

    /// <summary>Detalle de un paciente, solo si pertenece al cliente autenticado.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PacienteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(int id)
    {
        return Ok(await _pacientes.ObtenerAsync(User.GetUsuarioId(), id));
    }
}
