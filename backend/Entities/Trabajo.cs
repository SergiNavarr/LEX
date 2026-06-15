using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lex.Api.Enums;

namespace Lex.Api.Entities;

// Motor transaccional. Un trabajo nace de un servicio directo O de una
// postulacion aceptada: por eso IdServicio, IdPostulacion y PacienteId son NULLABLE.
[Table("trabajo")]
public class Trabajo
{
    [Key]
    [Column("id_trabajo")]
    public int IdTrabajo { get; set; }

    [Column("estudiante_id")]
    public int EstudianteId { get; set; }

    [Column("cliente_id")]
    public int ClienteId { get; set; }

    [Column("tipo_servicio_id")]
    public int? TipoServicioId { get; set; }

    [Column("origen")]
    public OrigenTrabajo Origen { get; set; } = OrigenTrabajo.Directo;

    [Column("id_servicio")]
    public int? IdServicio { get; set; }

    [Column("id_postulacion")]
    public int? IdPostulacion { get; set; }

    [Column("paciente_id")]
    public int? PacienteId { get; set; }

    [Column("estado")]
    public EstadoTrabajo Estado { get; set; } = EstadoTrabajo.Pendiente;

    [Column("monto")]
    public decimal Monto { get; set; }

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; }

    [Column("fecha_inicio")]
    public DateTime? FechaInicio { get; set; }

    [Column("fecha_fin")]
    public DateTime? FechaFin { get; set; }

    // Navegacion
    public PerfilEstudiante Estudiante { get; set; } = null!;
    public PerfilCliente Cliente { get; set; } = null!;
    public TipoServicio? TipoServicio { get; set; }
    public Servicio? Servicio { get; set; }
    public Postulacion? Postulacion { get; set; }
    public Paciente? Paciente { get; set; }
    public ICollection<TrabajoHistorial> Historiales { get; set; } = new List<TrabajoHistorial>();
    public Pago? Pago { get; set; }                   // 1->1: a lo sumo un pago
    public Consentimiento? Consentimiento { get; set; } // 1->1: a lo sumo un consentimiento
    public ICollection<Resena> Resenas { get; set; } = new List<Resena>();
}
