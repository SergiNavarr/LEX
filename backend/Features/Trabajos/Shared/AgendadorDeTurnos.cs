using Lex.Api.Data;
using Lex.Api.Domain.Entities;
using Lex.Api.Domain.Enums;

namespace Lex.Api.Features.Trabajos.Shared;

// Convierte los slots elegidos por el cliente en turnos con su sesion. Lo comparten Clase
// (que agenda N) y Salud (que agenda uno): la unica diferencia entre las dos verticales es
// cuantos slots llegan, asi que la mecanica de agendar vive aca y no en cada una.
public static class AgendadorDeTurnos
{
    /// <summary>
    /// Deja en el DbContext un Turno + una Sesion por slot, numeradas desde 1 en orden
    /// cronologico. No llama a SaveChanges: el llamador cierra la unidad de trabajo.
    /// </summary>
    public static void Agendar(
        AppDbContext db, Trabajo trabajo, int estudianteId, int clienteId,
        IReadOnlyList<DateTime> slots, int duracionMinutos, string? notasCliente, DateTime ahora)
    {
        var numero = 1;

        foreach (var slot in slots.OrderBy(s => s))
        {
            // Los turnos nacen Confirmados: el estudiante ya publico ese horario como
            // disponible, y esa publicacion es la aceptacion. No hay paso intermedio.
            var turno = new Turno
            {
                EstudianteId = estudianteId,
                ClienteId = clienteId,
                FechaHoraInicio = DateTime.SpecifyKind(slot, DateTimeKind.Utc),
                DuracionMinutos = duracionMinutos,
                Estado = EstadoTurno.Confirmado,
                // La nota va en todos los turnos del paquete, no solo en el primero: es el
                // contexto del pedido, y el estudiante lo lee al abrir cualquiera de ellos.
                NotasCliente = notasCliente,
                FechaCreacion = ahora
            };

            // Turno y sesion se enlazan por navegacion y no por Id: se insertan en el mismo
            // SaveChanges que el trabajo, asi que todavia no tienen Id y EF resuelve las FKs.
            db.Sesiones.Add(new Sesion
            {
                Trabajo = trabajo,
                Turno = turno,
                NumeroSesion = numero++,
                Estado = EstadoSesion.Pendiente
            });
        }
    }
}
