namespace Lex.Api.DTOs;

// Respuesta del login: el JWT mas los datos publicos del usuario.
public class AuthResponse
{
    public string Token { get; set; } = null!;
    public DateTime ExpiraEn { get; set; }
    public UsuarioResponse Usuario { get; set; } = null!;
}
