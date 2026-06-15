using Lex.Api.DTOs;
using Lex.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Registra un usuario, le asigna el rol y crea su perfil vacio.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var usuario = await _auth.RegisterAsync(request);
            return CreatedAtAction(nameof(Register), new { id = usuario.UsuarioId }, usuario);
        }
        catch (EmailYaRegistradoException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (TipoRegistroInvalidoException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Valida email + password y devuelve un JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            return Ok(await _auth.LoginAsync(request));
        }
        catch (CredencialesInvalidasException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
}
