namespace Lex.Api.Enums;

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

/// <summary>Origen del que nace un trabajo. (trabajo.origen)</summary>
public enum OrigenTrabajo
{
    Directo = 0,      // desde un servicio
    Postulacion = 1   // desde una postulacion aceptada
}

/// <summary>Estado del motor transaccional de un trabajo. (trabajo.estado)</summary>
public enum EstadoTrabajo
{
    Pendiente = 0,
    Aceptado = 1,
    EnCurso = 2,
    Completado = 3,
    Cancelado = 4
}

/// <summary>Estado del pago en escrow. (pago.estado)</summary>
public enum EstadoPago
{
    Retenido = 0,
    Liberado = 1,
    Reembolsado = 2
}
