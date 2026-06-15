using System.ComponentModel.DataAnnotations.Schema;

namespace Lex.Api.Entities;

// Tabla 'pacientes'.
[Table("pacientes")]
public class Paciente
{
    [Column("paciente_id")]
    public int PacienteId { get; set; }

    [Column("cliente_id")]
    public int ClienteId { get; set; }

    [Column("nombre_completo")]
    public string NombreCompleto { get; set; } = null!;

    [Column("edad")]
    public int? Edad { get; set; }

    [Column("notas")]
    public string? Notas { get; set; }

    // Navegacion
    public PerfilCliente Cliente { get; set; } = null!;
    public ICollection<Trabajo> Trabajos { get; set; } = new List<Trabajo>();
    public ICollection<Consentimiento> Consentimientos { get; set; } = new List<Consentimiento>();
}
