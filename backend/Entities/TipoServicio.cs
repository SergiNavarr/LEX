using System.ComponentModel.DataAnnotations.Schema;

namespace Lex.Api.Entities;

[Table("tipo_servicio")]
public class TipoServicio
{
    [Column("tipo_servicio_id")]
    public int TipoServicioId { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = null!; // Digital, Clase, Salud, Otro

    [Column("requiere_supervision")]
    public bool RequiereSupervision { get; set; } // activa logica de Salud

    // Navegacion
    public ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
    public ICollection<Solicitud> Solicitudes { get; set; } = new List<Solicitud>();
    public ICollection<Trabajo> Trabajos { get; set; } = new List<Trabajo>();
}
