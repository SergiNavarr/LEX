using System.ComponentModel.DataAnnotations.Schema;
using Lex.Api.Enums;

namespace Lex.Api.Entities;

// Clave primaria compuesta (estudiante_id, carrera_id) — configurada en AppDbContext.
[Table("estudiante_carrera")]
public class EstudianteCarrera
{
    [Column("estudiante_id")]
    public int EstudianteId { get; set; }

    [Column("carrera_id")]
    public int CarreraId { get; set; }

    [Column("estado_verificacion")]
    public EstadoVerificacion EstadoVerificacion { get; set; } = EstadoVerificacion.Pendiente;

    [Column("fecha_verificacion")]
    public DateTime? FechaVerificacion { get; set; }

    [Column("documento_comprobante")]
    public string? DocumentoComprobante { get; set; }

    // Navegacion
    public PerfilEstudiante Estudiante { get; set; } = null!;
    public Carrera Carrera { get; set; } = null!;
}
