using System.Security.Claims;

namespace Lex.Api.Extensions;

// Acceso reutilizable a la identidad del usuario autenticado desde los claims del JWT.
public static class ClaimsPrincipalExtensions
{
    /// <summary>usuario_id del token. Lanza si el token no lo trae (no deberia pasar tras [Authorize]).</summary>
    public static int GetUsuarioId(this ClaimsPrincipal user)
    {
        var valor = user.FindFirst("usuario_id")?.Value;
        if (!int.TryParse(valor, out var id))
            throw new UnauthorizedAccessException("No se pudo determinar el usuario autenticado.");
        return id;
    }

    /// <summary>Lista de roles (claims de tipo Role) del usuario.</summary>
    public static IReadOnlyList<string> GetRoles(this ClaimsPrincipal user) =>
        user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

    public static bool EsEstudiante(this ClaimsPrincipal user) => user.IsInRole("Estudiante");
    public static bool EsCliente(this ClaimsPrincipal user) => user.IsInRole("Cliente");
    public static bool EsAgencia(this ClaimsPrincipal user) => user.IsInRole("Agencia");
}
