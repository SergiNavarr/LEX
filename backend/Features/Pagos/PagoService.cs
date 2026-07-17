using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.Domain.Entities;
using Lex.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lex.Api.Features.Pagos;

public class PagoService : IPagoService
{
    private readonly AppDbContext _db;
    private readonly LexOptions _opciones;

    // Estados desde los que todavia se puede liberar plata al estudiante.
    // EnDisputa entra porque la maquina de estados habilita Disputa -> Completado, y esa
    // transicion tiene que poder cerrar el escrow. ParcialmenteLiberado entra desde Hito 2
    // Parte 3: un paquete a medio liberar sigue teniendo saldo pendiente.
    private static readonly EstadoPago[] EstadosLiberables =
        { EstadoPago.Retenido, EstadoPago.EnDisputa, EstadoPago.ParcialmenteLiberado };

    // Reembolsar devuelve el total al cliente, asi que exige que no se haya pagado nada
    // todavia: ParcialmenteLiberado queda afuera a proposito (ver README_PAGOS.md).
    private static readonly EstadoPago[] EstadosReembolsables = { EstadoPago.Retenido, EstadoPago.EnDisputa };

    public PagoService(AppDbContext db, IOptions<LexOptions> opciones)
    {
        _db = db;
        _opciones = opciones.Value;
    }

    // --- Negocio del escrow -------------------------------------------------

    // Crea el escrow con los snapshots contractuales (monto y % de comision vigentes al
    // contratar) y su primer asiento. El trabajo se referencia por navegacion y no por
    // TrabajoId porque puede venir sin Id todavia: al contratar, el trabajo y el pago se
    // insertan en el mismo SaveChanges y EF resuelve la FK.
    public Pago CrearPagoParaTrabajo(Trabajo trabajo)
    {
        var porcentaje = _opciones.PorcentajeComision;
        var comision = Math.Round(trabajo.PrecioAcordado * porcentaje / 100m, 2, MidpointRounding.AwayFromZero);
        var ahora = DateTime.UtcNow;

        var pago = new Pago
        {
            Trabajo = trabajo,
            MontoTotal = trabajo.PrecioAcordado,
            PorcentajeComisionLex = porcentaje,
            MontoComisionCalculada = comision,
            MontoAEstudiante = trabajo.PrecioAcordado - comision,
            Estado = EstadoPago.Retenido,
            FechaCreacion = ahora
        };
        pago.Movimientos.Add(new MovimientoPago
        {
            Tipo = TipoMovimientoPago.Retencion,
            Monto = trabajo.PrecioAcordado,
            Descripcion = "Retención del pago en escrow al contratar el trabajo.",
            FechaMovimiento = ahora
        });

        _db.Pagos.Add(pago);
        return pago;
    }

    // Cierre feliz: el estudiante cobra lo que le falta y LEX toma su parte. Exige escrow:
    // liberar plata que nunca se retuvo es una inconsistencia y tiene que explotar.
    //
    // Libera el REMANENTE, no el total: si el trabajo venia liberando por sesion (Clase o
    // Salud) puede haber asientos previos, y esta llamada solo cierra lo que quedaba. En un
    // trabajo sin sesiones el remanente es el total y el comportamiento no cambia.
    public async Task LiberarPagoTotalAsync(int idTrabajo)
    {
        var pago = await BuscarLiberableAsync(idTrabajo);
        var (aEstudiante, comision) = await RemanenteAsync(pago);

        if (aEstudiante > 0 || comision > 0)
            AgregarAsientosDeLiberacion(pago, aEstudiante, comision, "por trabajo completado", DateTime.UtcNow);

        MarcarLiberado(pago, DateTime.UtcNow);
    }

    // Libera la parte que le toca a una sesion. Se llama una vez por sesion marcada como
    // Realizada o NoAsistio (Hito 2 Parte 3): NoAsistio libera igual porque el estudiante
    // reservo el horario y puso su tiempo.
    //
    // No llama a SaveChanges, como el resto del negocio del escrow: el llamador cierra la
    // unidad de trabajo junto con el estado de la sesion y del turno.
    public async Task LiberarFraccionPagoPorSesionAsync(int idTrabajo, int cantidadSesionesTotales, bool esUltimaSesion)
    {
        if (cantidadSesionesTotales <= 0)
            throw new InvalidOperationException(
                $"El trabajo {idTrabajo} no tiene sesiones totales: no hay entre cuántas dividir el pago.");

        var pago = await BuscarLiberableAsync(idTrabajo);
        var ahora = DateTime.UtcNow;

        decimal aEstudiante, comision;

        if (esUltimaSesion)
        {
            // La ultima sesion no divide: paga lo que falte. Asi la suma de las fracciones
            // da exactamente el monto contratado aunque el redondeo de las anteriores haya
            // dejado centavos sueltos (ej. 3 sesiones sobre $100 -> 33.33 + 33.33 + 33.34).
            (aEstudiante, comision) = await RemanenteAsync(pago);
        }
        else
        {
            aEstudiante = Redondear(pago.MontoAEstudiante / cantidadSesionesTotales);
            comision = Redondear(pago.MontoComisionCalculada / cantidadSesionesTotales);
        }

        if (aEstudiante > 0 || comision > 0)
            AgregarAsientosDeLiberacion(pago, aEstudiante, comision, "por sesión realizada", ahora);

        if (esUltimaSesion)
            MarcarLiberado(pago, ahora);
        else
            pago.Estado = EstadoPago.ParcialmenteLiberado;
    }

    // Lo que todavia no se le pago al estudiante ni cobro LEX, segun el libro. Se calcula
    // desde los asientos y no desde un contador: el libro es la fuente de verdad.
    private async Task<(decimal AEstudiante, decimal Comision)> RemanenteAsync(Pago pago)
    {
        var liberados = await _db.MovimientosPago
            .Where(m => m.PagoId == pago.Id
                        && (m.Tipo == TipoMovimientoPago.LiberacionEstudiante || m.Tipo == TipoMovimientoPago.ComisionLex))
            .Select(m => new { m.Tipo, m.Monto })
            .ToListAsync();

        // Los asientos de esta misma unidad de trabajo todavia no estan en la DB, asi que
        // se suman aparte los que ya estan en el DbContext sin guardar.
        var enMemoria = _db.ChangeTracker.Entries<MovimientoPago>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .Where(m => (m.PagoId == pago.Id || m.Pago == pago)
                        && (m.Tipo == TipoMovimientoPago.LiberacionEstudiante || m.Tipo == TipoMovimientoPago.ComisionLex))
            .Select(m => new { m.Tipo, m.Monto });

        var todos = liberados.Concat(enMemoria).ToList();

        return (
            pago.MontoAEstudiante - todos.Where(m => m.Tipo == TipoMovimientoPago.LiberacionEstudiante).Sum(m => m.Monto),
            pago.MontoComisionCalculada - todos.Where(m => m.Tipo == TipoMovimientoPago.ComisionLex).Sum(m => m.Monto));
    }

    // Los dos asientos de una liberacion viajan siempre juntos: lo que cobra el estudiante
    // y lo que se queda LEX salen del mismo movimiento de plata.
    private void AgregarAsientosDeLiberacion(Pago pago, decimal aEstudiante, decimal comision, string concepto, DateTime ahora)
    {
        _db.MovimientosPago.Add(new MovimientoPago
        {
            PagoId = pago.Id,
            Tipo = TipoMovimientoPago.LiberacionEstudiante,
            Monto = aEstudiante,
            Descripcion = $"Liberación al estudiante {concepto}.",
            FechaMovimiento = ahora
        });
        _db.MovimientosPago.Add(new MovimientoPago
        {
            PagoId = pago.Id,
            Tipo = TipoMovimientoPago.ComisionLex,
            Monto = comision,
            Descripcion = $"Comisión LEX {pago.PorcentajeComisionLex:0.##}% {concepto}.",
            FechaMovimiento = ahora
        });
    }

    private static void MarcarLiberado(Pago pago, DateTime ahora)
    {
        pago.Estado = EstadoPago.Liberado;
        pago.FechaLiberacion = ahora;
    }

    private async Task<Pago> BuscarLiberableAsync(int idTrabajo)
    {
        var pago = await _db.Pagos.FirstOrDefaultAsync(p => p.TrabajoId == idTrabajo)
            ?? throw new InvalidOperationException(
                $"El trabajo {idTrabajo} no tiene un pago asociado: no se puede liberar el escrow.");

        if (!EstadosLiberables.Contains(pago.Estado))
            throw new InvalidOperationException($"No se puede liberar un pago en estado {pago.Estado}.");

        return pago;
    }

    private static decimal Redondear(decimal monto) => Math.Round(monto, 2, MidpointRounding.AwayFromZero);

    // Devuelve el total al cliente. A diferencia de liberar, tolera que no haya escrow:
    // un trabajo anterior a esta integracion se tiene que poder cancelar igual.
    public async Task ReembolsarPagoAsync(int idTrabajo, string motivo)
    {
        var pago = await _db.Pagos.FirstOrDefaultAsync(p => p.TrabajoId == idTrabajo);
        if (pago is null)
            return;

        if (!EstadosReembolsables.Contains(pago.Estado))
            throw new InvalidOperationException($"No se puede reembolsar un pago en estado {pago.Estado}.");

        _db.MovimientosPago.Add(new MovimientoPago
        {
            PagoId = pago.Id,
            Tipo = TipoMovimientoPago.Reembolso,
            Monto = pago.MontoTotal,
            Descripcion = $"Reembolso al cliente por cancelación del trabajo: {motivo}",
            FechaMovimiento = DateTime.UtcNow
        });

        pago.Estado = EstadoPago.Reembolsado;
    }

    // Congela el escrow mientras dura el conflicto. No genera asiento: la plata no se
    // mueve, solo deja de poder resolverse hasta que la disputa se cierre.
    //
    // Un paquete a medio liberar tambien se puede disputar: lo que se congela es el saldo
    // que queda: las fracciones ya liberadas no vuelven por esta via (harian falta asientos
    // de Ajuste, pendientes de la interfaz de admin).
    public async Task MarcarEnDisputaAsync(int idTrabajo)
    {
        var pago = await _db.Pagos.FirstOrDefaultAsync(p => p.TrabajoId == idTrabajo);
        if (pago is null)
            return;

        if (pago.Estado is not (EstadoPago.Retenido or EstadoPago.ParcialmenteLiberado))
            throw new InvalidOperationException($"No se puede poner en disputa un pago en estado {pago.Estado}.");

        pago.Estado = EstadoPago.EnDisputa;
    }

    // --- Consultas ----------------------------------------------------------

    public async Task<IReadOnlyList<PagoResumenResponse>> ListarMiosAsync(int usuarioId, EstadoPago? estado, TipoServicio? tipoTrabajo)
    {
        // Participa el que puso la plata o el que la va a cobrar.
        var query = _db.Pagos.AsNoTracking()
            .Include(p => p.Trabajo)
            .Where(p => p.Trabajo.ClienteId == usuarioId || p.Trabajo.EstudianteId == usuarioId);

        if (estado is EstadoPago e)
            query = query.Where(p => p.Estado == e);

        query = tipoTrabajo switch
        {
            TipoServicio.ProyectoCerrado => query.Where(p => p.Trabajo is TrabajoProyectoCerrado),
            TipoServicio.Clase => query.Where(p => p.Trabajo is TrabajoClase),
            TipoServicio.Salud => query.Where(p => p.Trabajo is TrabajoSalud),
            _ => query
        };

        var pagos = await query.OrderByDescending(p => p.FechaCreacion).ToListAsync();

        return pagos.Select(p => new PagoResumenResponse(
            p.Id,
            p.TrabajoId,
            p.Trabajo.TituloSnapshot,
            TipoDe(p.Trabajo),
            RolDe(usuarioId, p.Trabajo),
            p.MontoTotal,
            p.MontoAEstudiante,
            p.MontoComisionCalculada,
            p.Estado.ToString(),
            p.FechaCreacion,
            p.FechaLiberacion)).ToList();
    }

    public async Task<PagoDetalleResponse> ObtenerDetalleAsync(int usuarioId, int idPago)
    {
        var pago = await BuscarParticipandoAsync(usuarioId, idPago);

        var movimientos = await MovimientosDeAsync(idPago);

        return new PagoDetalleResponse(
            pago.Id,
            pago.TrabajoId,
            pago.Trabajo.TituloSnapshot,
            TipoDe(pago.Trabajo),
            pago.MontoTotal,
            pago.PorcentajeComisionLex,
            pago.MontoComisionCalculada,
            pago.MontoAEstudiante,
            pago.Estado.ToString(),
            pago.FechaCreacion,
            pago.FechaLiberacion,
            movimientos);
    }

    public async Task<IReadOnlyList<MovimientoPagoResponse>> ListarMovimientosAsync(int usuarioId, int idPago)
    {
        await BuscarParticipandoAsync(usuarioId, idPago);
        return await MovimientosDeAsync(idPago);
    }

    // Un pago al que no participás se responde igual que uno inexistente: 404 en los dos
    // casos, para no filtrar por diferencia de status que el pago existe.
    private async Task<Pago> BuscarParticipandoAsync(int usuarioId, int idPago)
    {
        var pago = await _db.Pagos.AsNoTracking()
            .Include(p => p.Trabajo)
            .FirstOrDefaultAsync(p => p.Id == idPago);

        if (pago is null || (pago.Trabajo.ClienteId != usuarioId && pago.Trabajo.EstudianteId != usuarioId))
            throw new NotFoundException($"No existe el pago {idPago}.");

        return pago;
    }

    // El libro se lee en orden cronologico: primero la retencion, despues su resolucion.
    private async Task<List<MovimientoPagoResponse>> MovimientosDeAsync(int idPago) =>
        await _db.MovimientosPago.AsNoTracking()
            .Where(m => m.PagoId == idPago)
            .OrderBy(m => m.FechaMovimiento).ThenBy(m => m.Id)
            .Select(m => new MovimientoPagoResponse(
                m.Id, m.Tipo.ToString(), m.Monto, m.Descripcion, m.FechaMovimiento))
            .ToListAsync();

    public async Task<IngresosAdminResponse> ObtenerIngresosLexAsync()
    {
        // El tipo de trabajo sale de la subclase TPT que materializa EF, asi que traemos
        // los pagos con su trabajo y agregamos en C# (tambien evita sumar decimals en SQL).
        var pagos = await _db.Pagos.AsNoTracking()
            .Include(p => p.Trabajo)
            .Select(p => new { p.Estado, Comision = p.MontoComisionCalculada, p.Trabajo })
            .ToListAsync();

        var liberados = pagos.Where(p => p.Estado == EstadoPago.Liberado).ToList();
        var retenidos = pagos.Where(p => p.Estado == EstadoPago.Retenido).ToList();
        var reembolsados = pagos.Where(p => p.Estado == EstadoPago.Reembolsado).ToList();

        var comisionLiberada = liberados.Sum(p => p.Comision);
        var comisionRetenida = retenidos.Sum(p => p.Comision);

        // Las 3 verticales aparecen siempre, aunque no tengan pagos todavia.
        var breakdown = Enum.GetValues<TipoServicio>().ToDictionary(
            tipo => tipo.ToString(),
            tipo =>
            {
                var delTipo = pagos.Where(p => TipoDe(p.Trabajo) == tipo.ToString()).ToList();
                return new IngresosPorVertical(
                    delTipo.Count,
                    delTipo.Where(p => p.Estado == EstadoPago.Liberado).Sum(p => p.Comision),
                    delTipo.Where(p => p.Estado == EstadoPago.Retenido).Sum(p => p.Comision));
            });

        return new IngresosAdminResponse(
            comisionLiberada,
            comisionRetenida,
            comisionLiberada + comisionRetenida, // las reembolsadas no cuentan
            pagos.Count,
            liberados.Count,
            retenidos.Count,
            reembolsados.Count,
            liberados.Count == 0 ? 0m : Math.Round(comisionLiberada / liberados.Count, 2, MidpointRounding.AwayFromZero),
            breakdown);
    }

    // La vertical la define la subclase concreta que materializo EF (TPT).
    private static string TipoDe(Trabajo trabajo) => trabajo switch
    {
        TrabajoProyectoCerrado => nameof(TipoServicio.ProyectoCerrado),
        TrabajoClase => nameof(TipoServicio.Clase),
        TrabajoSalud => nameof(TipoServicio.Salud),
        _ => throw new InvalidOperationException($"Vertical desconocida para el trabajo {trabajo.Id}.")
    };

    private static string RolDe(int usuarioId, Trabajo trabajo) =>
        trabajo.EstudianteId == usuarioId ? "Estudiante" : "Cliente";
}
