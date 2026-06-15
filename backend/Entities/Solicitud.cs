using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lex.Api.Enums;

namespace Lex.Api.Entities;

[Table("solicitud")]
public class Solicitud
{
    [Key]
    [Column("id_solicitud")]
    public int IdSolicitud { get; set; }

    [Column("cliente_id")]
    public int ClienteId { get; set; }

    [Column("tipo_servicio_id")]
    public int? TipoServicioId { get; set; }

    [Column("titulo")]
    public string Titulo { get; set; } = null!;

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("presupuesto_estimado")]
    public decimal? PresupuestoEstimado { get; set; }

    [Column("estado")]
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Abierta;

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; }

    [Column("fecha_cierre")]
    public DateTime? FechaCierre { get; set; }

    // Navegacion
    public PerfilCliente Cliente { get; set; } = null!;
    public TipoServicio? TipoServicio { get; set; }
    public ICollection<Postulacion> Postulaciones { get; set; } = new List<Postulacion>();
}
