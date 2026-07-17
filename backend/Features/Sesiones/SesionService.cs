using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.Domain.Entities;
using Lex.Api.Domain.Enums;
using Lex.Api.Features.Pagos;
using Lex.Api.Features.Trabajos.Shared;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Features.Sesiones;

public class SesionService : ISesionService
{
    private readonly AppDbContext _db;
    private readonly IPagoService _pagos;
    private readonly ITrabajoService _trabajos;

    public SesionService(AppDbContext db, IPagoService pagos, ITrabajoService trabajos)
    {
        _db = db;
        _pagos = pagos;
        _trabajos = trabajos;
    }

    public async Task<IReadOnlyList<SesionResponse>> ListarDeTrabajoAsync(int usuarioId, int idTrabajo)
    {
        var trabajo = await _db.Trabajos.AsNoTracking()
            .Select(t => new { t.Id, t.ClienteId, t.EstudianteId })
            .FirstOrDefaultAsync(t => t.Id == idTrabajo);

        // Un trabajo ajeno se responde igual que uno inexistente, como en pagos y turnos.
        if (trabajo is null || (trabajo.ClienteId != usuarioId && trabajo.EstudianteId != usuarioId))
            throw new NotFoundException($"No existe el trabajo {idTrabajo}.");

        // El orden es el del paquete (1..N), no el cronologico: un turno reagendado no
        // deberia reordenar la lista de sesiones del trabajo.
        return await _db.Sesiones.AsNoTracking()
            .Where(s => s.TrabajoId == idTrabajo)
            .OrderBy(s => s.NumeroSesion)
            .Select(s => new SesionResponse(
                s.Id,
                s.NumeroSesion,
                s.Estado.ToString(),
                s.Turno.FechaHoraInicio,
                s.Turno.DuracionMinutos,
                s.FechaRealizada,
                s.Observaciones,
                s.Turno.LinkVideollamada))
            .ToListAsync();
    }

    public Task<SesionResponse> MarcarRealizadaAsync(int usuarioId, int idSesion, string? observaciones) =>
        MarcarAsync(usuarioId, idSesion, EstadoSesion.Realizada, EstadoTurno.Realizado, observaciones);

    // El cliente no vino, pero el estudiante bloqueo el horario y estuvo: cobra igual. La
    // unica diferencia con una sesion realizada es como queda registrada.
    public Task<SesionResponse> MarcarNoAsistioAsync(int usuarioId, int idSesion, string? observaciones) =>
        MarcarAsync(usuarioId, idSesion, EstadoSesion.NoAsistio, EstadoTurno.Ausente, observaciones);

    // Cerrar una sesion mueve plata, asi que sesion, turno, contador del trabajo y asientos
    // del escrow entran en la misma transaccion: una sesion marcada sin su liberacion (o al
    // reves) deja al estudiante sin cobrar o cobrando dos veces.
    private async Task<SesionResponse> MarcarAsync(
        int usuarioId, int idSesion, EstadoSesion estadoSesion, EstadoTurno estadoTurno, string? observaciones)
    {
        var sesion = await _db.Sesiones
            .Include(s => s.Turno)
            .Include(s => s.Trabajo)
            .FirstOrDefaultAsync(s => s.Id == idSesion);

        // Una sesion ajena se responde igual que una inexistente, como en pagos y turnos.
        if (sesion is null || (sesion.Trabajo.EstudianteId != usuarioId && sesion.Trabajo.ClienteId != usuarioId))
            throw new NotFoundException($"No existe la sesión {idSesion}.");

        // Marcar cobra: es el estudiante el que declara que dio la clase o la practica.
        if (sesion.Trabajo.EstudianteId != usuarioId)
            throw new ForbiddenException("Solo el estudiante del trabajo puede marcar una sesión.");

        if (sesion.Estado != EstadoSesion.Pendiente)
            throw new BadRequestException($"La sesión {idSesion} ya está en estado {sesion.Estado}.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            // Marcar la primera sesion arranca el trabajo. Se delega en la maquina de
            // estados para no saltear sus reglas: en Salud, iniciar sin consentimiento
            // firmado falla aca y la sesion no se marca.
            if (sesion.Trabajo.Estado == EstadoTrabajo.Aceptado)
                await _trabajos.IniciarAsync(usuarioId, sesion.TrabajoId);

            if (sesion.Trabajo.Estado != EstadoTrabajo.EnCurso)
                throw new BadRequestException(
                    $"No se pueden marcar sesiones de un trabajo en estado {sesion.Trabajo.Estado}.");

            var ahora = DateTime.UtcNow;
            sesion.Estado = estadoSesion;
            sesion.FechaRealizada = ahora;
            if (!string.IsNullOrWhiteSpace(observaciones))
                sesion.Observaciones = observaciones.Trim();

            sesion.Turno.Estado = estadoTurno;

            var (completadas, totales) = ContarSesiones(sesion.Trabajo);
            completadas += 1;
            AsignarCompletadas(sesion.Trabajo, completadas);

            var esUltima = completadas >= totales;
            await _pagos.LiberarFraccionPagoPorSesionAsync(sesion.TrabajoId, totales, esUltima);

            if (esUltima)
                await _trabajos.CompletarPorSesionesAsync(usuarioId, sesion.TrabajoId);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return (await ListarDeTrabajoAsync(usuarioId, sesion.TrabajoId)).First(s => s.Id == idSesion);
    }

    // Los contadores viven en TrabajoClase. Salud no los tiene porque una practica es
    // siempre una sesion: la primera que se marca es tambien la ultima.
    private static (int Completadas, int Totales) ContarSesiones(Trabajo trabajo) => trabajo switch
    {
        TrabajoClase c => (c.SesionesCompletadas, c.CantidadSesionesTotales),
        _ => (0, 1)
    };

    private static void AsignarCompletadas(Trabajo trabajo, int completadas)
    {
        if (trabajo is TrabajoClase c)
            c.SesionesCompletadas = completadas;
    }
}
