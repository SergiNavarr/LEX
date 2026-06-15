using System.ComponentModel.DataAnnotations.Schema;

namespace Lex.Api.Entities;

// Clave primaria compuesta (rol_id, usuario_id) — configurada en AppDbContext.
[Table("usuario_rol")]
public class UsuarioRol
{
    [Column("rol_id")]
    public int RolId { get; set; }

    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    // Navegacion
    public Rol Rol { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}
