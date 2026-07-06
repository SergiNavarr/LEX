using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lex.Api.Entities;

[Table("servicio")]
public class Servicio
{
    [Key]
    [Column("id_servicio")]
    public int IdServicio { get; set; }

    [Column("estudiante_id")]
    public int EstudianteId { get; set; }

    [Column("tipo_servicio_id")]
    public int TipoServicioId { get; set; }

    [Column("titulo")]
    public string Titulo { get; set; } = null!;

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    // URL externa de la imagen de portada (sin subida de archivos por ahora).
    [Column("imagen_url")]
    public string? ImagenUrl { get; set; }

    [Column("precio")]
    public decimal Precio { get; set; }

    [Column("tiempo_entrega_dias")]
    public int? TiempoEntregaDias { get; set; }

    [Column("activo")]
    public bool Activo { get; set; } = true;

    [Column("fecha_publicacion")]
    public DateTime FechaPublicacion { get; set; }

    // Navegacion
    public PerfilEstudiante Estudiante { get; set; } = null!;
    public TipoServicio TipoServicio { get; set; } = null!;
    public ICollection<Trabajo> Trabajos { get; set; } = new List<Trabajo>();
}
