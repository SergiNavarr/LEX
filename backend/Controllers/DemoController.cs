using Lex.Api.DTOs;
using Lex.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Controllers;

// Carga de datos de DEMOSTRACIÓN. Solo Admin. Separado del seeder base, que se
// ejecuta al arrancar la app (roles, tipos de servicio, instituciones, admin).
[ApiController]
[Route("api/demo")]
[Authorize(Roles = "Admin")]
public class DemoController : ControllerBase
{
    private readonly IDemoService _demo;

    public DemoController(IDemoService demo)
    {
        _demo = demo;
    }

    /// <summary>
    /// Carga (o recarga) todos los datos de demostración. Idempotente: primero
    /// borra los datos demo previos (por email @demo.com) y luego los recrea, así
    /// nunca duplica. No toca los datos base.
    /// </summary>
    [HttpPost("seed")]
    [ProducesResponseType(typeof(DemoSeedResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Seed()
    {
        return Ok(await _demo.SeedAsync());
    }

    /// <summary>Limpia únicamente los datos de demostración, sin recargarlos.</summary>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(DemoResetResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reset()
    {
        return Ok(await _demo.ResetAsync());
    }
}
