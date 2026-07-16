using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lex.Api.Domain.Enums;

namespace Lex.Api.Domain.Entities;

// Instancia concreta de agenda: un hueco de la disponibilidad del estudiante ya tomado
// por un cliente, con fecha, hora y duracion propias. El turno NO cuelga del bloque de
// disponibilidad que lo origino: una vez reservado vive por su cuenta, asi que dar de
// baja el bloque no lo afecta.
//
// La duracion se copia al turno en vez de leerse del servicio porque el turno tiene que
// sobrevivir a que el estudiante edite la duracion de sus sesiones despues de agendar.
//
// Un turno de Clase/Salud tiene ademas una Sesion asociada que lo ata a su trabajo
// (relacion 1-1). La creacion de turnos con su sesion llega en Hito 2 Parte 2.
[Table("turno")]
public class Turno
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    // FK a perfil_estudiante.usuario_id.
    [Column("estudiante_id")]
    public int EstudianteId { get; set; }

    // FK a perfil_cliente.usuario_id.
    [Column("cliente_id")]
    public int ClienteId { get; set; }

    // Instante de inicio en UTC (timestamptz). La hora local Argentina se deriva en
    // la capa de presentacion; la DB no guarda offsets locales.
    [Column("fecha_hora_inicio")]
    public DateTime FechaHoraInicio { get; set; }

    [Column("duracion_minutos")]
    public int DuracionMinutos { get; set; }

    [Column("estado")]
    public EstadoTurno Estado { get; set; } = EstadoTurno.Reservado;

    [Column("link_videollamada")]
    public string? LinkVideollamada { get; set; }

    [Column("notas_estudiante")]
    public string? NotasEstudiante { get; set; }

    [Column("notas_cliente")]
    public string? NotasCliente { get; set; }

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; }

    // Navegacion
    public PerfilEstudiante Estudiante { get; set; } = null!;
    public PerfilCliente Cliente { get; set; } = null!;
    public Sesion? Sesion { get; set; }   // 1->1: a lo sumo una sesion por turno
}
