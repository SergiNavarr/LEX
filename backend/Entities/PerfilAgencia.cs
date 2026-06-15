using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lex.Api.Entities;

// Perfil propio que cuelga de usuario (NO es subtipo de cliente). PK = usuario_id.
[Table("perfil_agencia")]
public class PerfilAgencia
{
    [Key]
    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("nombre_agencia")]
    public string? NombreAgencia { get; set; }

    [Column("rubro")]
    public string? Rubro { get; set; }

    [Column("sitio_web")]
    public string? SitioWeb { get; set; }

    // Navegacion
    public Usuario Usuario { get; set; } = null!;
}
