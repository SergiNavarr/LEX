using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.DTOs;
using Lex.Api.Entities;
using Lex.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Services;

public class SolicitudService : ISolicitudService
{
    private readonly AppDbContext _db;

    public SolicitudService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SolicitudResponse> CrearAsync(int clienteId, CrearSolicitudRequest request)
    {
        if (request.TipoServicioId is int tipo &&
            !await _db.TiposServicio.AnyAsync(t => t.TipoServicioId == tipo))
            throw new BadRequestException($"El tipo de servicio {tipo} no existe.");

        var solicitud = new Solicitud
        {
            ClienteId = clienteId,
            TipoServicioId = request.TipoServicioId,
            Titulo = request.Titulo.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            PresupuestoEstimado = request.PresupuestoEstimado,
            Estado = EstadoSolicitud.Abierta,
            FechaCreacion = DateTime.UtcNow
        };

        _db.Solicitudes.Add(solicitud);
        await _db.SaveChangesAsync();

        return await ObtenerAsync(solicitud.IdSolicitud);
    }

    public async Task<IReadOnlyList<SolicitudResponse>> ListarAbiertasAsync(int? tipoServicioId, string? texto)
    {
        var query = _db.Solicitudes.AsNoTracking().Where(s => s.Estado == EstadoSolicitud.Abierta);

        if (tipoServicioId is int tipo)
            query = query.Where(s => s.TipoServicioId == tipo);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var t = texto.Trim();
            query = query.Where(s => EF.Functions.Like(s.Titulo, $"%{t}%"));
        }

        return await query
            .OrderByDescending(s => s.FechaCreacion)
            .Select(Proyeccion)
            .ToListAsync();
    }

    public async Task<SolicitudResponse> ObtenerAsync(int idSolicitud)
    {
        return await _db.Solicitudes.AsNoTracking()
            .Where(s => s.IdSolicitud == idSolicitud)
            .Select(Proyeccion)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"No existe la solicitud {idSolicitud}.");
    }

    public async Task<IReadOnlyList<SolicitudMiaResponse>> ListarMiasAsync(int clienteId)
    {
        return await _db.Solicitudes.AsNoTracking()
            .Where(s => s.ClienteId == clienteId)
            .OrderByDescending(s => s.FechaCreacion)
            .Select(s => new SolicitudMiaResponse
            {
                IdSolicitud = s.IdSolicitud,
                ClienteId = s.ClienteId,
                ClienteNombre = s.Cliente.Usuario.NombreCompleto,
                TipoServicioId = s.TipoServicioId,
                TipoServicioNombre = s.TipoServicio != null ? s.TipoServicio.Nombre : null,
                Titulo = s.Titulo,
                Descripcion = s.Descripcion,
                PresupuestoEstimado = s.PresupuestoEstimado,
                Estado = s.Estado,
                FechaCreacion = s.FechaCreacion,
                FechaCierre = s.FechaCierre,
                CantidadPostulaciones = s.Postulaciones.Count
            })
            .ToListAsync();
    }

    public async Task CerrarAsync(int clienteId, int idSolicitud)
    {
        var solicitud = await _db.Solicitudes.FirstOrDefaultAsync(s => s.IdSolicitud == idSolicitud)
            ?? throw new NotFoundException($"No existe la solicitud {idSolicitud}.");

        if (solicitud.ClienteId != clienteId)
            throw new ForbiddenException("No podés cerrar una solicitud que no es tuya.");

        solicitud.Estado = EstadoSolicitud.Cancelada;
        solicitud.FechaCierre = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static readonly System.Linq.Expressions.Expression<Func<Solicitud, SolicitudResponse>> Proyeccion =
        s => new SolicitudResponse
        {
            IdSolicitud = s.IdSolicitud,
            ClienteId = s.ClienteId,
            ClienteNombre = s.Cliente.Usuario.NombreCompleto,
            TipoServicioId = s.TipoServicioId,
            TipoServicioNombre = s.TipoServicio != null ? s.TipoServicio.Nombre : null,
            Titulo = s.Titulo,
            Descripcion = s.Descripcion,
            PresupuestoEstimado = s.PresupuestoEstimado,
            Estado = s.Estado,
            FechaCreacion = s.FechaCreacion,
            FechaCierre = s.FechaCierre
        };
}
