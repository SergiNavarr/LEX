using Lex.Api.DTOs;

namespace Lex.Api.Services;

public interface IDemoService
{
    // Borra los datos demo previos y vuelve a cargarlos (idempotente / reseteable).
    Task<DemoSeedResponse> SeedAsync();

    // Solo limpia los datos demo, sin recargarlos.
    Task<DemoResetResponse> ResetAsync();
}
