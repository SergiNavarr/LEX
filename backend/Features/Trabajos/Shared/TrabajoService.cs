using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.Domain.Entities;
using Lex.Api.Domain.Enums;
using Lex.Api.Features.Pagos;
using Lex.Api.Features.Trabajos.Clase;
using Lex.Api.Features.Trabajos.ProyectoCerrado;
using Lex.Api.Features.Trabajos.Salud;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Features.Trabajos.Shared;

// Rol de un usuario dentro de un trabajo concreto (no es el rol global del sistema).
public enum ParteTrabajo
{
    Estudiante,
    Cliente
}

// Transiciones de estado (compartidas por las 3 verticales) + consultas unificadas.
// La vertical concreta la materializa EF al consultar el DbSet base (TPT).
public class TrabajoService : ITrabajoService
{
    private readonly AppDbContext _db;
    private readonly IPagoService _pagos;

    public TrabajoService(AppDbContext db, IPagoService pagos)
    {
        _db = db;
        _pagos = pagos;
    }

    // --- Transiciones --------------------------------------------------------
    public Task<TrabajoResponse> AceptarAsync(int usuarioId, int idTrabajo) =>
        TransicionarAsync(usuarioId, idTrabajo, EstadoTrabajo.Aceptado);

    public Task<TrabajoResponse> IniciarAsync(int usuarioId, int idTrabajo) =>
        TransicionarAsync(usuarioId, idTrabajo, EstadoTrabajo.EnCurso);

    public Task<TrabajoResponse> EntregarAsync(int usuarioId, int idTrabajo) =>
        TransicionarAsync(usuarioId, idTrabajo, EstadoTrabajo.Entregado);

    public Task<TrabajoResponse> CompletarAsync(int usuarioId, int idTrabajo) =>
        TransicionarAsync(usuarioId, idTrabajo, EstadoTrabajo.Completado);

    public Task<TrabajoResponse> CancelarAsync(int usuarioId, int idTrabajo, string? motivo) =>
        TransicionarAsync(usuarioId, idTrabajo, EstadoTrabajo.Cancelado, motivo);

    public Task<TrabajoResponse> DisputarAsync(int usuarioId, int idTrabajo, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BadRequestException("Debe indicar el motivo de la disputa.");
        return TransicionarAsync(usuarioId, idTrabajo, EstadoTrabajo.Disputa, motivo);
    }

    // Cierra un trabajo de Clase/Salud cuyo escrow ya se fue liberando sesion a sesion.
    // No pasa por la maquina de estados porque no hay transicion EnCurso -> Completado
    // declarada: en estos verticales el completado no lo pide nadie, lo dispara la ultima
    // sesion marcada. Y no toca el pago: eso ya lo hizo cada sesion.
    public async Task<TrabajoResponse> CompletarPorSesionesAsync(int usuarioId, int idTrabajo)
    {
        var trabajo = await _db.Trabajos.FirstOrDefaultAsync(t => t.Id == idTrabajo)
            ?? throw new NotFoundException($"No existe el trabajo {idTrabajo}.");

        await AplicarTransicionAsync(trabajo, EstadoTrabajo.Completado, usuarioId, pagoYaResuelto: true);
        await _db.SaveChangesAsync();
        return await MapBaseAsync(idTrabajo);
    }

    private async Task<TrabajoResponse> TransicionarAsync(int usuarioId, int idTrabajo, EstadoTrabajo destino, string? motivo = null)
    {
        var trabajo = await _db.Trabajos.FirstOrDefaultAsync(t => t.Id == idTrabajo)
            ?? throw new NotFoundException($"No existe el trabajo {idTrabajo}.");

        var parte = ParteDe(usuarioId, trabajo)
            ?? throw new ForbiddenException("No participás en este trabajo.");

        // La maquina de estados es la unica fuente de verdad: si el par (actual->destino)
        // no esta declarado, la transicion no existe y se rechaza con 400.
        var permitidas = PartesAutorizadas(trabajo.Estado, destino)
            ?? throw new BadRequestException($"No se puede pasar de {trabajo.Estado} a {destino}.");

        if (!permitidas.Contains(parte))
            throw new ForbiddenException(
                $"La transición {trabajo.Estado} → {destino} solo puede hacerla {DescribirPartes(permitidas)} del trabajo.");

        // Salud: no se puede pasar a EnCurso sin consentimiento firmado.
        if (destino == EstadoTrabajo.EnCurso && trabajo is TrabajoSalud ts && ts.ConsentimientoId is null)
            throw new BadRequestException("Consentimiento pendiente: el cliente debe firmarlo antes de iniciar.");

        // Clase y Salud no se completan a mano: el completado lo dispara la ultima sesion
        // marcada como realizada, que es tambien la que libera la ultima fraccion del pago.
        // Completar por afuera dejaria el escrow y las sesiones diciendo cosas distintas.
        // Se exceptua el cierre de una disputa, que es la via de resolucion del conflicto.
        if (destino == EstadoTrabajo.Completado
            && trabajo is TrabajoClase or TrabajoSalud
            && trabajo.Estado != EstadoTrabajo.Disputa)
            throw new BadRequestException(
                "Este trabajo se completa automáticamente al marcar la última sesión como realizada " +
                "(POST /api/sesiones/{id}/realizar).");

        await AplicarTransicionAsync(trabajo, destino, usuarioId, motivo);

        await _db.SaveChangesAsync();
        return await MapBaseAsync(idTrabajo);
    }

    // Aplica una transicion ya autorizada: mueve el estado, deja la traza en el historial y
    // resuelve el escrow. No valida ni guarda; eso es de quien llama.
    //
    // pagoYaResuelto corta el efecto sobre el escrow para el unico caso en que la plata ya
    // se movio antes de la transicion: el completado por sesiones, donde cada sesion fue
    // liberando su fraccion.
    private async Task AplicarTransicionAsync(
        Trabajo trabajo, EstadoTrabajo destino, int usuarioId, string? motivo = null, bool pagoYaResuelto = false)
    {
        var anterior = trabajo.Estado;
        var ahora = DateTime.UtcNow;

        trabajo.Estado = destino;
        if (destino == EstadoTrabajo.EnCurso)
            trabajo.FechaInicio = ahora;
        if (destino is EstadoTrabajo.Completado or EstadoTrabajo.Cancelado)
            trabajo.FechaFin = ahora;

        // Efecto sobre el escrow. Va antes del SaveChanges del llamador a proposito: el
        // service de pagos solo deja los cambios en el DbContext, asi el nuevo estado del
        // trabajo y el del pago se commitean juntos o no se commitea ninguno.
        if (!pagoYaResuelto)
        {
            switch (destino)
            {
                case EstadoTrabajo.Completado:
                    await _pagos.LiberarPagoTotalAsync(trabajo.Id);
                    break;
                case EstadoTrabajo.Cancelado:
                    await _pagos.ReembolsarPagoAsync(trabajo.Id, motivo ?? "Cancelación");
                    break;
                case EstadoTrabajo.Disputa:
                    await _pagos.MarcarEnDisputaAsync(trabajo.Id);
                    break;
            }
        }

        trabajo.Historiales.Add(new TrabajoHistorial
        {
            EstadoAnterior = anterior,
            EstadoNuevo = destino,
            Fecha = ahora,
            UsuarioId = usuarioId
        });

        // Al completar, sube el contador de trabajos del estudiante.
        if (destino == EstadoTrabajo.Completado)
        {
            var perfil = await _db.PerfilesEstudiante.FirstOrDefaultAsync(p => p.UsuarioId == trabajo.EstudianteId);
            if (perfil is not null)
                perfil.CantidadTrabajos += 1;
        }
    }

    // --- Maquina de estados --------------------------------------------------
    // Que rol del trabajo puede provocar cada transicion. Si el par (actual->nuevo)
    // no esta aca, la transicion no existe (salvo el escape de cancelar/disputar).
    private static ParteTrabajo[]? PartesAutorizadas(EstadoTrabajo desde, EstadoTrabajo hasta) =>
        (desde, hasta) switch
        {
            (EstadoTrabajo.Pendiente, EstadoTrabajo.Aceptado) => new[] { ParteTrabajo.Estudiante },
            (EstadoTrabajo.Pendiente, EstadoTrabajo.Cancelado) => new[] { ParteTrabajo.Estudiante, ParteTrabajo.Cliente },
            (EstadoTrabajo.Aceptado, EstadoTrabajo.EnCurso) => new[] { ParteTrabajo.Estudiante },
            (EstadoTrabajo.Aceptado, EstadoTrabajo.Cancelado) => new[] { ParteTrabajo.Estudiante, ParteTrabajo.Cliente },
            (EstadoTrabajo.EnCurso, EstadoTrabajo.Entregado) => new[] { ParteTrabajo.Estudiante },
            (EstadoTrabajo.EnCurso, EstadoTrabajo.Disputa) => new[] { ParteTrabajo.Estudiante, ParteTrabajo.Cliente },
            (EstadoTrabajo.EnCurso, EstadoTrabajo.Cancelado) => new[] { ParteTrabajo.Estudiante, ParteTrabajo.Cliente },
            (EstadoTrabajo.Entregado, EstadoTrabajo.Completado) => new[] { ParteTrabajo.Cliente },
            (EstadoTrabajo.Entregado, EstadoTrabajo.Disputa) => new[] { ParteTrabajo.Estudiante, ParteTrabajo.Cliente },
            (EstadoTrabajo.Disputa, EstadoTrabajo.Completado) => new[] { ParteTrabajo.Cliente },
            (EstadoTrabajo.Disputa, EstadoTrabajo.Cancelado) => new[] { ParteTrabajo.Estudiante, ParteTrabajo.Cliente },
            _ => null
        };

    private static ParteTrabajo? ParteDe(int usuarioId, Trabajo t) =>
        usuarioId == t.EstudianteId ? ParteTrabajo.Estudiante
        : usuarioId == t.ClienteId ? ParteTrabajo.Cliente
        : null;

    private static string DescribirPartes(IEnumerable<ParteTrabajo> partes) =>
        string.Join(" o ", partes.Select(p => p == ParteTrabajo.Estudiante ? "el estudiante" : "el cliente"));

    // --- Consultas unificadas ------------------------------------------------
    public async Task<IReadOnlyList<TrabajoResponse>> ListarAsync(
        TipoServicio? tipo, EstadoTrabajo? estado, int? clienteId, int? estudianteId)
    {
        var query = _db.Trabajos.AsNoTracking()
            .Include(t => t.Cliente).ThenInclude(c => c.Usuario)
            .Include(t => t.Estudiante).ThenInclude(e => e.Usuario)
            .AsQueryable();

        if (clienteId is int cli)
            query = query.Where(t => t.ClienteId == cli);
        if (estudianteId is int est)
            query = query.Where(t => t.EstudianteId == est);
        if (estado is EstadoTrabajo e)
            query = query.Where(t => t.Estado == e);

        query = tipo switch
        {
            TipoServicio.ProyectoCerrado => query.Where(t => t is TrabajoProyectoCerrado),
            TipoServicio.Clase => query.Where(t => t is TrabajoClase),
            TipoServicio.Salud => query.Where(t => t is TrabajoSalud),
            _ => query
        };

        var trabajos = await query.OrderByDescending(t => t.FechaCreacion).ToListAsync();
        return trabajos.Select(t => new TrabajoResponse().LlenarBase(t)).ToList();
    }

    public async Task<IReadOnlyList<TrabajoResponse>> ListarMiosAsync(int usuarioId)
    {
        var trabajos = await _db.Trabajos.AsNoTracking()
            .Include(t => t.Cliente).ThenInclude(c => c.Usuario)
            .Include(t => t.Estudiante).ThenInclude(e => e.Usuario)
            .Where(t => t.EstudianteId == usuarioId || t.ClienteId == usuarioId)
            .OrderByDescending(t => t.FechaCreacion)
            .ToListAsync();

        return trabajos.Select(t => new TrabajoResponse().LlenarBase(t)).ToList();
    }

    public async Task<TrabajoDetalleResponse> ObtenerDetalleAsync(int usuarioId, int idTrabajo)
    {
        var trabajo = await _db.Trabajos.AsNoTracking()
            .Include(t => t.Cliente).ThenInclude(c => c.Usuario)
            .Include(t => t.Estudiante).ThenInclude(e => e.Usuario)
            .FirstOrDefaultAsync(t => t.Id == idTrabajo)
            ?? throw new NotFoundException($"No existe el trabajo {idTrabajo}.");

        if (trabajo.EstudianteId != usuarioId && trabajo.ClienteId != usuarioId)
            throw new ForbiddenException("No participás en este trabajo.");

        var detalle = await ObtenerDetalleVerticalAsync(trabajo);

        var response = new TrabajoDetalleResponse { Detalle = detalle };
        return response.LlenarBase(trabajo);
    }

    // Bloque polimorfico: los campos propios de la vertical concreta.
    private async Task<object?> ObtenerDetalleVerticalAsync(Trabajo trabajo) => trabajo switch
    {
        TrabajoProyectoCerrado => await _db.TrabajosProyectoCerrado.AsNoTracking()
            .Where(t => t.Id == trabajo.Id)
            .Select(t => new TrabajoProyectoCerradoDetalle
            {
                PlazoEntregaFecha = t.PlazoEntregaFecha,
                RevisionesMaximas = t.RevisionesMaximas,
                RevisionesUsadas = t.RevisionesUsadas,
                FormatoEntregaSnapshot = t.FormatoEntregaSnapshot
            })
            .FirstOrDefaultAsync(),

        TrabajoClase => await _db.TrabajosClase.AsNoTracking()
            .Where(t => t.Id == trabajo.Id)
            .Select(t => new TrabajoClaseDetalle
            {
                MateriaSnapshot = t.MateriaSnapshot,
                NivelSnapshot = t.NivelSnapshot,
                ModalidadSnapshot = t.ModalidadSnapshot,
                DuracionMinutosSesionSnapshot = t.DuracionMinutosSesionSnapshot,
                EsPaqueteSnapshot = t.EsPaqueteSnapshot,
                CantidadSesionesTotales = t.CantidadSesionesTotales,
                SesionesCompletadas = t.SesionesCompletadas
            })
            .FirstOrDefaultAsync(),

        TrabajoSalud => await _db.TrabajosSalud.AsNoTracking()
            .Where(t => t.Id == trabajo.Id)
            .Select(t => new TrabajoSaludDetalle
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
                ConsentimientoFirmado = t.ConsentimientoId != null,
                Consentimiento = t.Consentimiento == null ? null : new ConsentimientoResponse
                {
                    Id = t.Consentimiento.Id,
                    TextoCompleto = t.Consentimiento.TextoCompleto,
                    AceptadoPorUsuarioId = t.Consentimiento.AceptadoPorUsuarioId,
                    FechaAceptacion = t.Consentimiento.FechaAceptacion,
                    IpAceptacion = t.Consentimiento.IpAceptacion
                }
            })
            .FirstOrDefaultAsync(),

        _ => null
    };

    public async Task<IReadOnlyList<TrabajoHistorialResponse>> ListarHistorialAsync(int usuarioId, int idTrabajo)
    {
        var trabajo = await _db.Trabajos.AsNoTracking()
            .Where(t => t.Id == idTrabajo)
            .Select(t => new { t.EstudianteId, t.ClienteId })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"No existe el trabajo {idTrabajo}.");

        if (trabajo.EstudianteId != usuarioId && trabajo.ClienteId != usuarioId)
            throw new ForbiddenException("No participás en este trabajo.");

        return await _db.TrabajoHistoriales.AsNoTracking()
            .Where(h => h.TrabajoId == idTrabajo)
            .OrderBy(h => h.Fecha)
            .Select(h => new TrabajoHistorialResponse
            {
                Id = h.Id,
                EstadoAnterior = h.EstadoAnterior,
                EstadoNuevo = h.EstadoNuevo,
                Fecha = h.Fecha,
                UsuarioId = h.UsuarioId
            })
            .ToListAsync();
    }

    private async Task<TrabajoResponse> MapBaseAsync(int idTrabajo)
    {
        var t = await _db.Trabajos.AsNoTracking()
            .Include(x => x.Cliente).ThenInclude(c => c.Usuario)
            .Include(x => x.Estudiante).ThenInclude(e => e.Usuario)
            .FirstAsync(x => x.Id == idTrabajo);
        return new TrabajoResponse().LlenarBase(t);
    }
}
