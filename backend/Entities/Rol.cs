using System.ComponentModel.DataAnnotations.Schema;

namespace Lex.Api.Entities;

[Table("rol")]
public class Rol
{
    [Column("rol_id")]
    public int RolId { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = null!; // Estudiante, Cliente, Agencia, Admin

    // Navegacion
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}
