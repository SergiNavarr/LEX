using Lex.Api.Domain.Enums;
using Lex.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Features.Turnos;

// Consulta de la agenda. Sin rol fijo: el mismo turno lo miran su estudiante y su cliente.
// La creacion y cancelacion de turnos llegan en Hito 2 Parte 2, junto con la contratacion.
[ApiController]
[Route("api/turnos")]
[Authorize]
public class TurnoController : ControllerBase
{
    private readonly ITurnoService _turnos;

    public TurnoController(ITurnoService turnos)
    {
        _turnos = turnos;
    }

    /// <summary>Turnos del usuario autenticado (como estudiante o cliente), los mas proximos primero.</summary>
    /// <remarks>desde/hasta son fechas locales de Argentina, inclusive ambas.</remarks>
    [HttpGet("mios")]
    [ProducesResponseType(typeof(IEnumerable<TurnoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mios(
        [FromQuery] EstadoTurno? estado,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta) =>
        Ok(await _turnos.ListarMiosAsync(User.GetUsuarioId(), estado, desde, hasta));

    /// <summary>Detalle de un turno con su sesion asociada. 404 si no existe o no participas.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TurnoDetalleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id) =>
        Ok(await _turnos.ObtenerDetalleAsync(User.GetUsuarioId(), id));

    /// <summary>Cancela un turno futuro propio. Arrastra la sesión asociada y, si era la última, el trabajo entero.</summary>
    [HttpPost("{id:int}/cancelar")]
    [ProducesResponseType(typeof(TurnoDetalleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarTurnoRequest? request) =>
        Ok(await _turnos.CancelarAsync(User.GetUsuarioId(), id, request?.Motivo));

    // Reservado para el dia que exista el flujo Reservado -> Confirmado. Hoy los turnos
    // nacen Confirmados al contratar, asi que no hay nada que confirmar y el endpoint no
    // se publica en Swagger para no ofrecer una accion que no hace nada.
    /// <summary>No implementado: los turnos ya nacen Confirmados (ver README_TURNOS.md).</summary>
    [HttpPost("{id:int}/confirmar")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Confirmar(int id) =>
        StatusCode(StatusCodes.Status501NotImplemented, new
        {
            error = "Los turnos se crean ya confirmados: la disponibilidad publicada por el estudiante " +
                    "es la aceptación. Este endpoint queda reservado para un futuro flujo de confirmación explícita."
        });

    /// <summary>Huecos libres en la agenda de un estudiante. Publico: se consulta antes de contratar.</summary>
    /// <remarks>
    /// desde/hasta son fechas locales de Argentina (inclusive ambas); duracion_minutos sale
    /// del servicio que se va a contratar. Las fechas de los slots vuelven en UTC.
    /// </remarks>
    [HttpGet("disponibles/estudiante/{estudianteId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<SlotDisponibleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Disponibles(
        int estudianteId,
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        [FromQuery(Name = "duracion_minutos")] int duracionMinutos = 60) =>
        Ok(await _turnos.ListarSlotsDisponiblesAsync(estudianteId, desde, hasta, duracionMinutos));
}
