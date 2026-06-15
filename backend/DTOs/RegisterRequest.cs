using System.ComponentModel.DataAnnotations;

namespace Lex.Api.DTOs;

// Tipo de registro segun el modelo de identidad de LEX.
//   - Una persona se registra como Cliente (Particular o Empresa) o como Agencia.
//   - Estudiante NO es una opcion de registro: se activa despues, y solo si el
//     usuario es un Cliente Particular (POST /api/perfil/activar-estudiante).
public enum TipoRegistro
{
    ClienteParticular,
    ClienteEmpresa,
    Agencia
}

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = null!;

    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; set; } = null!;

    [Required, MaxLength(150)]
    public string NombreCompleto { get; set; } = null!;

    [MaxLength(30)]
    public string? Telefono { get; set; }

    [Required]
    public TipoRegistro TipoRegistro { get; set; }

    // --- Datos opcionales segun el tipo de registro (se pueden completar despues) ---

    // ClienteParticular
    [MaxLength(20)]
    public string? Dni { get; set; }

    // ClienteEmpresa
    [MaxLength(150)]
    public string? RazonSocial { get; set; }

    [MaxLength(20)]
    public string? Cuit { get; set; }

    // Agencia
    [MaxLength(150)]
    public string? NombreAgencia { get; set; }
}
