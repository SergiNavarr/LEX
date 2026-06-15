using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.DTOs;
using Lex.Api.Entities;
using Lex.Api.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lex.Api.Services;

// Rol de un usuario dentro de un trabajo concreto (no es el rol global del sistema).
public enum ParteTrabajo
{
    Estudiante,
    Cliente
}

public class TrabajoService : ITrabajoService
{
    private readonly AppDbContext _db;
    private readonly LexOptions _lex;

    public TrabajoService(AppDbContext db, IOptions<LexOptions> lexOptions)
    {
        _db = db;
        _lex = lexOptions.Value;
    }

    // Texto estandar del consentimiento informado para servicios de Salud.
    private const string TextoConsentimientoSalud =
        "El cliente declara haber sido informado sobre la naturaleza del servicio de salud " +
        "contratado, sus alcances y limitaciones, y presta su consentimiento para que el " +
        "estudiante lo realice bajo la supervisión de un profesional matriculado responsable. " +
        "LEX actúa únicamente como plataforma de intermediación.";

    public async Task<TrabajoResponse> ContratarServicioAsync(int clienteId, ContratarServicioRequest request)
    {
        // Solo un Cliente puede contratar: debe tener perfil de cliente.
        var esCliente = await _db.PerfilesCliente.AnyAsync(p => p.UsuarioId == clienteId);
        if (!esCliente)
            throw new ForbiddenException("Solo los clientes pueden contratar servicios.");

        var servicio = await _db.Servicios.FirstOrDefaultAsync(s => s.IdServicio == request.IdServicio)
            ?? throw new NotFoundException($"No existe el servicio {request.IdServicio}.");

        if (!servicio.Activo)
            throw new BadRequestException("El servicio no está disponible para contratar.");

        if (servicio.EstudianteId == clienteId)
            throw new BadRequestException("No podés contratar tu propio servicio.");

        var ahora = DateTime.UtcNow;
        var trabajo = new Trabajo
        {
            EstudianteId = servicio.EstudianteId,
            ClienteId = clienteId,
            TipoServicioId = servicio.TipoServicioId,   // se copia del servicio
            Origen = OrigenTrabajo.Directo,
            IdServicio = servicio.IdServicio,
            IdPostulacion = null,
            PacienteId = null,
            Estado = EstadoTrabajo.Pendiente,
            Monto = servicio.Precio,
            FechaCreacion = ahora
        };

        // Primer movimiento en el historial (estado_anterior = null).
        trabajo.Historiales.Add(new TrabajoHistorial
        {
            EstadoAnterior = null,
            EstadoNuevo = EstadoTrabajo.Pendiente,
            Fecha = ahora,
            UsuarioId = clienteId
        });

        _db.Trabajos.Add(trabajo);
        await _db.SaveChangesAsync();

        return await ObtenerInternoAsync(trabajo.IdTrabajo);
    }

    public async Task<TrabajoResponse> ContratarServicioSaludAsync(int clienteId, ContratarServicioSaludRequest request)
    {
        // Solo un Cliente puede contratar: debe tener perfil de cliente.
        var esCliente = await _db.PerfilesCliente.AnyAsync(p => p.UsuarioId == clienteId);
        if (!esCliente)
            throw new ForbiddenException("Solo los clientes pueden contratar servicios.");

        // 1) El servicio debe existir.
        var servicio = await _db.Servicios
            .Include(s => s.TipoServicio)
            .FirstOrDefaultAsync(s => s.IdServicio == request.IdServicio)
            ?? throw new NotFoundException($"No existe el servicio {request.IdServicio}.");

        // 2) Su tipo de servicio debe requerir supervision (area Salud).
        if (!servicio.TipoServicio.RequiereSupervision)
            throw new BadRequestException("Este servicio no es del área de salud, usá el endpoint de contratación normal.");

        if (!servicio.Activo)
            throw new BadRequestException("El servicio no está disponible para contratar.");

        if (servicio.EstudianteId == clienteId)
            throw new BadRequestException("No podés contratar tu propio servicio.");

        // 3) El paciente debe existir y pertenecer al cliente autenticado.
        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.PacienteId == request.PacienteId)
            ?? throw new NotFoundException($"No existe el paciente {request.PacienteId}.");
        if (paciente.ClienteId != clienteId)
            throw new ForbiddenException("Este paciente no te pertenece.");

        // 4) El consentimiento informado debe estar aceptado.
        if (!request.ConsentimientoAceptado)
            throw new BadRequestException("Debe aceptar el consentimiento informado para contratar un servicio de salud.");

        var ahora = DateTime.UtcNow;
        var trabajo = new Trabajo
        {
            EstudianteId = servicio.EstudianteId,
            ClienteId = clienteId,
            TipoServicioId = servicio.TipoServicioId,   // se copia del servicio
            Origen = OrigenTrabajo.Directo,
            IdServicio = servicio.IdServicio,
            IdPostulacion = null,
            PacienteId = paciente.PacienteId,
            Estado = EstadoTrabajo.Pendiente,
            Monto = servicio.Precio,
            FechaCreacion = ahora
        };

        // Consentimiento asociado (1->1 con el trabajo). El supervisor se asigna
        // mas adelante, cuando el estudiante acepta el trabajo.
        trabajo.Consentimiento = new Consentimiento
        {
            PacienteId = paciente.PacienteId,
            TextoConsentimiento = TextoConsentimientoSalud,
            Aceptado = true,
            FechaAceptacion = ahora,
            SupervisorResponsable = null
        };

        // Primer movimiento en el historial (estado_anterior = null).
        trabajo.Historiales.Add(new TrabajoHistorial
        {
            EstadoAnterior = null,
            EstadoNuevo = EstadoTrabajo.Pendiente,
            Fecha = ahora,
            UsuarioId = clienteId
        });

        _db.Trabajos.Add(trabajo);
        await _db.SaveChangesAsync();

        return await ObtenerInternoAsync(trabajo.IdTrabajo);
    }

    public async Task<IReadOnlyList<TrabajoResponse>> ListarMiosAsync(int usuarioId)
    {
        // Devuelve los trabajos donde el usuario participa, sea como estudiante o como cliente.
        return await _db.Trabajos.AsNoTracking()
            .Where(t => t.EstudianteId == usuarioId || t.ClienteId == usuarioId)
            .OrderByDescending(t => t.FechaCreacion)
            .Select(Proyeccion)
            .ToListAsync();
    }

    public async Task<TrabajoResponse> ObtenerAsync(int usuarioId, int idTrabajo)
    {
        var trabajo = await _db.Trabajos.AsNoTracking()
            .Where(t => t.IdTrabajo == idTrabajo)
            .Select(t => new { t.EstudianteId, t.ClienteId })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"No existe el trabajo {idTrabajo}.");

        if (trabajo.EstudianteId != usuarioId && trabajo.ClienteId != usuarioId)
            throw new ForbiddenException("No participás en este trabajo.");

        return await ObtenerInternoAsync(idTrabajo);
    }

    public async Task<TrabajoResponse> CambiarEstadoAsync(int usuarioId, int idTrabajo, EstadoTrabajo nuevoEstado, string? supervisorResponsable = null)
    {
        var trabajo = await _db.Trabajos
            .Include(t => t.Consentimiento)
            .Include(t => t.Pago)
            .FirstOrDefaultAsync(t => t.IdTrabajo == idTrabajo)
            ?? throw new NotFoundException($"No existe el trabajo {idTrabajo}.");

        // El usuario debe ser parte del trabajo (estudiante o cliente).
        var parte = ParteDe(usuarioId, trabajo)
            ?? throw new ForbiddenException("No participás en este trabajo.");

        // La transicion debe existir en la maquina de estados...
        var partesAutorizadas = PartesAutorizadas(trabajo.Estado, nuevoEstado);
        if (partesAutorizadas is null)
            throw new BadRequestException($"No se puede pasar de {trabajo.Estado} a {nuevoEstado}.");

        // ...y ser permitida para el rol del usuario dentro del trabajo.
        if (!partesAutorizadas.Contains(parte))
            throw new ForbiddenException(
                $"La transición {trabajo.Estado} → {nuevoEstado} solo puede hacerla {DescribirPartes(partesAutorizadas)} del trabajo.");

        // Salud: al aceptar (Pendiente->Aceptado) un trabajo con consentimiento,
        // el estudiante debe indicar el supervisor responsable (profesional matriculado).
        var esAceptacionSalud = nuevoEstado == EstadoTrabajo.Aceptado && trabajo.Consentimiento is not null;
        if (esAceptacionSalud && string.IsNullOrWhiteSpace(supervisorResponsable))
            throw new BadRequestException("Debe indicar el supervisor responsable para aceptar un trabajo de salud.");

        var estadoAnterior = trabajo.Estado;
        var ahora = DateTime.UtcNow;

        // Se persiste el supervisor en el mismo paso que la aceptacion del trabajo de salud.
        if (esAceptacionSalud)
            trabajo.Consentimiento!.SupervisorResponsable = supervisorResponsable!.Trim();

        trabajo.Estado = nuevoEstado;
        if (nuevoEstado == EstadoTrabajo.EnCurso)
            trabajo.FechaInicio = ahora;
        if (nuevoEstado is EstadoTrabajo.Completado or EstadoTrabajo.Cancelado)
            trabajo.FechaFin = ahora;

        // --- Escrow: el pago sigue al ciclo de vida del trabajo, en la misma transaccion ---
        AplicarEscrow(trabajo, estadoAnterior, nuevoEstado, ahora);

        trabajo.Historiales.Add(new TrabajoHistorial
        {
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = nuevoEstado,
            Fecha = ahora,
            UsuarioId = usuarioId
        });

        // Al completar, sube el contador de trabajos del estudiante.
        // (La calificacion promedio queda para el modulo de resenas.)
        if (nuevoEstado == EstadoTrabajo.Completado)
        {
            var perfil = await _db.PerfilesEstudiante
                .FirstOrDefaultAsync(p => p.UsuarioId == trabajo.EstudianteId);
            if (perfil is not null)
                perfil.CantidadTrabajos += 1;
        }

        await _db.SaveChangesAsync();
        return await ObtenerInternoAsync(idTrabajo);
    }

    public async Task<IReadOnlyList<TrabajoHistorialResponse>> ListarHistorialAsync(int usuarioId, int idTrabajo)
    {
        var trabajo = await _db.Trabajos.AsNoTracking()
            .Where(t => t.IdTrabajo == idTrabajo)
            .Select(t => new { t.EstudianteId, t.ClienteId })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"No existe el trabajo {idTrabajo}.");

        if (trabajo.EstudianteId != usuarioId && trabajo.ClienteId != usuarioId)
            throw new ForbiddenException("No participás en este trabajo.");

        return await _db.TrabajoHistoriales.AsNoTracking()
            .Where(h => h.IdTrabajo == idTrabajo)
            .OrderBy(h => h.Fecha)
            .Select(h => new TrabajoHistorialResponse
            {
                IdHistorial = h.IdHistorial,
                EstadoAnterior = h.EstadoAnterior,
                EstadoNuevo = h.EstadoNuevo,
                Fecha = h.Fecha,
                UsuarioId = h.UsuarioId
            })
            .ToListAsync();
    }

    // --- Escrow + take rate -------------------------------------------------
    // Mueve el pago segun la transicion del trabajo. Los calculos de comision
    // se hacen en C# (decimal): SQLite guarda decimales como texto.
    private void AplicarEscrow(Trabajo trabajo, EstadoTrabajo desde, EstadoTrabajo hasta, DateTime ahora)
    {
        // Pendiente -> Aceptado: el cliente "paga" y LEX retiene en escrow.
        if (desde == EstadoTrabajo.Pendiente && hasta == EstadoTrabajo.Aceptado && trabajo.Pago is null)
        {
            var porcentaje = _lex.PorcentajeComision;                 // ej. 10 (= 10%)
            var comision = Math.Round(trabajo.Monto * porcentaje / 100m, 2, MidpointRounding.AwayFromZero);
            var montoEstudiante = trabajo.Monto - comision;           // así comision + estudiante == monto

            trabajo.Pago = new Pago
            {
                MontoTotal = trabajo.Monto,
                PorcentajeComision = porcentaje,                      // registro historico del % aplicado
                ComisionLex = comision,
                MontoEstudiante = montoEstudiante,
                Estado = EstadoPago.Retenido,
                FechaRetencion = ahora
            };
            return;
        }

        if (trabajo.Pago is null)
            return;

        // EnCurso -> Completado: LEX libera el dinero al estudiante (se queda la comision).
        if (desde == EstadoTrabajo.EnCurso && hasta == EstadoTrabajo.Completado &&
            trabajo.Pago.Estado == EstadoPago.Retenido)
        {
            trabajo.Pago.Estado = EstadoPago.Liberado;
            trabajo.Pago.FechaLiberacion = ahora;
            return;
        }

        // Cualquier transicion a Cancelado: si habia plata retenida, se reembolsa al cliente.
        if (hasta == EstadoTrabajo.Cancelado && trabajo.Pago.Estado == EstadoPago.Retenido)
        {
            trabajo.Pago.Estado = EstadoPago.Reembolsado;
        }
    }

    // --- Maquina de estados -------------------------------------------------
    // Que rol del trabajo puede provocar cada transicion. Si la combinacion
    // (estado actual -> nuevo) no esta aca, la transicion no existe.
    private static ParteTrabajo[]? PartesAutorizadas(EstadoTrabajo desde, EstadoTrabajo hasta) =>
        (desde, hasta) switch
        {
            (EstadoTrabajo.Pendiente, EstadoTrabajo.Aceptado) => new[] { ParteTrabajo.Estudiante },
            (EstadoTrabajo.Aceptado, EstadoTrabajo.EnCurso) => new[] { ParteTrabajo.Estudiante },
            (EstadoTrabajo.EnCurso, EstadoTrabajo.Completado) => new[] { ParteTrabajo.Cliente },
            (EstadoTrabajo.Pendiente, EstadoTrabajo.Cancelado) => new[] { ParteTrabajo.Estudiante, ParteTrabajo.Cliente },
            (EstadoTrabajo.Aceptado, EstadoTrabajo.Cancelado) => new[] { ParteTrabajo.Estudiante, ParteTrabajo.Cliente },
            _ => null
        };

    private static ParteTrabajo? ParteDe(int usuarioId, Trabajo t) =>
        usuarioId == t.EstudianteId ? ParteTrabajo.Estudiante
        : usuarioId == t.ClienteId ? ParteTrabajo.Cliente
        : null;

    private static string DescribirPartes(IEnumerable<ParteTrabajo> partes) =>
        string.Join(" o ", partes.Select(p => p == ParteTrabajo.Estudiante ? "el estudiante" : "el cliente"));

    private async Task<TrabajoResponse> ObtenerInternoAsync(int idTrabajo)
    {
        return await _db.Trabajos.AsNoTracking()
            .Where(t => t.IdTrabajo == idTrabajo)
            .Select(Proyeccion)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"No existe el trabajo {idTrabajo}.");
    }

    private static readonly System.Linq.Expressions.Expression<Func<Trabajo, TrabajoResponse>> Proyeccion =
        t => new TrabajoResponse
        {
            IdTrabajo = t.IdTrabajo,
            EstudianteId = t.EstudianteId,
            EstudianteNombre = t.Estudiante.Usuario.NombreCompleto,
            ClienteId = t.ClienteId,
            ClienteNombre = t.Cliente.Usuario.NombreCompleto,
            TipoServicioId = t.TipoServicioId,
            TipoServicioNombre = t.TipoServicio != null ? t.TipoServicio.Nombre : null,
            Origen = t.Origen,
            IdServicio = t.IdServicio,
            IdPostulacion = t.IdPostulacion,
            PacienteId = t.PacienteId,
            Estado = t.Estado,
            Monto = t.Monto,
            FechaCreacion = t.FechaCreacion,
            FechaInicio = t.FechaInicio,
            FechaFin = t.FechaFin,
            Consentimiento = t.Consentimiento == null ? null : new ConsentimientoResponse
            {
                IdConsentimiento = t.Consentimiento.IdConsentimiento,
                PacienteId = t.Consentimiento.PacienteId,
                PacienteNombre = t.Consentimiento.Paciente != null ? t.Consentimiento.Paciente.NombreCompleto : null,
                TextoConsentimiento = t.Consentimiento.TextoConsentimiento,
                Aceptado = t.Consentimiento.Aceptado,
                FechaAceptacion = t.Consentimiento.FechaAceptacion,
                SupervisorResponsable = t.Consentimiento.SupervisorResponsable
            }
        };
}
