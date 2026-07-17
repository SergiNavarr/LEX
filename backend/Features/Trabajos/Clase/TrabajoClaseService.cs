using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.Domain.Entities;
using Lex.Api.Domain.Enums;
using Lex.Api.Features.Pagos;
using Lex.Api.Features.Sesiones;
using Lex.Api.Features.Trabajos.Shared;
using Lex.Api.Features.Turnos;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Features.Trabajos.Clase;

public class TrabajoClaseService : ITrabajoClaseService
{
    private readonly AppDbContext _db;
    private readonly IPagoService _pagos;
    private readonly IValidadorTurnosService _validador;
    private readonly ISesionService _sesiones;

    public TrabajoClaseService(
        AppDbContext db,
        IPagoService pagos,
        IValidadorTurnosService validador,
        ISesionService sesiones)
    {
        _db = db;
        _pagos = pagos;
        _validador = validador;
        _sesiones = sesiones;
    }

    public async Task<TrabajoClaseResponse> ContratarAsync(int clienteId, ContratarTrabajoClaseRequest request)
    {
        if (!await _db.PerfilesCliente.AnyAsync(p => p.UsuarioId == clienteId))
            throw new ForbiddenException("Solo los clientes pueden contratar servicios.");

        var servicioBase = await _db.Servicios.FirstOrDefaultAsync(s => s.Id == request.ServicioId)
            ?? throw new NotFoundException($"No existe el servicio {request.ServicioId}.");

        if (servicioBase is not ServicioClase servicio)
            throw new BadRequestException("Este servicio no es de clases.");

        if (!servicio.Activo)
            throw new BadRequestException("El servicio no está disponible para contratar.");

        if (servicio.EstudianteId == clienteId)
            throw new BadRequestException("No podés contratar tu propio servicio.");

        var cantidadSesiones = ResolverCantidadSesiones(servicio);

        // Los slots se validan ANTES de abrir la transaccion: si la agenda no cierra, no
        // hay nada que revertir. Adentro solo queda la escritura.
        var slots = await ValidarSlotsAsync(servicio.EstudianteId, request.SlotsElegidos,
            cantidadSesiones, servicio.DuracionMinutosSesion);

        var trabajo = await CrearConTurnosAsync(clienteId, servicio, cantidadSesiones, slots, request.NotasCliente);

        return await ObtenerAsync(clienteId, trabajo.Id);
    }

    // Trabajo + escrow + turnos + sesiones son una sola unidad: un trabajo sin sus turnos
    // deja al cliente pagando por una agenda vacia, y unos turnos sin trabajo bloquean la
    // agenda del estudiante sin que nadie los haya pagado. O entra todo, o no entra nada.
    private async Task<TrabajoClase> CrearConTurnosAsync(
        int clienteId, ServicioClase servicio, int cantidadSesiones, List<DateTime> slots, string? notasCliente)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var ahora = DateTime.UtcNow;
            var trabajo = new TrabajoClase
            {
                ServicioId = servicio.Id,
                ClienteId = clienteId,
                EstudianteId = servicio.EstudianteId,
                TituloSnapshot = servicio.Titulo,
                DescripcionSnapshot = servicio.Descripcion,
                PrecioAcordado = servicio.Precio,
                Estado = EstadoTrabajo.Pendiente,
                FechaCreacion = ahora,
                MateriaSnapshot = servicio.Materia,
                NivelSnapshot = servicio.Nivel,
                ModalidadSnapshot = servicio.Modalidad,
                DuracionMinutosSesionSnapshot = servicio.DuracionMinutosSesion,
                EsPaqueteSnapshot = servicio.EsPaquete,
                CantidadSesionesTotales = cantidadSesiones,
                SesionesCompletadas = 0
            };
            trabajo.Historiales.Add(new TrabajoHistorial
            {
                EstadoAnterior = null,
                EstadoNuevo = EstadoTrabajo.Pendiente,
                Fecha = ahora,
                UsuarioId = clienteId
            });

            _db.TrabajosClase.Add(trabajo);
            // Contratar retiene la plata: el trabajo nace junto con su escrow.
            _pagos.CrearPagoParaTrabajo(trabajo);

            AgendadorDeTurnos.Agendar(_db, trabajo, servicio.EstudianteId, clienteId, slots,
                servicio.DuracionMinutosSesion, notasCliente, ahora);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return trabajo;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // Valida la tanda completa contra la agenda del estudiante y contra si misma.
    // Devuelve los slots normalizados a UTC, listos para persistir.
    private async Task<List<DateTime>> ValidarSlotsAsync(
        int estudianteId, List<DateTime> elegidos, int cantidadSesiones, int duracionMinutos)
    {
        if (elegidos.Count != cantidadSesiones)
            throw new BadRequestException(
                $"Este servicio requiere agendar {cantidadSesiones} " +
                $"{(cantidadSesiones == 1 ? "sesión" : "sesiones")}, pero elegiste {elegidos.Count} " +
                $"{(elegidos.Count == 1 ? "horario" : "horarios")}. Hay que agendar el paquete completo.");

        var slots = elegidos.Select(s => DateTime.SpecifyKind(s, DateTimeKind.Utc)).ToList();

        var choqueEntreSlots = _validador.ValidarSlotsNoSolapan(slots, duracionMinutos);
        if (choqueEntreSlots is not null)
            throw new BadRequestException(choqueEntreSlots);

        foreach (var slot in slots)
        {
            var motivo = await _validador.ValidarSlotAsync(estudianteId, slot, duracionMinutos);
            if (motivo is not null)
                throw new BadRequestException(motivo);
        }

        return slots;
    }

    // La cantidad la fija el servicio: N si es paquete, 1 si es una clase suelta. El cliente
    // no la elige, solo elige los horarios.
    private static int ResolverCantidadSesiones(ServicioClase servicio)
    {
        if (!servicio.EsPaquete)
            return 1;

        var totalPaquete = servicio.CantidadSesionesPaquete ?? 0;
        if (totalPaquete <= 0)
            throw new BadRequestException("El servicio es un paquete pero no define la cantidad de sesiones.");

        return totalPaquete;
    }

    public async Task<TrabajoClaseResponse> ObtenerAsync(int usuarioId, int idTrabajo)
    {
        var t = await _db.TrabajosClase.AsNoTracking()
            .Include(x => x.Cliente).ThenInclude(c => c.Usuario)
            .Include(x => x.Estudiante).ThenInclude(e => e.Usuario)
            .FirstOrDefaultAsync(x => x.Id == idTrabajo)
            ?? throw new NotFoundException($"No existe el trabajo de clase {idTrabajo}.");

        if (t.EstudianteId != usuarioId && t.ClienteId != usuarioId)
            throw new ForbiddenException("No participás en este trabajo.");

        var response = Map(t);
        response.Sesiones = (await _sesiones.ListarDeTrabajoAsync(usuarioId, idTrabajo)).ToList();
        return response;
    }

    public static TrabajoClaseResponse Map(TrabajoClase t)
    {
        var r = new TrabajoClaseResponse
        {
            MateriaSnapshot = t.MateriaSnapshot,
            NivelSnapshot = t.NivelSnapshot,
            ModalidadSnapshot = t.ModalidadSnapshot,
            DuracionMinutosSesionSnapshot = t.DuracionMinutosSesionSnapshot,
            EsPaqueteSnapshot = t.EsPaqueteSnapshot,
            CantidadSesionesTotales = t.CantidadSesionesTotales,
            SesionesCompletadas = t.SesionesCompletadas
        };
        return r.LlenarBase(t);
    }
}
