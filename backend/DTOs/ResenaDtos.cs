using System.ComponentModel.DataAnnotations;

namespace Lex.Api.DTOs;

public class CrearResenaRequest
{
    [Required, Range(1, 5)]
    public int? Puntaje { get; set; }

    [MaxLength(1000)]
    public string? Comentario { get; set; }
}

public class ResenaResponse
{
    public int IdResena { get; set; }
    public int IdTrabajo { get; set; }

    public int AutorUsuarioId { get; set; }
    public string AutorNombre { get; set; } = null!;

    public int ReceptorUsuarioId { get; set; }

    public int Puntaje { get; set; }
    public string? Comentario { get; set; }
    public DateTime Fecha { get; set; }
}
