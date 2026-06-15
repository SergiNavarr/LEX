using System.ComponentModel.DataAnnotations;
using Lex.Api.Enums;

namespace Lex.Api.DTOs;

// Body de POST /api/perfil/activar-estudiante.
// Para esta etapa de prototipo, la institucion/carrera se elige por id de una
// carrera ya existente (sembrada en el catalogo). El vinculo nace Pendiente de
// verificacion: la validacion institucional real es vision de producto.
public class ActivarEstudianteRequest
{
    [Required]
    public int CarreraId { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    [Range(1, 10)]
    public int? AnioCursado { get; set; }
}

// Respuesta de GET /api/perfil/yo: identidad completa del usuario autenticado.
// El frontend la usa para decidir que vistas mostrar.
public class IdentidadResponse
{
    public int UsuarioId { get; set; }
    public string Email { get; set; } = null!;
    public string NombreCompleto { get; set; } = null!;
    public string? Telefono { get; set; }
    public List<string> Roles { get; set; } = new();

    // Solo si es Cliente.
    public TipoCliente? TipoCliente { get; set; }

    // true si el usuario ya activo su perfil de estudiante.
    public bool EsEstudiante { get; set; }

    // true si puede activar el perfil de estudiante (Cliente Particular que aun no lo activo).
    public bool PuedeActivarEstudiante { get; set; }

    // Carreras a las que esta vinculado como estudiante (vacio si no es estudiante).
    public List<CarreraEstudianteResponse> Carreras { get; set; } = new();
}

public class CarreraEstudianteResponse
{
    public int CarreraId { get; set; }
    public string Carrera { get; set; } = null!;
    public string Institucion { get; set; } = null!;
    public EstadoVerificacion EstadoVerificacion { get; set; }
}

// Item del catalogo de carreras para poblar los selectores del frontend.
public class CarreraCatalogoResponse
{
    public int CarreraId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? AreaConocimiento { get; set; }
    public int InstitucionId { get; set; }
    public string Institucion { get; set; } = null!;
    public string? Provincia { get; set; }
    public string? Ciudad { get; set; }
}
