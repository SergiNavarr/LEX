using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lex.Api.Domain.Enums;

namespace Lex.Api.Domain.Entities;

// Nexo entre el trabajo (que dice que se contrato y cuanto se paga) y el turno (que dice
// cuando). Un trabajo tiene N sesiones: 1 en Salud y en Clase suelta, N en un paquete de
// Clase. Cada sesion consume exactamente un turno (indice UNIQUE en turno_id).
//
// Es la unidad de avance del trabajo: marcarla Realizada libera la fraccion de pago que le
// toca (MontoAEstudiante / CantidadSesionesTotales). Ese flujo llega en Hito 2 Parte 3.
[Table("sesion")]
public class Sesion
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    // FK a la tabla base de la jerarquia TPT (trabajo.id): un trabajo de Clase o Salud.
    [Column("trabajo_id")]
    public int TrabajoId { get; set; }

    [Column("turno_id")]
    public int TurnoId { get; set; }

    // Posicion dentro del paquete (1..N). En Salud y Clase suelta siempre 1.
    [Column("numero_sesion")]
    public int NumeroSesion { get; set; }

    [Column("estado")]
    public EstadoSesion Estado { get; set; } = EstadoSesion.Pendiente;

    [Column("fecha_realizada")]
    public DateTime? FechaRealizada { get; set; }

    [Column("observaciones")]
    public string? Observaciones { get; set; }

    // Navegacion
    public Trabajo Trabajo { get; set; } = null!;
    public Turno Turno { get; set; } = null!;
}
