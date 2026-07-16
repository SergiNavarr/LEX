using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.Domain.Entities;
using Lex.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Features.Disponibilidad;

public class DisponibilidadService : IDisponibilidadService
{
    private readonly AppDbContext _db;

    public DisponibilidadService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BloqueDisponibilidadResponse>> ListarMiosAsync(int estudianteId) =>
        await ActivosDe(estudianteId).Select(Proyeccion).ToListAsync();

    // Vista publica: misma proyeccion que la propia. Un bloque de disponibilidad no tiene
    // nada privado (es justamente lo que el estudiante publica para que le reserven).
    public async Task<IReadOnlyList<BloqueDisponibilidadResponse>> ListarDeEstudianteAsync(int estudianteId)
    {
        if (!await _db.PerfilesEstudiante.AnyAsync(p => p.UsuarioId == estudianteId))
            throw new NotFoundException($"No existe el estudiante {estudianteId}.");

        return await ActivosDe(estudianteId).Select(Proyeccion).ToListAsync();
    }

    public async Task<BloqueDisponibilidadResponse> CrearAsync(int estudianteId, CrearBloqueDisponibilidadRequest request)
    {
        await ValidarEsEstudianteAsync(estudianteId);
        ValidarFranja(request);
        await ValidarSinSuperposicionAsync(estudianteId, request, idExcluido: null);

        var bloque = new DisponibilidadEstudiante
        {
            EstudianteId = estudianteId,
            DiaSemana = request.DiaSemana,
            HoraInicio = request.HoraInicio,
            HoraFin = request.HoraFin,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _db.DisponibilidadesEstudiante.Add(bloque);
        await _db.SaveChangesAsync();

        return ToResponse(bloque);
    }

    public async Task<BloqueDisponibilidadResponse> ActualizarAsync(int estudianteId, int idBloque, CrearBloqueDisponibilidadRequest request)
    {
        var bloque = await BuscarPropioAsync(estudianteId, idBloque);
        ValidarFranja(request);
        // El propio bloque no cuenta como choque consigo mismo.
        await ValidarSinSuperposicionAsync(estudianteId, request, idExcluido: idBloque);

        bloque.DiaSemana = request.DiaSemana;
        bloque.HoraInicio = request.HoraInicio;
        bloque.HoraFin = request.HoraFin;
        await _db.SaveChangesAsync();

        return ToResponse(bloque);
    }

    // Baja logica. No se valida si hay turnos reservados en ese horario: los turnos ya
    // agendados no cuelgan del bloque, asi que dar de baja el bloque solo cierra la puerta
    // a reservas nuevas y no cancela nada de lo comprometido.
    public async Task DesactivarAsync(int estudianteId, int idBloque)
    {
        var bloque = await BuscarPropioAsync(estudianteId, idBloque);
        bloque.Activo = false;
        await _db.SaveChangesAsync();
    }

    // --- Validaciones -------------------------------------------------------

    private async Task ValidarEsEstudianteAsync(int estudianteId)
    {
        if (!await _db.PerfilesEstudiante.AnyAsync(p => p.UsuarioId == estudianteId))
            throw new ForbiddenException("Tu usuario no tiene un perfil de estudiante.");
    }

    private static void ValidarFranja(CrearBloqueDisponibilidadRequest request)
    {
        if (request.HoraInicio >= request.HoraFin)
            throw new BadRequestException(
                $"La hora de inicio ({request.HoraInicio:HH\\:mm}) debe ser anterior a la hora de fin ({request.HoraFin:HH\\:mm}).");
    }

    // Dos franjas del mismo dia chocan si una arranca antes de que la otra termine, y
    // viceversa. Los extremos que se tocan (18:00-20:00 despues de 14:00-18:00) no chocan.
    private async Task ValidarSinSuperposicionAsync(int estudianteId, CrearBloqueDisponibilidadRequest request, int? idExcluido)
    {
        var choque = await ActivosDe(estudianteId)
            .Where(d => d.DiaSemana == request.DiaSemana)
            .Where(d => idExcluido == null || d.Id != idExcluido)
            .Where(d => d.HoraInicio < request.HoraFin && request.HoraInicio < d.HoraFin)
            .FirstOrDefaultAsync();

        if (choque is not null)
            throw new BadRequestException(
                $"El bloque {request.DiaSemana} {request.HoraInicio:HH\\:mm}-{request.HoraFin:HH\\:mm} se superpone " +
                $"con uno existente ({choque.HoraInicio:HH\\:mm}-{choque.HoraFin:HH\\:mm}). " +
                "Editá o eliminá el bloque existente, o elegí otro horario.");
    }

    // Un bloque de otro estudiante se responde 404 igual que uno inexistente: distinguirlos
    // con un 403 confirmaria que el bloque existe.
    private async Task<DisponibilidadEstudiante> BuscarPropioAsync(int estudianteId, int idBloque)
    {
        var bloque = await _db.DisponibilidadesEstudiante
            .FirstOrDefaultAsync(d => d.Id == idBloque && d.Activo);

        if (bloque is null || bloque.EstudianteId != estudianteId)
            throw new NotFoundException($"No existe el bloque de disponibilidad {idBloque}.");

        return bloque;
    }

    // --- Helpers ------------------------------------------------------------

    private IQueryable<DisponibilidadEstudiante> ActivosDe(int estudianteId) =>
        _db.DisponibilidadesEstudiante.AsNoTracking()
            .Where(d => d.EstudianteId == estudianteId && d.Activo)
            .OrderBy(d => d.DiaSemana).ThenBy(d => d.HoraInicio);

    private static BloqueDisponibilidadResponse ToResponse(DisponibilidadEstudiante d) =>
        new(d.Id, d.DiaSemana, d.HoraInicio, d.HoraFin);

    private static readonly System.Linq.Expressions.Expression<Func<DisponibilidadEstudiante, BloqueDisponibilidadResponse>> Proyeccion =
        d => new BloqueDisponibilidadResponse(d.Id, d.DiaSemana, d.HoraInicio, d.HoraFin);
}
