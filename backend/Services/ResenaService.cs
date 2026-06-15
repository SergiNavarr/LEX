using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.DTOs;
using Lex.Api.Entities;
using Lex.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Services;

public class ResenaService : IResenaService
{
    private readonly AppDbContext _db;

    public ResenaService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ResenaResponse> CrearAsync(int autorId, int idTrabajo, CrearResenaRequest request)
    {
        var puntaje = request.Puntaje ?? throw new BadRequestException("El puntaje es obligatorio.");
        if (puntaje is < 1 or > 5)
            throw new BadRequestException("El puntaje debe estar entre 1 y 5.");

        var trabajo = await _db.Trabajos.FirstOrDefaultAsync(t => t.IdTrabajo == idTrabajo)
            ?? throw new NotFoundException($"No existe el trabajo {idTrabajo}.");

        // Solo las partes pueden reseñar; el receptor es la otra parte.
        int receptorId;
        if (autorId == trabajo.EstudianteId)
            receptorId = trabajo.ClienteId;
        else if (autorId == trabajo.ClienteId)
            receptorId = trabajo.EstudianteId;
        else
            throw new ForbiddenException("Solo las partes del trabajo pueden dejar una reseña.");

        if (trabajo.Estado != EstadoTrabajo.Completado)
            throw new BadRequestException("Solo se pueden reseñar trabajos completados.");

        // Una sola reseña por autor por trabajo (lo refuerza el indice unico).
        var yaResenado = await _db.Resenas
            .AnyAsync(r => r.IdTrabajo == idTrabajo && r.AutorUsuarioId == autorId);
        if (yaResenado)
            throw new BadRequestException("Ya dejaste una reseña para este trabajo.");

        var resena = new Resena
        {
            IdTrabajo = idTrabajo,
            AutorUsuarioId = autorId,
            ReceptorUsuarioId = receptorId,
            Puntaje = puntaje,
            Comentario = string.IsNullOrWhiteSpace(request.Comentario) ? null : request.Comentario.Trim(),
            Fecha = DateTime.UtcNow
        };
        _db.Resenas.Add(resena);

        // Si el receptor es el estudiante del trabajo, recalculamos su calificacion
        // promedio en la misma operacion (promedio de todos los puntajes recibidos).
        if (receptorId == trabajo.EstudianteId)
            await RecalcularCalificacionAsync(receptorId, puntaje);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException) // carrera contra el indice unico (id_trabajo, autor)
        {
            throw new BadRequestException("Ya dejaste una reseña para este trabajo.");
        }

        return await ObtenerAsync(resena.IdResena);
    }

    public async Task<IReadOnlyList<ResenaResponse>> ListarRecibidasAsync(int usuarioId)
    {
        return await _db.Resenas.AsNoTracking()
            .Where(r => r.ReceptorUsuarioId == usuarioId)
            .OrderByDescending(r => r.Fecha)
            .Select(Proyeccion)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ResenaResponse>> ListarPorTrabajoAsync(int usuarioId, int idTrabajo)
    {
        var trabajo = await _db.Trabajos.AsNoTracking()
            .Where(t => t.IdTrabajo == idTrabajo)
            .Select(t => new { t.EstudianteId, t.ClienteId })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"No existe el trabajo {idTrabajo}.");

        if (trabajo.EstudianteId != usuarioId && trabajo.ClienteId != usuarioId)
            throw new ForbiddenException("No participás en este trabajo.");

        return await _db.Resenas.AsNoTracking()
            .Where(r => r.IdTrabajo == idTrabajo)
            .OrderByDescending(r => r.Fecha)
            .Select(Proyeccion)
            .ToListAsync();
    }

    // El promedio incluye la reseña recien agregada (aun no persistida): por eso
    // sumamos el nuevo puntaje a los ya existentes.
    private async Task RecalcularCalificacionAsync(int estudianteId, int puntajeNuevo)
    {
        var perfil = await _db.PerfilesEstudiante
            .FirstOrDefaultAsync(p => p.UsuarioId == estudianteId);
        if (perfil is null)
            return;

        var puntajesPrevios = await _db.Resenas
            .Where(r => r.ReceptorUsuarioId == estudianteId)
            .Select(r => r.Puntaje)
            .ToListAsync();

        var suma = puntajesPrevios.Sum() + puntajeNuevo;
        var cantidad = puntajesPrevios.Count + 1;
        perfil.CalificacionPromedio = Math.Round((decimal)suma / cantidad, 2, MidpointRounding.AwayFromZero);
    }

    private async Task<ResenaResponse> ObtenerAsync(int idResena)
    {
        return await _db.Resenas.AsNoTracking()
            .Where(r => r.IdResena == idResena)
            .Select(Proyeccion)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"No existe la reseña {idResena}.");
    }

    private static readonly System.Linq.Expressions.Expression<Func<Resena, ResenaResponse>> Proyeccion =
        r => new ResenaResponse
        {
            IdResena = r.IdResena,
            IdTrabajo = r.IdTrabajo,
            AutorUsuarioId = r.AutorUsuarioId,
            AutorNombre = r.Autor.NombreCompleto,
            ReceptorUsuarioId = r.ReceptorUsuarioId,
            Puntaje = r.Puntaje,
            Comentario = r.Comentario,
            Fecha = r.Fecha
        };
}
