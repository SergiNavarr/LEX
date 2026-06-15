using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lex.Api.Entities;

[Table("usuario")]
public class Usuario
{
    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("email")]
    public string Email { get; set; } = null!;

    [Column("password_hash")]
    public string PasswordHash { get; set; } = null!;

    [Column("nombre_completo")]
    public string NombreCompleto { get; set; } = null!;

    [Column("telefono")]
    public string? Telefono { get; set; }

    [Column("fecha_registro")]
    public DateTime FechaRegistro { get; set; }

    [Column("activo")]
    public bool Activo { get; set; } = true;

    // Navegacion
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
    public PerfilEstudiante? PerfilEstudiante { get; set; }
    public PerfilCliente? PerfilCliente { get; set; }
    public PerfilAgencia? PerfilAgencia { get; set; }
    public ICollection<TrabajoHistorial> TrabajoHistoriales { get; set; } = new List<TrabajoHistorial>();
    public ICollection<Resena> ResenasComoAutor { get; set; } = new List<Resena>();
    public ICollection<Resena> ResenasComoReceptor { get; set; } = new List<Resena>();
}
