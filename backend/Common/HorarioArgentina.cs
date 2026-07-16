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
}
