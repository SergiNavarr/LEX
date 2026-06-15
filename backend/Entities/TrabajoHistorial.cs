using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lex.Api.Enums;

namespace Lex.Api.Entities;

// Trazabilidad de cambios de estado de cada trabajo.
[Table("trabajo_historial")]
public class TrabajoHistorial
{
    [Key]
    [Column("id_historial")]
    public int IdHistorial { get; set; }

    [Column("id_trabajo")]
    public int IdTrabajo { get; set; }

    [Column("estado_anterior")]
    public EstadoTrabajo? EstadoAnterior { get; set; }

    [Column("estado_nuevo")]
    public EstadoTrabajo EstadoNuevo { get; set; }

    [Column("fecha")]
    public DateTime Fecha { get; set; }

    [Column("usuario_id")]
    public int? UsuarioId { get; set; } // quien provoco el cambio

    // Navegacion
    public Trabajo Trabajo { get; set; } = null!;
    public Usuario? Usuario { get; set; }
}
