using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.Domain.Entities;
using Lex.Api.Domain.Enums;
using Lex.Api.Features.Pagos;
using Lex.Api.Features.Trabajos.Shared;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Features.Turnos;

public class TurnoService : ITurnoService
{
    private readonly AppDbContext _db;
    private readonly ITrabajoService _trabajos;
    private readonly IPagoService _pagos;

    // Tope del rango de busqueda de slots. Un rango abierto obligaria a materializar en
    // memoria todos los huecos de la agenda: 62 dias cubre la vista de 2 meses de un
    // calendario, que es el caso de uso real.
    private const int MaxDiasRangoSlots = 62;

    public TurnoService(AppDbContext db, ITrabajoService trabajos, IPagoService pagos)
    {
        _db = db;
        _trabajos = trabajos;
        _pagos = pagos;
    }

    public async Task<IReadOnlyList<TurnoResponse>> ListarMiosAsync(int usuarioId, EstadoTurno? estado, DateOnly? desde, DateOnly? hasta)
    {
        var query = TurnosConPartes()
            .Where(t => t.EstudianteId == usuarioId || t.ClienteId == usuarioId);

        if (estado is EstadoTurno e)
            query = query.Where(t => t.Estado == e);

        // Los filtros son fechas argentinas: se traducen a la ventana UTC equivalente.
        if (desde is DateOnly d)
        {
            var inicioUtc = HorarioArgentina.InicioDelDiaUtc(d);
            query = query.Where(t => t.FechaHoraInicio >= inicioUtc);
        }
        if (hasta is DateOnly h)
        {
            // Fin exclusivo: 'hasta' incluye todo su dia local.
            var finUtc = HorarioArgentina.FinDelDiaUtc(h);
            query = query.Where(t => t.FechaHoraInicio < finUtc);
        }

        // Los mas proximos primero: la agenda se lee hacia adelante.
        var turnos = await query.OrderBy(t => t.FechaHoraInicio).ToListAsync();

        return turnos.Select(t => new TurnoResponse(
            t.Id,
            t.EstudianteId,
            t.Estudiante.Usuario.NombreCompleto,
            t.ClienteId,
            t.Cliente.Usuario.NombreCompleto,
            RolDe(usuarioId, t),
            t.FechaHoraInicio,
            t.FechaHoraInicio.AddMinutes(t.DuracionMinutos),
            t.DuracionMinutos,
            t.Estado.ToString(),
            t.LinkVideollamada,
            t.FechaCreacion)).ToList();
    }

    public async Task<TurnoDetalleResponse> ObtenerDetalleAsync(int usuarioId, int idTurno)
    {
        var turno = await TurnosConPartes()
            .Include(t => t.Sesion!).ThenInclude(s => s.Trabajo)
            .FirstOrDefaultAsync(t => t.Id == idTurno);

        // Un turno ajeno se responde igual que uno inexistente: distinguirlos con un 403
        // filtraria que el turno existe.
        if (turno is null || (turno.EstudianteId != usuarioId && turno.ClienteId != usuarioId))
            throw new NotFoundException($"No existe el turno {idTurno}.");

        var sesion = turno.Sesion is null ? null : new SesionDeTurnoResponse(
            turno.Sesion.Id,
            turno.Sesion.TrabajoId,
            turno.Sesion.Trabajo.TituloSnapshot,
            turno.Sesion.NumeroSesion,
            turno.Sesion.Estado.ToString());

        return new TurnoDetalleResponse(
            turno.Id,
            turno.EstudianteId,
            turno.Estudiante.Usuario.NombreCompleto,
            turno.ClienteId,
            turno.Cliente.Usuario.NombreCompleto,
            RolDe(usuarioId, turno),
            turno.FechaHoraInicio,
            turno.FechaHoraInicio.AddMinutes(turno.DuracionMinutos),
            turno.DuracionMinutos,
            turno.Estado.ToString(),
            turno.LinkVideollamada,
            turno.NotasEstudiante,
            turno.NotasCliente,
            turno.FechaCreacion,
            sesion);
    }

    // Proyecta los bloques semanales del estudiante sobre el rango pedido y le resta los
    // turnos ya tomados. El resultado no se persiste: un slot solo existe mientras nadie
    // lo reserve.
    //
    // Cuesta 2 queries fijas (bloques + turnos de la ventana) y el resto es aritmetica en
    // memoria: el numero de dias del rango no multiplica los viajes a la DB.
    public async Task<IReadOnlyList<SlotDisponibleResponse>> ListarSlotsDisponiblesAsync(
        int estudianteId, DateOnly desde, DateOnly hasta, int duracionMinutos)
    {
        if (!await _db.PerfilesEstudiante.AnyAsync(p => p.UsuarioId == estudianteId))
            throw new NotFoundException($"No existe el estudiante {estudianteId}.");

        if (duracionMinutos <= 0)
            throw new BadRequestException("La duración de la sesión debe ser mayor a cero minutos.");

        if (hasta < desde)
            throw new BadRequestException($"La fecha 'hasta' ({hasta:yyyy-MM-dd}) no puede ser anterior a 'desde' ({desde:yyyy-MM-dd}).");

        var dias = hasta.DayNumber - desde.DayNumber + 1;
        if (dias > MaxDiasRangoSlots)
            throw new BadRequestException($"El rango no puede superar los {MaxDiasRangoSlots} días (pediste {dias}).");

        var bloques = await _db.DisponibilidadesEstudiante.AsNoTracking()
            .Where(d => d.EstudianteId == estudianteId && d.Activo)
            .Select(d => new { d.DiaSemana, d.HoraInicio, d.HoraFin })
            .ToListAsync();

        if (bloques.Count == 0)
            return Array.Empty<SlotDisponibleResponse>();

        var ventanaInicio = HorarioArgentina.InicioDelDiaUtc(desde);
        var ventanaFin = HorarioArgentina.FinDelDiaUtc(hasta);

        // El margen de un dia hacia atras atrapa el turno que arranca antes de la ventana
        // pero termina adentro: la duracion es una columna, asi que el solapamiento exacto
        // se decide en memoria y no en SQL.
        var ocupados = (await _db.Turnos.AsNoTracking()
                .Where(t => t.EstudianteId == estudianteId
                            && EstadosDeAgenda.Ocupan.Contains(t.Estado)
                            && t.FechaHoraInicio < ventanaFin
                            && t.FechaHoraInicio >= ventanaInicio.AddDays(-1))
                .Select(t => new { t.FechaHoraInicio, t.DuracionMinutos })
                .ToListAsync())
            .Select(t => (Inicio: t.FechaHoraInicio, Fin: t.FechaHoraInicio.AddMinutes(t.DuracionMinutos)))
            .ToList();

        var bloquesPorDia = bloques.ToLookup(b => b.DiaSemana);
        // Un slot que ya arranco no se puede reservar. No hay anticipacion minima mas alla
        // de eso: reservar para dentro de 10 minutos es valido.
        var ahora = DateTime.UtcNow;
        var slots = new List<SlotDisponibleResponse>();

        for (var dia = desde; dia <= hasta; dia = dia.AddDays(1))
        {
            var medianocheUtc = HorarioArgentina.InicioDelDiaUtc(dia);

            foreach (var bloque in bloquesPorDia[HorarioArgentina.DiaSemanaDe(dia)])
            {
                // Se cuenta en minutos desde medianoche en vez de sumar sobre TimeOnly:
                // un bloque que llega a las 23:00 con slots de 60' daria la vuelta al reloj.
                var finBloque = HorarioArgentina.MinutosDeDia(bloque.HoraFin);

                for (var minuto = HorarioArgentina.MinutosDeDia(bloque.HoraInicio);
                     minuto + duracionMinutos <= finBloque;
                     minuto += duracionMinutos)
                {
                    var inicio = medianocheUtc.AddMinutes(minuto);
                    var fin = inicio.AddMinutes(duracionMinutos);

                    if (inicio < ahora)
                        continue;

                    if (ocupados.Any(o => o.Inicio < fin && inicio < o.Fin))
                        continue;

                    slots.Add(new SlotDisponibleResponse(inicio, fin, duracionMinutos));
                }
            }
        }

        // Un dia puede tener varios bloques, asi que el orden final se impone al cerrar.
        return slots.OrderBy(s => s.FechaHoraInicio).ToList();
    }

    // Cancela un turno futuro. El turno arrastra a su sesion, y la sesion descuenta una
    // unidad del trabajo: un paquete de 4 al que le cancelan una clase pasa a ser de 3, y
    // el cliente termina debiendo 3. La plata NO se toca aca; el reparto se recalcula al
    // liberar (Parte 3), dividiendo por el CantidadSesionesTotales vigente en ese momento.
    public async Task<TurnoDetalleResponse> CancelarAsync(int usuarioId, int idTurno, string? motivo)
    {
        var turno = await _db.Turnos.FirstOrDefaultAsync(t => t.Id == idTurno);

        if (turno is null || (turno.EstudianteId != usuarioId && turno.ClienteId != usuarioId))
            throw new NotFoundException($"No existe el turno {idTurno}.");

        if (turno.Estado is not (EstadoTurno.Reservado or EstadoTurno.Confirmado))
            throw new BadRequestException($"No se puede cancelar un turno en estado {turno.Estado}.");

        // Sin regla de anticipacion: se cancela hasta un minuto antes. Lo unico que no se
        // puede es cancelar hacia atras, que seria reescribir lo que ya ocurrio.
        if (turno.FechaHoraInicio <= DateTime.UtcNow)
            throw new BadRequestException(
                $"No se puede cancelar un turno que ya ocurrió ({HorarioArgentina.Describir(turno.FechaHoraInicio)}).");

        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            turno.Estado = EstadoTurno.Cancelado;

            var sesion = await _db.Sesiones.FirstOrDefaultAsync(s => s.TurnoId == idTurno);

            if (sesion is not null && sesion.Estado == EstadoSesion.Pendiente)
            {
                sesion.Estado = EstadoSesion.Cancelada;
                await DescontarSesionDelTrabajoAsync(usuarioId, sesion.TrabajoId, motivo);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return await ObtenerDetalleAsync(usuarioId, idTurno);
    }

    // Baja el contador del trabajo y decide si el trabajo sobrevive a la cancelacion.
    //
    // El contador vive en TrabajoClase. Salud no lo tiene porque una practica es siempre
    // una sesion: cancelarla es quedarse sin trabajo, sin nada que descontar.
    private async Task DescontarSesionDelTrabajoAsync(int usuarioId, int idTrabajo, string? motivo)
    {
        var trabajo = await _db.Trabajos.FirstOrDefaultAsync(t => t.Id == idTrabajo);
        if (trabajo is null)
            return;

        if (trabajo is not TrabajoClase clase)
        {
            await CancelarTrabajoAsync(usuarioId, idTrabajo, motivo);
            return;
        }

        clase.CantidadSesionesTotales -= 1;

        // No queda ninguna sesion: el trabajo se cae entero. Se delega en la maquina de
        // estados para que la cancelacion quede en el historial y el reembolso lo emita el
        // mismo camino que cualquier otra.
        if (clase.CantidadSesionesTotales <= 0)
        {
            await CancelarTrabajoAsync(usuarioId, idTrabajo, motivo);
            return;
        }

        // Cancelar las que faltaban puede dejar el paquete ya cumplido: 2 de 4 dadas y las
        // otras 2 canceladas es un paquete de 2, entero. Sin esto el trabajo quedaria vivo
        // para siempre y su pago clavado en ParcialmenteLiberado, con plata sin liberar.
        if (clase.SesionesCompletadas >= clase.CantidadSesionesTotales)
        {
            await _pagos.LiberarPagoTotalAsync(idTrabajo);
            await _trabajos.CompletarPorSesionesAsync(usuarioId, idTrabajo);
        }
    }

    private Task CancelarTrabajoAsync(int usuarioId, int idTrabajo, string? motivo) =>
        _trabajos.CancelarAsync(usuarioId, idTrabajo,
            motivo ?? "Se cancelaron todas las sesiones agendadas del trabajo.");

    // --- Helpers ------------------------------------------------------------

    // El nombre de cada parte vive en usuario, no en el perfil.
    private IQueryable<Turno> TurnosConPartes() =>
        _db.Turnos.AsNoTracking()
            .Include(t => t.Estudiante).ThenInclude(e => e.Usuario)
            .Include(t => t.Cliente).ThenInclude(c => c.Usuario);

    private static string RolDe(int usuarioId, Turno turno) =>
        turno.EstudianteId == usuarioId ? "Estudiante" : "Cliente";
}
