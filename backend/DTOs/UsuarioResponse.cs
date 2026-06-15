using Lex.Api.Enums;

namespace Lex.Api.DTOs;

// Representacion segura de un usuario: nunca incluye el password_hash.
public class UsuarioResponse
{
    public int UsuarioId { get; set; }
    public string Email { get; set; } = null!;
    public string NombreCompleto { get; set; } = null!;
    public string? Telefono { get; set; }
    public List<string> Roles { get; set; } = new();

    // Solo si el usuario es Cliente: su subtipo (Particular / Empresa). null en otro caso.
    public TipoCliente? TipoCliente { get; set; }
}
