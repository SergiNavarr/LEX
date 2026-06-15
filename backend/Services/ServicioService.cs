using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.DTOs;
using Lex.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Services;

public class ServicioService : IServicioService
{
    private readonly AppDbContext _db;

    public ServicioService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServicioResponse> CrearAsync(int estudianteId, CrearServicioRequest request)
    {
        ValidarPrecio(request.Precio);
        await ValidarTipoServicioAsync(request.TipoServicioId);

        var perfilExiste = await _db.PerfilesEstudiante.AnyAsync(p => p.UsuarioId == estudianteId);
        if (!perfilExiste)
            throw new ForbiddenException("Tu usuario no tiene un perfil de estudiante.");

        var servicio = new Servicio
        {
            EstudianteId = estudianteId,
            TipoServicioId = request.TipoServicioId,
            Titulo = request.Titulo.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            Precio = request.Precio,
            TiempoEntregaDias = request.TiempoEntregaDias,
            Activo = true,
            FechaPublicacion = DateTime.UtcNow
        };

        _db.Servicios.Add(servicio);
        await _db.SaveChangesAsync();

        return await ObtenerAsync(servicio.IdServicio);
    }

    public async Task<ServicioResponse> ActualizarAsync(int estudianteId, int idServicio, ActualizarServicioRequest request)
    {
        ValidarPrecio(request.Precio);
        await ValidarTipoServicioAsync(request.TipoServicioId);

        var servicio = await _db.Servicios.FirstOrDefaultAsync(s => s.IdServicio == idServicio)
            ?? throw new NotFoundException($"No existe el servicio {idServicio}.");

        if (servicio.EstudianteId != estudianteId)
            throw new ForbiddenException("No podés editar un servicio que no es tuyo.");

        servicio.Titulo = request.Titulo.Trim();
        servicio.Descripcion = request.Descripcion?.Trim();
        servicio.Precio = request.Precio;
        servicio.TipoServicioId = request.TipoServicioId;
        servicio.TiempoEntregaDias = request.TiempoEntregaDias;

        await _db.SaveChangesAsync();

        return await ObtenerAsync(servicio.IdServicio);
    }

    public async Task EliminarAsync(int estudianteId, int idServicio)
    {
        var servicio = await _db.Servicios.FirstOrDefaultAsync(s => s.IdServicio == idServicio)
            ?? throw new NotFoundException($"No existe el servicio {idServicio}.");

        if (servicio.EstudianteId != estudianteId)
            throw new ForbiddenException("No podés dar de baja un servicio que no es tuyo.");

        // Baja logica: no se borra fisicamente.
        servicio.Activo = false;
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ServicioResponse>> ListarAsync(int? tipoServicioId, string? texto)
    {
        var query = _db.Servicios.AsNoTracking().Where(s => s.Activo);

        if (tipoServicioId is int tipo)
            query = query.Where(s => s.TipoServicioId == tipo);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var t = texto.Trim();
            query = query.Where(s => EF.Functions.Like(s.Titulo, $"%{t}%"));
        }

        return await query
            .OrderByDescending(s => s.FechaPublicacion)
            .Select(Proyeccion)
            .ToListAsync();
    }

    public async Task<ServicioResponse> ObtenerAsync(int idServicio)
    {
        return await _db.Servicios.AsNoTracking()
            .Where(s => s.IdServicio == idServicio)
            .Select(Proyeccion)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"No existe el servicio {idServicio}.");
    }

    private static void ValidarPrecio(decimal precio)
    {
        if (precio <= 0)
            throw new BadRequestException("El precio debe ser mayor a 0.");
    }

    private async Task ValidarTipoServicioAsync(int tipoServicioId)
    {
        var existe = await _db.TiposServicio.AnyAsync(t => t.TipoServicioId == tipoServicioId);
        if (!existe)
            throw new BadRequestException($"El tipo de servicio {tipoServicioId} no existe.");
    }

    // Proyeccion reutilizable: incluye nombre y calificacion del estudiante.
    private static readonly System.Linq.Expressions.Expression<Func<Servicio, ServicioResponse>> Proyeccion =
        s => new ServicioResponse
        {
            IdServicio = s.IdServicio,
            Titulo = s.Titulo,
            Descripcion = s.Descripcion,
            Precio = s.Precio,
            TiempoEntregaDias = s.TiempoEntregaDias,
            Activo = s.Activo,
            FechaPublicacion = s.FechaPublicacion,
            TipoServicioId = s.TipoServicioId,
            TipoServicioNombre = s.TipoServicio.Nombre,
            EstudianteId = s.EstudianteId,
            EstudianteNombre = s.Estudiante.Usuario.NombreCompleto,
            EstudianteCalificacion = s.Estudiante.CalificacionPromedio
        };
}
