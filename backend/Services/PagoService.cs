using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.DTOs;
using Lex.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Services;

public class PagoService : IPagoService
{
    private readonly AppDbContext _db;

    public PagoService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagoResponse> ObtenerPorTrabajoAsync(int usuarioId, int idTrabajo)
    {
        // Validamos participacion mirando el trabajo (existe siempre, aunque no haya pago).
        var trabajo = await _db.Trabajos.AsNoTracking()
            .Where(t => t.IdTrabajo == idTrabajo)
            .Select(t => new { t.EstudianteId, t.ClienteId })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"No existe el trabajo {idTrabajo}.");

        if (trabajo.EstudianteId != usuarioId && trabajo.ClienteId != usuarioId)
            throw new ForbiddenException("No participás en este trabajo.");

        return await _db.Pagos.AsNoTracking()
            .Where(p => p.IdTrabajo == idTrabajo)
            .Select(Proyeccion)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"El trabajo {idTrabajo} todavía no tiene un pago asociado.");
    }

    public async Task<MisPagosResponse> ListarMiosAsync(int estudianteId)
    {
        var pagos = await _db.Pagos.AsNoTracking()
            .Where(p => p.Trabajo.EstudianteId == estudianteId)
            .OrderByDescending(p => p.FechaRetencion)
            .Select(Proyeccion)
            .ToListAsync();

        return new MisPagosResponse
        {
            TotalCobrado = pagos.Where(p => p.Estado == EstadoPago.Liberado).Sum(p => p.MontoEstudiante),
            TotalRetenido = pagos.Where(p => p.Estado == EstadoPago.Retenido).Sum(p => p.MontoEstudiante),
            Pagos = pagos
        };
    }

    public async Task<IngresosLexResponse> ObtenerIngresosLexAsync()
    {
        // Traemos las comisiones por estado; el agregado se hace en C# (decimal).
        var pagos = await _db.Pagos.AsNoTracking()
            .Select(p => new { p.Estado, p.ComisionLex })
            .ToListAsync();

        var liberados = pagos.Where(p => p.Estado == EstadoPago.Liberado).ToList();
        var retenidos = pagos.Where(p => p.Estado == EstadoPago.Retenido).ToList();
        var reembolsados = pagos.Where(p => p.Estado == EstadoPago.Reembolsado).ToList();

        var comisionLiberada = liberados.Sum(p => p.ComisionLex);
        var comisionRetenida = retenidos.Sum(p => p.ComisionLex);

        return new IngresosLexResponse
        {
            ComisionLiberada = comisionLiberada,
            ComisionRetenida = comisionRetenida,
            ComisionTotal = comisionLiberada + comisionRetenida, // las reembolsadas no cuentan
            CantidadTrabajosConPago = pagos.Count,
            CantidadPagosLiberados = liberados.Count,
            CantidadPagosRetenidos = retenidos.Count,
            CantidadPagosReembolsados = reembolsados.Count
        };
    }

    private static readonly System.Linq.Expressions.Expression<Func<Entities.Pago, PagoResponse>> Proyeccion =
        p => new PagoResponse
        {
            IdPago = p.IdPago,
            IdTrabajo = p.IdTrabajo,
            MontoTotal = p.MontoTotal,
            PorcentajeComision = p.PorcentajeComision,
            ComisionLex = p.ComisionLex,
            MontoEstudiante = p.MontoEstudiante,
            Estado = p.Estado,
            FechaRetencion = p.FechaRetencion,
            FechaLiberacion = p.FechaLiberacion
        };
}
