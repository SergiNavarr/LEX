using Lex.Api.DTOs;

namespace Lex.Api.Services;

public interface IAuthService
{
    Task<UsuarioResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
