using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.Domain.Entities;
using Lex.Api.Domain.Enums;
using Lex.Api.Features.Pagos;
using Lex.Api.Features.Sesiones;
using Lex.Api.Features.Trabajos.Shared;
using Lex.Api.Features.Turnos;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Features.Trabajos.Salud;

public class TrabajoSaludService : ITrabajoSaludService
{
    private readonly AppDbContext _db;
    private readonly IPagoService _pagos;
    private readonly IValidadorTurnosService _validador;
    private readonly ISesionService _sesiones;

    public TrabajoSaludService(
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

    public async Task<TrabajoSaludResponse> ContratarAsync(int clienteId, ContratarTrabajoSaludRequest request)
    {
        if (!await _db.PerfilesCliente.AnyAsync(p => p.UsuarioId == clienteId))
            throw new ForbiddenException("Solo los clientes pueden contratar servicios.");

        // El servicio debe existir y ser de la vertical Salud (con supervisor + catalogo).
        var servicio = await _db.ServiciosSalud
            .Include(s => s.CatalogoServicio)
            .Include(s => s.Supervisor)
            .FirstOrDefaultAsync(s => s.Id == request.ServicioId);

        if (servicio is null)
        {
            var existe = await _db.Servicios.AnyAsync(s => s.Id == request.ServicioId);
            if (!existe)
                throw new NotFoundException($"No existe el servicio {request.ServicioId}.");
            throw new BadRequestException("Este servicio no es del área de salud.");
        }

        if (!servicio.Activo)
            throw new BadRequestException("El servicio no está disponible para contratar.");

        if (servicio.EstudianteId == clienteId)
            throw new BadRequestException("No podés contratar tu propio servicio.");

        // El paciente debe existir y pertenecer al cliente contratante.
        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.Id == request.PacienteId)
            ?? throw new NotFoundException($"No existe el paciente {request.PacienteId}.");
        if (paciente.ClienteResponsableId != clienteId)
            throw new ForbiddenException("Este paciente no te pertenece.");

        // Año minimo exigido para la carrera del estudiante en esta entrada del catalogo.
        var anioMinimo = await _db.CatalogoServicioCarreras
            .Where(cc => cc.CatalogoServicioId == servicio.CatalogoServicioId
                && _db.EstudianteCarreras.Any(ec => ec.EstudianteId == servicio.EstudianteId && ec.CarreraId == cc.CarreraId))
            .Select(cc => (int?)cc.AnioMinimo)
            .MinAsync() ?? 0;

        // El turno se valida antes de abrir la transaccion: si el horario no sirve, no hay
        // nada que revertir. Reservar el turno NO reemplaza al consentimiento: el trabajo
        // sigue sin poder pasar a EnCurso hasta que el cliente lo firme.
        var slot = DateTime.SpecifyKind(request.SlotElegido, DateTimeKind.Utc);
        var motivo = await _validador.ValidarSlotAsync(servicio.EstudianteId, slot, servicio.DuracionMinutosSesion);
        if (motivo is not null)
            throw new BadRequestException(motivo);

        var trabajo = await CrearConTurnoAsync(clienteId, servicio, paciente, anioMinimo, slot, request.NotasCliente);

        return await ObtenerAsync(clienteId, trabajo.Id);
    }

    // Trabajo + escrow + turno + sesion en una sola unidad: o entra todo, o no entra nada.
    private async Task<TrabajoSalud> CrearConTurnoAsync(
        int clienteId, ServicioSalud servicio, Paciente paciente, int anioMinimo, DateTime slot, string? notasCliente)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var ahora = DateTime.UtcNow;
            var trabajo = new TrabajoSalud
            {
                ServicioId = servicio.Id,
                ClienteId = clienteId,
                EstudianteId = servicio.EstudianteId,
                TituloSnapshot = servicio.Titulo,
                DescripcionSnapshot = servicio.Descripcion,
                PrecioAcordado = servicio.Precio,
                Estado = EstadoTrabajo.Pendiente,
                FechaCreacion = ahora,
                // Snapshots por valor (evidencia legal).
                CatalogoServicioIdSnapshot = servicio.CatalogoServicioId,
                CatalogoServicioNombreSnapshot = servicio.CatalogoServicio.Nombre,
                CatalogoServicioAnioMinimoSnapshot = anioMinimo,
                SupervisorIdSnapshot = servicio.SupervisorId,
                SupervisorNombreSnapshot = servicio.Supervisor.NombreCompleto,
                SupervisorMatriculaSnapshot = servicio.Supervisor.Matricula,
                PacienteId = paciente.Id,
                ModalidadSaludSnapshot = servicio.Modalidad,
                DuracionMinutosSesionSnapshot = servicio.DuracionMinutosSesion,
                ConsentimientoId = null // se llena cuando el cliente firma
            };
            trabajo.Historiales.Add(new TrabajoHistorial
            {
                EstadoAnterior = null,
                EstadoNuevo = EstadoTrabajo.Pendiente,
                Fecha = ahora,
                UsuarioId = clienteId
            });

            _db.TrabajosSalud.Add(trabajo);
            // Contratar retiene la plata: el trabajo nace junto con su escrow.
            _pagos.CrearPagoParaTrabajo(trabajo);

            // Una practica = un turno: el mismo agendador que Clase, con una lista de uno.
            AgendadorDeTurnos.Agendar(_db, trabajo, servicio.EstudianteId, clienteId,
                new[] { slot }, servicio.DuracionMinutosSesion, notasCliente, ahora);

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

    public async Task<TrabajoSaludResponse> FirmarConsentimientoAsync(int usuarioId, int idTrabajo, string? ip)
    {
        var trabajo = await _db.TrabajosSalud
            .Include(t => t.Paciente)
            .Include(t => t.Estudiante).ThenInclude(e => e.Usuario)
            .FirstOrDefaultAsync(t => t.Id == idTrabajo)
            ?? throw new NotFoundException($"No existe el trabajo de salud {idTrabajo}.");

        // El consentimiento lo firma el cliente responsable del trabajo.
        if (trabajo.ClienteId != usuarioId)
            throw new ForbiddenException("Solo el cliente del trabajo puede firmar el consentimiento.");

        if (trabajo.ConsentimientoId is not null)
            throw new BadRequestException("El consentimiento de este trabajo ya fue firmado.");

        var ahora = DateTime.UtcNow;
        var texto = ConsentimientoTemplate.Generar(trabajo, trabajo.Paciente, trabajo.Estudiante.Usuario.NombreCompleto);

        var consentimiento = new Consentimiento
        {
            TrabajoSaludId = trabajo.Id,
            TextoCompleto = texto,
            AceptadoPorUsuarioId = usuarioId,
            FechaAceptacion = ahora,
            IpAceptacion = string.IsNullOrWhiteSpace(ip) ? null : ip
        };
        _db.Consentimientos.Add(consentimiento);
        await _db.SaveChangesAsync();

        trabajo.ConsentimientoId = consentimiento.Id;
        await _db.SaveChangesAsync();

        return await ObtenerAsync(usuarioId, idTrabajo);
    }

    public async Task<TrabajoSaludResponse> ObtenerAsync(int usuarioId, int idTrabajo)
    {
        var t = await _db.TrabajosSalud.AsNoTracking()
            .Include(x => x.Cliente).ThenInclude(c => c.Usuario)
            .Include(x => x.Estudiante).ThenInclude(e => e.Usuario)
            .Include(x => x.Paciente)
            .Include(x => x.Consentimiento)
            .FirstOrDefaultAsync(x => x.Id == idTrabajo)
            ?? throw new NotFoundException($"No existe el trabajo de salud {idTrabajo}.");

        if (t.EstudianteId != usuarioId && t.ClienteId != usuarioId)
            throw new ForbiddenException("No participás en este trabajo.");

        var response = Map(t);
        response.Sesiones = (await _sesiones.ListarDeTrabajoAsync(usuarioId, idTrabajo)).ToList();
        return response;
    }

    public static TrabajoSaludResponse Map(TrabajoSalud t)
    {
        var r = new TrabajoSaludResponse
        {
            CatalogoServicioIdSnapshot = t.CatalogoServicioIdSnapshot,
            CatalogoServicioNombreSnapshot = t.CatalogoServicioNombreSnapshot,
            CatalogoServicioAnioMinimoSnapshot = t.CatalogoServicioAnioMinimoSnapshot,
            SupervisorIdSnapshot = t.SupervisorIdSnapshot,
            SupervisorNombreSnapshot = t.SupervisorNombreSnapshot,
            SupervisorMatriculaSnapshot = t.SupervisorMatriculaSnapshot,
            PacienteId = t.PacienteId,
            PacienteNombre = t.Paciente.NombreCompleto,
            ModalidadSaludSnapshot = t.ModalidadSaludSnapshot,
            DuracionMinutosSesionSnapshot = t.DuracionMinutosSesionSnapshot,
            ConsentimientoId = t.ConsentimientoId,
            ConsentimientoFirmado = t.ConsentimientoId is not null,
            Consentimiento = t.Consentimiento is null ? null : new ConsentimientoResponse
            {
                Id = t.Consentimiento.Id,
                TextoCompleto = t.Consentimiento.TextoCompleto,
                AceptadoPorUsuarioId = t.Consentimiento.AceptadoPorUsuarioId,
                FechaAceptacion = t.Consentimiento.FechaAceptacion,
                IpAceptacion = t.Consentimiento.IpAceptacion
            }
        };
        return r.LlenarBase(t);
    }
}
