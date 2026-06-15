using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lex.Api.Entities;

// PK = usuario_id, FK a perfil_cliente.
[Table("datos_particular")]
public class DatosParticular
{
    [Key]
    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("dni")]
    public string? Dni { get; set; }

    // Navegacion
    public PerfilCliente PerfilCliente { get; set; } = null!;
}
