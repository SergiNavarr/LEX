using System.ComponentModel.DataAnnotations;

namespace Lex.Api.DTOs;

public class CrearServicioRequest
{
    [Required, MaxLength(150)]
    public string Titulo { get; set; } = null!;

    [MaxLength(1000)]
    public string? Descripcion { get; set; }

    [Required]
    public decimal Precio { get; set; }

    [Required]
    public int TipoServicioId { get; set; }

    public int? TiempoEntregaDias { get; set; }

    // Opcional: URL externa de la imagen de portada. Si no viene, queda null.
    [MaxLength(500)]
    public string? ImagenUrl { get; set; }
}

// Mismos campos editables que en la creacion.
public class ActualizarServicioRequest
{
    [Required, MaxLength(150)]
    public string Titulo { get; set; } = null!;

    [MaxLength(1000)]
    public string? Descripcion { get; set; }

    [Required]
    public decimal Precio { get; set; }

    [Required]
    public int TipoServicioId { get; set; }

    public int? TiempoEntregaDias { get; set; }

    // Opcional: URL externa de la imagen de portada. Si no viene, queda null.
    [MaxLength(500)]
    public string? ImagenUrl { get; set; }
}

public class ServicioResponse
{
    public int IdServicio { get; set; }
    public string Titulo { get; set; } = null!;
    public string? Descripcion { get; set; }
    public string? ImagenUrl { get; set; }
    public decimal Precio { get; set; }
    public int? TiempoEntregaDias { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaPublicacion { get; set; }

    public int TipoServicioId { get; set; }
    public string TipoServicioNombre { get; set; } = null!;

    public int EstudianteId { get; set; }
    public string EstudianteNombre { get; set; } = null!;
    public decimal EstudianteCalificacion { get; set; }
}
