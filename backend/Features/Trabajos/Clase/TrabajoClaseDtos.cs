using System.ComponentModel.DataAnnotations;
using Lex.Api.Domain.Enums;
using Lex.Api.Features.Sesiones;
using Lex.Api.Features.Trabajos.Shared;

namespace Lex.Api.Features.Trabajos.Clase;

public class ContratarTrabajoClaseRequest
{
    [Required]
    public int ServicioId { get; set; }

    // Fechas de inicio de cada turno, en UTC. Tienen que ser exactamente tantas como
    // sesiones tenga el servicio: N si es paquete de N, 1 si es una clase suelta. No se
    // aceptan paquetes a medio agendar.
    [Required]
    public List<DateTime> SlotsElegidos { get; set; } = new();

    // Contexto opcional para el estudiante ("vengo flojo en integrales").
    public string? NotasCliente { get; set; }
}

public class TrabajoClaseDetalle
{
    public string MateriaSnapshot { get; set; } = null!;
    public NivelClase NivelSnapshot { get; set; }
    public ModalidadClase ModalidadSnapshot { get; set; }
    public int DuracionMinutosSesionSnapshot { get; set; }
    public bool EsPaqueteSnapshot { get; set; }
    public int CantidadSesionesTotales { get; set; }
    public int SesionesCompletadas { get; set; }
}

public class TrabajoClaseResponse : TrabajoResponse
{
    public string MateriaSnapshot { get; set; } = null!;
    public NivelClase NivelSnapshot { get; set; }
    public ModalidadClase ModalidadSnapshot { get; set; }
    public int DuracionMinutosSesionSnapshot { get; set; }
    public bool EsPaqueteSnapshot { get; set; }
    public int CantidadSesionesTotales { get; set; }
    public int SesionesCompletadas { get; set; }

    // Agenda del trabajo: una sesion por turno reservado, en orden de paquete.
    public List<SesionResponse> Sesiones { get; set; } = new();
}
