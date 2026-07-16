using Lex.Api.Common;
using Lex.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Features.Sesiones;

public class SesionService : ISesionService
{
    private readonly AppDbContext _db;

    public SesionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SesionResponse>> ListarDeTrabajoAsync(int usuarioId, int idTrabajo)
    {
        var trabajo = await _db.Trabajos.AsNoTracking()
            .Select(t => new { t.Id, t.ClienteId, t.EstudianteId })
            .FirstOrDefaultAsync(t => t.Id == idTrabajo);

        // Un trabajo ajeno se responde igual que uno inexistente, como en pagos y turnos.
        if (trabajo is null || (trabajo.ClienteId != usuarioId && trabajo.EstudianteId != usuarioId))
            throw new NotFoundException($"No existe el trabajo {idTrabajo}.");

        // El orden es el del paquete (1..N), no el cronologico: un turno reagendado no
        // deberia reordenar la lista de sesiones del trabajo.
        return await _db.Sesiones.AsNoTracking()
            .Where(s => s.TrabajoId == idTrabajo)
            .OrderBy(s => s.NumeroSesion)
            .Select(s => new SesionResponse(
                s.Id,
                s.NumeroSesion,
                s.Estado.ToString(),
                s.Turno.FechaHoraInicio,
                s.Turno.DuracionMinutos,
                s.FechaRealizada,
                s.Observaciones,
                s.Turno.LinkVideollamada))
            .ToListAsync();
    }
}
