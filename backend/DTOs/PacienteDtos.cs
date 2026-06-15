using System.ComponentModel.DataAnnotations;

namespace Lex.Api.DTOs;

public class CrearPacienteRequest
{
    [Required, MaxLength(150)]
    public string NombreCompleto { get; set; } = null!;

    [Range(0, 130)]
    public int? Edad { get; set; }

    [MaxLength(1000)]
    public string? Notas { get; set; }
}

public class PacienteResponse
{
    public int PacienteId { get; set; }
    public int ClienteId { get; set; }
    public string NombreCompleto { get; set; } = null!;
    public int? Edad { get; set; }
    public string? Notas { get; set; }
}
