namespace Lex.Api.Domain.Enums;

/// <summary>Subtipo de un perfil de cliente. (perfil_cliente.tipo_cliente)</summary>
public enum TipoCliente
{
    Particular = 0,
    Empresa = 1
}

/// <summary>Estado de verificacion de la relacion estudiante-carrera. (estudiante_carrera.estado_verificacion)</summary>
public enum EstadoVerificacion
{
    Pendiente = 0,
    Verificado = 1,
    Rechazado = 2
}

/// <summary>Estado de una solicitud de demanda abierta. (solicitud.estado)</summary>
public enum EstadoSolicitud
{
    Abierta = 0,
    Cerrada = 1,
    Cancelada = 2
}

/// <summary>Estado de una postulacion a una solicitud. (postulacion.estado)</summary>
public enum EstadoPostulacion
{
    Enviada = 0,
    Aceptada = 1,
    Rechazada = 2
}

/// <summary>
/// Estado del motor transaccional de un trabajo, unificado para las 3 verticales.
/// Se persiste como string (trabajo.estado). El significado por vertical se documenta
/// en README_ESTADOS_TRABAJO.md. La maquina de estados vive en TrabajoService (Shared).
/// </summary>
public enum EstadoTrabajo
{
    Pendiente = 0,   // Cliente contrato, estudiante aun no acepto
    Aceptado = 1,    // Estudiante acepto, aun no arranco
    EnCurso = 2,     // Trabajo iniciado (Salud requiere consentimiento firmado)
    Entregado = 3,   // Estudiante marco entrega (PC) o completo todas las sesiones (Clase/Salud)
    Completado = 4,  // Cliente confirmo y libero pago
    Cancelado = 5,   // Alguien cancelo antes de completar
    Disputa = 6      // Conflicto que requiere resolucion (admin/mediador)
}

/// <summary>Subtipo de un paciente de Salud. (paciente.tipo). Se persiste como string.</summary>
public enum TipoPaciente
{
    Humano,
    Animal
}

/// <summary>Estado del pago en escrow. (pago.estado). Se persiste como string.</summary>
public enum EstadoPago
{
    Retenido,             // Escrow activo
    ParcialmenteLiberado, // Reservado para Hito 2 (paquetes)
    Liberado,             // Todo liberado + comisión
    Reembolsado,          // Devuelto al cliente
    EnDisputa             // Bloqueado
}

/// <summary>Tipo de asiento en el libro de movimientos de un pago. (movimiento_pago.tipo). Se persiste como string.</summary>
public enum TipoMovimientoPago
{
    Retencion,            // Cliente paga
    LiberacionEstudiante, // Se libera al estudiante
    ComisionLex,          // Comisión LEX
    Reembolso,            // Devuelto al cliente
    Ajuste                // Corrección manual admin
}

/// <summary>
/// Vertical de un servicio. Reemplaza a la vieja tabla lookup 'tipo_servicio':
/// con la jerarquia TPT de Servicio, el tipo queda determinado por la subclase
/// concreta. Se persiste como string donde hace falta guardarlo (catalogo, solicitud).
/// </summary>
public enum TipoServicio
{
    ProyectoCerrado,
    Clase,
    Salud
}

/// <summary>Como entrega el estudiante un proyecto cerrado. (servicio_proyecto_cerrado.formato_entrega)</summary>
public enum FormatoEntrega
{
    Archivos,
    Link,
    Ambos
}

/// <summary>Nivel educativo al que apunta una clase. (servicio_clase.nivel)</summary>
public enum NivelClase
{
    Primario,
    Secundario,
    Universitario,
    Adulto,
    Idioma,
    Otro
}

/// <summary>Modalidad de cursada de una clase. (servicio_clase.modalidad)</summary>
public enum ModalidadClase
{
    Online,
    Presencial,
    Ambas
}

/// <summary>Donde se presta un servicio de salud. (servicio_salud.modalidad)</summary>
public enum ModalidadSalud
{
    Domicilio,
    Consultorio,
    Ambas
}

/// <summary>
/// Dia de la semana de un bloque de disponibilidad. (disponibilidad_estudiante.dia_semana).
/// Se persiste como string, asi que el valor subyacente no llega a la DB: arranca en 0
/// como cualquier enum de C#. El orden si importa en memoria (la semana se lista de lunes
/// a domingo); no confundir con System.DayOfWeek, que arranca en domingo. La conversion
/// entre ambos vive en TurnoService.
/// </summary>
public enum DiaSemana
{
    Lunes = 0,
    Martes = 1,
    Miercoles = 2,
    Jueves = 3,
    Viernes = 4,
    Sabado = 5,
    Domingo = 6
}

/// <summary>Estado de un turno agendado. (turno.estado). Se persiste como string.</summary>
public enum EstadoTurno
{
    Reservado,   // Cliente reservo, el estudiante aun no confirmo
    Confirmado,  // Estudiante confirmo (o auto-confirmado al contratar)
    Realizado,   // Turno cumplido
    Cancelado,   // Cancelado antes de la fecha
    Ausente      // El cliente no se presento
}

/// <summary>Estado de una sesion dentro de un trabajo. (sesion.estado). Se persiste como string.</summary>
public enum EstadoSesion
{
    Pendiente,  // Turno programado, aun no ocurrio
    Realizada,  // El estudiante la marco completada (dispara liberacion fraccionada; Hito 2 Parte 3)
    Cancelada,  // El turno se cancelo antes de realizarse
    NoAsistio   // El cliente no se presento (el estudiante decide si igual libera el pago)
}
