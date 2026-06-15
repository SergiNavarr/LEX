using System.ComponentModel.DataAnnotations.Schema;

namespace Lex.Api.Entities;

[Table("carrera")]
public class Carrera
{
    [Column("carrera_id")]
    public int CarreraId { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = null!;

    [Column("institucion_id")]
    public int InstitucionId { get; set; }

    [Column("area_conocimiento")]
    public string? AreaConocimiento { get; set; } // habilita servicios de Salud, etc.

    // Navegacion
    public Institucion Institucion { get; set; } = null!;
    public ICollection<EstudianteCarrera> EstudianteCarreras { get; set; } = new List<EstudianteCarrera>();
}
