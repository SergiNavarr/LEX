using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lex.Api.Domain.Enums;

namespace Lex.Api.Domain.Entities;

// Bloque semanal recurrente que el estudiante ofrece para sus servicios de Clase y
// Salud. Es GLOBAL por estudiante (no por servicio): la duracion de cada turno la
// define el servicio que se contrata (DuracionMinutosSesion), no el bloque.
//
// Las horas son locales de Argentina (UTC-3), sin fecha ni zona: "Lunes 14:00-18:00"
// significa lo mismo toda la temporada. La conversion a los instantes UTC concretos de
// cada turno la hace TurnoService al proyectar los slots sobre un rango de fechas.
//
// Baja logica (Activo = false): desactivar un bloque impide reservar turnos NUEVOS en
// ese horario, pero no toca los turnos ya reservados, que son independientes del bloque.
[Table("disponibilidad_estudiante")]
public class DisponibilidadEstudiante
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    // FK a perfil_estudiante.usuario_id (el perfil tiene PK = usuario_id).
    [Column("estudiante_id")]
    public int EstudianteId { get; set; }

    [Column("dia_semana")]
    public DiaSemana DiaSemana { get; set; }

    // TimeOnly -> 'time without time zone' en Postgres: una hora del dia sin fecha asociada.
    [Column("hora_inicio")]
    public TimeOnly HoraInicio { get; set; }

    [Column("hora_fin")]
    public TimeOnly HoraFin { get; set; }

    [Column("activo")]
    public bool Activo { get; set; } = true;

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; }

    // Navegacion
    public PerfilEstudiante Estudiante { get; set; } = null!;
}
