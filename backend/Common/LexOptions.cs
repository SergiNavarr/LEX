namespace Lex.Api.Common;

// Parametros de negocio de la plataforma. Se bindea desde la seccion "Lex" de appsettings.
public class LexOptions
{
    public const string SectionName = "Lex";

    // Take rate de LEX, expresado como porcentaje (10 = 10%). Default 10%.
    public decimal PorcentajeComision { get; set; } = 10m;

    // Credenciales del usuario Admin sembrado para la demo.
    public string AdminEmail { get; set; } = "admin@lex.com";
    public string AdminPassword { get; set; } = "Admin123!";
}
