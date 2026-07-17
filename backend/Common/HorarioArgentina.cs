using Lex.Api.Domain.Enums;

namespace Lex.Api.Common;

// Conversion entre la hora local de Argentina y UTC, en un solo lugar.
//
// LEX opera en el NEA, asi que toda la agenda se piensa en hora argentina: un bloque
// "Lunes 14:00-18:00" es 14:00 en Corrientes, no 14:00 UTC. La DB, en cambio, guarda
// instantes en UTC (timestamptz). Este helper es la frontera entre esos dos mundos.
//
// Offset fijo -03:00, sin TimeZoneInfo: Argentina no aplica horario de verano desde 2009
// y la base de datos de zonas horarias no esta disponible de forma uniforme en el
// contenedor Linux del deploy. Si alguna vez vuelve el DST o LEX sale del pais, este es
// el unico archivo que hay que cambiar.
public static class HorarioArgentina
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(-3);

    /// <summary>Instante UTC que corresponde a una fecha y hora local argentina.</summary>
    public static DateTime ALocalUtc(DateOnly fecha, TimeOnly hora) =>
        DateTime.SpecifyKind(fecha.ToDateTime(hora) - Offset, DateTimeKind.Utc);

    /// <summary>Instante UTC del comienzo (00:00 local) de una fecha argentina.</summary>
    public static DateTime InicioDelDiaUtc(DateOnly fecha) =>
        ALocalUtc(fecha, TimeOnly.MinValue);

    /// <summary>Instante UTC del comienzo del dia siguiente: fin exclusivo de una fecha argentina.</summary>
    public static DateTime FinDelDiaUtc(DateOnly fecha) =>
        InicioDelDiaUtc(fecha.AddDays(1));

    /// <summary>Fecha y hora local argentina de un instante UTC.</summary>
    public static DateTime AHoraLocal(DateTime instanteUtc) =>
        DateTime.SpecifyKind(instanteUtc, DateTimeKind.Utc) + Offset;

    /// <summary>
    /// DiaSemana de una fecha local. System.DayOfWeek arranca en domingo; DiaSemana en lunes.
    /// El dia que importa es siempre el local: un turno del lunes 00:30 ART cae domingo en UTC.
    /// </summary>
    public static DiaSemana DiaSemanaDe(DateOnly fecha) => fecha.DayOfWeek switch
    {
        DayOfWeek.Monday => DiaSemana.Lunes,
        DayOfWeek.Tuesday => DiaSemana.Martes,
        DayOfWeek.Wednesday => DiaSemana.Miercoles,
        DayOfWeek.Thursday => DiaSemana.Jueves,
        DayOfWeek.Friday => DiaSemana.Viernes,
        DayOfWeek.Saturday => DiaSemana.Sabado,
        DayOfWeek.Sunday => DiaSemana.Domingo,
        _ => throw new ArgumentOutOfRangeException(nameof(fecha))
    };

    /// <summary>Minutos transcurridos desde la medianoche local. Evita aritmetica sobre TimeOnly, que da la vuelta al reloj.</summary>
    public static int MinutosDeDia(TimeOnly hora) => (int)hora.ToTimeSpan().TotalMinutes;

    /// <summary>Un instante UTC formateado en hora local, para mensajes de error legibles.</summary>
    public static string Describir(DateTime instanteUtc) =>
        AHoraLocal(instanteUtc).ToString("dd/MM/yyyy HH:mm");
}
