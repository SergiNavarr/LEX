using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Features.Turnos;

public class ValidadorTurnosService : IValidadorTurnosService
{
    private readonly AppDbContext _db;

    public ValidadorTurnosService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string?> ValidarSlotAsync(int estudianteId, DateTime fechaHoraInicio, int duracionMinutos)
    {
        if (duracionMinutos <= 0)
            return "La duración de la sesión debe ser mayor a cero minutos.";

        // El cliente manda la fecha en UTC; si el JSON no traia 'Z' el Kind llega Unspecified
        // y Npgsql lo rechaza al comparar contra timestamptz. Se normaliza una sola vez aca.
        var inicio = DateTime.SpecifyKind(fechaHoraInicio, DateTimeKind.Utc);
        var fin = inicio.AddMinutes(duracionMinutos);

        // (a) Solo horarios futuros. No hay anticipacion minima: reservar para dentro de
        // 10 minutos es valido, reservar para ayer no.
        if (inicio <= DateTime.UtcNow)
            return $"El horario {HorarioArgentina.Describir(inicio)} ya pasó: solo se pueden reservar turnos futuros.";

        // (b) y (c): el turno entero tiene que entrar en un bloque del dia. El dia y la hora
        // que importan son los locales: un turno de lunes 00:30 ART cae domingo en UTC.
        var local = HorarioArgentina.AHoraLocal(inicio);
        var dia = HorarioArgentina.DiaSemanaDe(DateOnly.FromDateTime(local));
        var minutoInicio = (int)local.TimeOfDay.TotalMinutes;
        var minutoFin = minutoInicio + duracionMinutos;

        var bloques = await _db.DisponibilidadesEstudiante.AsNoTracking()
            .Where(d => d.EstudianteId == estudianteId && d.Activo && d.DiaSemana == dia)
            .Select(d => new { d.HoraInicio, d.HoraFin })
            .ToListAsync();

        if (bloques.Count == 0)
            return $"El estudiante no atiende los días {dia}.";

        // Un turno que se pasa de medianoche tampoco entra: los bloques son intra-dia, asi
        // que minutoFin > 1440 no cae en ninguno y la comparacion lo rechaza sola.
        var entraEnAlgunBloque = bloques.Any(b =>
            HorarioArgentina.MinutosDeDia(b.HoraInicio) <= minutoInicio
            && minutoFin <= HorarioArgentina.MinutosDeDia(b.HoraFin));

        if (!entraEnAlgunBloque)
            return $"El horario {HorarioArgentina.Describir(inicio)} ({duracionMinutos} min) no entra en ningún bloque " +
                   $"de disponibilidad del estudiante para el día {dia}.";

        // (d) No pisa un turno ya tomado. El margen de un dia hacia atras atrapa al turno que
        // arranca antes y termina adentro; el solapamiento exacto se decide en memoria porque
        // la duracion es una columna y no se puede sumar a la fecha en SQL.
        var ocupados = await _db.Turnos.AsNoTracking()
            .Where(t => t.EstudianteId == estudianteId
                        && EstadosDeAgenda.Ocupan.Contains(t.Estado)
                        && t.FechaHoraInicio < fin
                        && t.FechaHoraInicio >= inicio.AddDays(-1))
            .Select(t => new { t.FechaHoraInicio, t.DuracionMinutos })
            .ToListAsync();

        var choque = ocupados.FirstOrDefault(o => inicio < o.FechaHoraInicio.AddMinutes(o.DuracionMinutos));
        if (choque is not null)
            return $"El horario {HorarioArgentina.Describir(inicio)} se superpone con un turno que el estudiante " +
                   $"ya tiene tomado ({HorarioArgentina.Describir(choque.FechaHoraInicio)}, {choque.DuracionMinutos} min).";

        return null;
    }

    // Los slots de un mismo paquete se validan entre si ademas de contra la agenda: dos
    // slots nuevos que chocan pasarian el chequeo individual, porque ninguno de los dos
    // esta todavia en la DB cuando se valida el otro.
    public string? ValidarSlotsNoSolapan(IReadOnlyList<DateTime> slots, int duracionMinutos)
    {
        // Como todos los slots duran lo mismo, alcanza con ordenarlos y comparar cada uno
        // con su anterior: si hay algun solapamiento, hay uno entre consecutivos. Ordenar
        // tambien hace que dos slots identicos caigan juntos y se detecten.
        var ordenados = slots
            .Select(s => DateTime.SpecifyKind(s, DateTimeKind.Utc))
            .OrderBy(s => s)
            .ToList();

        for (var i = 1; i < ordenados.Count; i++)
        {
            var finAnterior = ordenados[i - 1].AddMinutes(duracionMinutos);
            if (ordenados[i] < finAnterior)
                return $"Los turnos {HorarioArgentina.Describir(ordenados[i - 1])} y " +
                       $"{HorarioArgentina.Describir(ordenados[i])} se superponen entre sí " +
                       $"(cada sesión dura {duracionMinutos} min).";
        }

        return null;
    }
}
