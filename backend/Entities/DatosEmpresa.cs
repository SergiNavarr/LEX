using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lex.Api.Entities;

// PK = usuario_id, FK a perfil_cliente.
[Table("datos_empresa")]
public class DatosEmpresa
{
    [Key]
    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("razon_social")]
    public string? RazonSocial { get; set; }

    [Column("cuit")]
    public string? Cuit { get; set; }

    [Column("rubro")]
    public string? Rubro { get; set; }

    // Navegacion
    public PerfilCliente PerfilCliente { get; set; } = null!;
}
