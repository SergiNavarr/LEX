using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lex.Api.Enums;

namespace Lex.Api.Entities;

// Escrow + take rate. estado: 0=Retenido, 1=Liberado, 2=Reembolsado.
[Table("pago")]
public class Pago
{
    [Key]
    [Column("id_pago")]
    public int IdPago { get; set; }

    [Column("id_trabajo")]
    public int IdTrabajo { get; set; }

    [Column("monto_total")]
    public decimal MontoTotal { get; set; }

    [Column("porcentaje_comision")]
    public decimal PorcentajeComision { get; set; } // ej. 10.00 = 10%

    [Column("comision_lex")]
    public decimal ComisionLex { get; set; }

    [Column("monto_estudiante")]
    public decimal MontoEstudiante { get; set; }

    [Column("estado")]
    public EstadoPago Estado { get; set; } = EstadoPago.Retenido;

    [Column("metodo_pago")]
    public string? MetodoPago { get; set; }

    [Column("fecha_retencion")]
    public DateTime? FechaRetencion { get; set; }

    [Column("fecha_liberacion")]
    public DateTime? FechaLiberacion { get; set; }

    // Navegacion
    public Trabajo Trabajo { get; set; } = null!;
}
