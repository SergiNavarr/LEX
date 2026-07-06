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

// Respuesta de GET /api/estudiantes/{id}/portafolio: arma en una sola llamada
// todo lo que el frontend necesita para la "carta de presentación" pública del
// estudiante (perfil + verificación institucional + servicios + reseñas).
public class PortafolioResponse
{
    public int UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = null!;
    public string? Bio { get; set; }
    public int? AnioCursado { get; set; }
    public decimal CalificacionPromedio { get; set; }
    public int TrabajosCompletados { get; set; }

    // Vínculos institucionales con su estado de verificación (el sello de confianza).
    public List<CarreraEstudianteResponse> Carreras { get; set; } = new();

    // Servicios activos (mismo DTO que el listado público -> el front reusa ServiceCard).
    public List<ServicioResponse> Servicios { get; set; } = new();

    // Reseñas recibidas (mismo DTO que GET /api/usuarios/{id}/resenas).
    public List<ResenaResponse> Resenas { get; set; } = new();
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
