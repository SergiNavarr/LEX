using System.ComponentModel.DataAnnotations;
using Lex.Api.Enums;

namespace Lex.Api.DTOs;

public class ContratarServicioRequest
{
    [Required]
    public int IdServicio { get; set; }
}

// Flujo 3 (Salud): contratacion directa de un servicio que requiere supervision.
public class ContratarServicioSaludRequest
{
    [Required]
    public int IdServicio { get; set; }

    [Required]
    public int PacienteId { get; set; }

    // Debe venir en true: representa la aceptacion del consentimiento informado.
    public bool ConsentimientoAceptado { get; set; }
}

public class CambiarEstadoRequest
{
    // Nullable + Required: si el body no lo trae, falla con 400 (no asume Pendiente=0).
    [Required]
    public EstadoTrabajo? NuevoEstado { get; set; }

    // Solo aplica al aceptar (Pendiente->Aceptado) un trabajo de Salud: el
    // estudiante indica el profesional matriculado a cargo. Se ignora en el resto.
    [MaxLength(200)]
    public string? SupervisorResponsable { get; set; }
}

public class ConsentimientoResponse
{
    public int IdConsentimiento { get; set; }
    public int? PacienteId { get; set; }
    public string? PacienteNombre { get; set; }
    public string? TextoConsentimiento { get; set; }
    public bool Aceptado { get; set; }
    public DateTime? FechaAceptacion { get; set; }
    public string? SupervisorResponsable { get; set; }
}

public class TrabajoHistorialResponse
{
    public int IdHistorial { get; set; }
    public EstadoTrabajo? EstadoAnterior { get; set; }
    public EstadoTrabajo EstadoNuevo { get; set; }
    public DateTime Fecha { get; set; }
    public int? UsuarioId { get; set; }
}

public class TrabajoResponse
{
    public int IdTrabajo { get; set; }

    public int EstudianteId { get; set; }
    public string EstudianteNombre { get; set; } = null!;

    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = null!;

    public int? TipoServicioId { get; set; }
    public string? TipoServicioNombre { get; set; }

    public OrigenTrabajo Origen { get; set; }
    public int? IdServicio { get; set; }
    public int? IdPostulacion { get; set; }
    public int? PacienteId { get; set; }

    public EstadoTrabajo Estado { get; set; }
    public decimal Monto { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    // Solo presente en trabajos de Salud (los que tienen consentimiento asociado).
    public ConsentimientoResponse? Consentimiento { get; set; }
}
