using System.ComponentModel.DataAnnotations.Schema;

namespace Lex.Api.Entities;

[Table("tipo_institucion")]
public class TipoInstitucion
{
    [Column("tipo_institucion_id")]
    public int TipoInstitucionId { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = null!; // Universidad, Instituto terciario...

    // Navegacion
    public ICollection<Institucion> Instituciones { get; set; } = new List<Institucion>();
}
