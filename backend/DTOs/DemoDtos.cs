namespace Lex.Api.DTOs;

// Resumen de lo que dejó cargado (o borrado) el seeder de demostración.
public class DemoSeedResponse
{
    public string Mensaje { get; set; } = null!;

    // Password único de todos los usuarios demo (para presentar en vivo).
    public string PasswordDemo { get; set; } = "Demo1234";

    // Conteos por tipo de registro creado.
    public int Estudiantes { get; set; }
    public int ClientesParticulares { get; set; }
    public int Empresas { get; set; }
    public int Agencias { get; set; }
    public int Pacientes { get; set; }
    public int Servicios { get; set; }
    public int Trabajos { get; set; }
    public int Resenas { get; set; }

    // Trabajos discriminados por estado (Pendiente, Aceptado, EnCurso, Completado...).
    public Dictionary<string, int> TrabajosPorEstado { get; set; } = new();

    // Coherencia del escrow: comisión efectiva (liberada) vs. potencial (retenida).
    public decimal ComisionLexLiberada { get; set; }
    public decimal ComisionLexRetenida { get; set; }

    // Algunos emails listos para copiar/pegar en el login de la demo.
    public List<string> EmailsEjemplo { get; set; } = new();
}

public class DemoResetResponse
{
    public string Mensaje { get; set; } = null!;
    public int UsuariosEliminados { get; set; }
}
