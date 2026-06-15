using System.ComponentModel.DataAnnotations.Schema;

namespace Lex.Api.Entities;

[Table("institucion")]
public class Institucion
{
    [Column("institucion_id")]
    public int InstitucionId { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = null!;

    [Column("tipo_institucion_id")]
    public int TipoInstitucionId { get; set; }

    [Column("provincia")]
    public string? Provincia { get; set; }

    [Column("ciudad")]
    public string? Ciudad { get; set; }

    // Navegacion
    public TipoInstitucion TipoInstitucion { get; set; } = null!;
    public ICollection<Carrera> Carreras { get; set; } = new List<Carrera>();
}
