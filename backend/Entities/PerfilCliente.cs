using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lex.Api.Entities;

// PK = usuario_id, FK a usuario. tipo_cliente: 0=Particular, 1=Empresa.
[Table("perfil_cliente")]
public class PerfilCliente
{
    [Key]
    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("tipo_cliente")]
    public int TipoCliente { get; set; }

    // Navegacion
    public Usuario Usuario { get; set; } = null!;
    public DatosParticular? DatosParticular { get; set; }
    public DatosEmpresa? DatosEmpresa { get; set; }
    public ICollection<Solicitud> Solicitudes { get; set; } = new List<Solicitud>();
    public ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();
    public ICollection<Trabajo> Trabajos { get; set; } = new List<Trabajo>();
}
