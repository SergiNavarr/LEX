using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lex.Api.Data;
using Lex.Api.DTOs;
using Lex.Api.Entities;
using Lex.Api.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Lex.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<UsuarioResponse> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim();

        if (await _db.Usuarios.AnyAsync(u => u.Email == email))
            throw new EmailYaRegistradoException(email);

        // El tipo de registro define que rol base y que perfil se crean.
        // Estudiante NUNCA se crea aca: se activa despues sobre un Cliente Particular.
        var nombreRol = request.TipoRegistro switch
        {
            TipoRegistro.ClienteParticular => "Cliente",
            TipoRegistro.ClienteEmpresa => "Cliente",
            TipoRegistro.Agencia => "Agencia",
            _ => throw new TipoRegistroInvalidoException(request.TipoRegistro)
        };

        var rol = await _db.Roles.FirstOrDefaultAsync(r => r.Nombre == nombreRol)
            ?? throw new InvalidOperationException(
                $"El rol '{nombreRol}' no existe. ¿Se ejecuto el seeder de roles?");

        var usuario = new Usuario
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            NombreCompleto = request.NombreCompleto.Trim(),
            Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim(),
            FechaRegistro = DateTime.UtcNow,
            Activo = true
        };

        // Asignacion de rol (usuario_rol) por navegacion: EF completa las FKs.
        usuario.UsuarioRoles.Add(new UsuarioRol { Rol = rol });

        // Perfil correspondiente al tipo de registro.
        switch (request.TipoRegistro)
        {
            case TipoRegistro.ClienteParticular:
                usuario.PerfilCliente = new PerfilCliente
                {
                    TipoCliente = (int)TipoCliente.Particular,
                    DatosParticular = new DatosParticular
                    {
                        Dni = string.IsNullOrWhiteSpace(request.Dni) ? null : request.Dni.Trim()
                    }
                };
                break;

            case TipoRegistro.ClienteEmpresa:
                usuario.PerfilCliente = new PerfilCliente
                {
                    TipoCliente = (int)TipoCliente.Empresa,
                    DatosEmpresa = new DatosEmpresa
                    {
                        RazonSocial = string.IsNullOrWhiteSpace(request.RazonSocial) ? null : request.RazonSocial.Trim(),
                        Cuit = string.IsNullOrWhiteSpace(request.Cuit) ? null : request.Cuit.Trim()
                    }
                };
                break;

            case TipoRegistro.Agencia:
                usuario.PerfilAgencia = new PerfilAgencia
                {
                    NombreAgencia = string.IsNullOrWhiteSpace(request.NombreAgencia) ? null : request.NombreAgencia.Trim()
                };
                break;
        }

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return ToUsuarioResponse(usuario, new List<string> { nombreRol }, usuario.PerfilCliente);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim();

        var usuario = await _db.Usuarios
            .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
            .Include(u => u.PerfilCliente)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (usuario is null || !usuario.Activo ||
            !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
        {
            throw new CredencialesInvalidasException();
        }

        var roles = usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList();
        var (token, expiraEn) = GenerarToken(usuario, roles);

        return new AuthResponse
        {
            Token = token,
            ExpiraEn = expiraEn,
            Usuario = ToUsuarioResponse(usuario, roles, usuario.PerfilCliente)
        };
    }

    private (string token, DateTime expiraEn) GenerarToken(Usuario usuario, IEnumerable<string> roles)
    {
        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiraEn = DateTime.UtcNow.AddMinutes(jwt.GetValue("ExpireMinutes", 120));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.UsuarioId.ToString()),
            new("usuario_id", usuario.UsuarioId.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(ClaimTypes.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expiraEn,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }

    private static UsuarioResponse ToUsuarioResponse(Usuario usuario, List<string> roles, PerfilCliente? perfilCliente) => new()
    {
        UsuarioId = usuario.UsuarioId,
        Email = usuario.Email,
        NombreCompleto = usuario.NombreCompleto,
        Telefono = usuario.Telefono,
        Roles = roles,
        TipoCliente = perfilCliente is null ? null : (TipoCliente)perfilCliente.TipoCliente
    };
}
