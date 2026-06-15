using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lex.Api.Entities;

// PK = usuario_id, que ademas es FK a usuario (perfil 1:1 con usuario).
[Table("perfil_estudiante")]
public class PerfilEstudiante
{
    [Key]
    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("bio")]
    public string? Bio { get; set; }

    [Column("anio_cursado")]
    public int? AnioCursado { get; set; }

    [Column("calificacion_promedio")]
    public decimal CalificacionPromedio { get; set; }

    [Column("cantidad_trabajos")]
    public int CantidadTrabajos { get; set; }

    [Column("disponible")]
    public bool Disponible { get; set; } = true;

    // Navegacion
    public Usuario Usuario { get; set; } = null!;
    public ICollection<EstudianteCarrera> EstudianteCarreras { get; set; } = new List<EstudianteCarrera>();
    public ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
    public ICollection<Postulacion> Postulaciones { get; set; } = new List<Postulacion>();
    public ICollection<Trabajo> Trabajos { get; set; } = new List<Trabajo>();
}
