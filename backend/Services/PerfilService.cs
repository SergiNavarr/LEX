using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.DTOs;
using Lex.Api.Entities;
using Lex.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Services;

public class PerfilService : IPerfilService
{
    private readonly AppDbContext _db;
    private readonly IServicioService _servicios;
    private readonly IResenaService _resenas;

    public PerfilService(AppDbContext db, IServicioService servicios, IResenaService resenas)
    {
        _db = db;
        _servicios = servicios;
        _resenas = resenas;
    }

    public async Task<IdentidadResponse> ObtenerIdentidadAsync(int usuarioId)
    {
        var usuario = await CargarUsuarioAsync(usuarioId)
            ?? throw new NotFoundException("No se encontró el usuario autenticado.");

        return MapearIdentidad(usuario);
    }

    public async Task<IdentidadResponse> ActivarEstudianteAsync(int usuarioId, ActivarEstudianteRequest request)
    {
        var usuario = await CargarUsuarioAsync(usuarioId)
            ?? throw new NotFoundException("No se encontró el usuario autenticado.");

        // 1) Ya es estudiante -> 400.
        if (usuario.PerfilEstudiante is not null)
            throw new BadRequestException("Ya tenés un perfil de estudiante.");

        // 2) Las agencias no pueden volverse estudiantes -> 403.
        if (usuario.PerfilAgencia is not null || usuario.UsuarioRoles.Any(ur => ur.Rol.Nombre == "Agencia"))
            throw new ForbiddenException("Una agencia no puede ofrecer servicios como estudiante.");

        // 3) Solo Clientes Particulares -> 403 para Empresa o cualquier otro caso.
        if (usuario.PerfilCliente is null || usuario.PerfilCliente.TipoCliente != (int)TipoCliente.Particular)
            throw new ForbiddenException("Solo los clientes particulares pueden ofrecer servicios como estudiantes.");

        // 4) La carrera elegida debe existir en el catalogo.
        var carreraExiste = await _db.Carreras.AnyAsync(c => c.CarreraId == request.CarreraId);
        if (!carreraExiste)
            throw new BadRequestException($"La carrera {request.CarreraId} no existe. Consultá el catálogo en GET /api/catalogo/carreras.");

        // 5) Sumar el rol Estudiante (sin tocar los roles ya existentes).
        var rolEstudiante = await _db.Roles.FirstOrDefaultAsync(r => r.Nombre == "Estudiante")
            ?? throw new InvalidOperationException("El rol 'Estudiante' no existe. ¿Se ejecutó el seeder de roles?");
        usuario.UsuarioRoles.Add(new UsuarioRol { RolId = rolEstudiante.RolId });

        // 6) Crear el perfil de estudiante con sus datos.
        usuario.PerfilEstudiante = new PerfilEstudiante
        {
            Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim(),
            AnioCursado = request.AnioCursado,
            Disponible = true
        };

        // 7) Vincular la carrera. La verificacion institucional real es vision de producto:
        //    en el prototipo el vinculo nace Pendiente.
        usuario.PerfilEstudiante.EstudianteCarreras.Add(new EstudianteCarrera
        {
            CarreraId = request.CarreraId,
            EstadoVerificacion = EstadoVerificacion.Pendiente
        });

        await _db.SaveChangesAsync();

        // Recargar para devolver la identidad completa y consistente.
        var actualizado = await CargarUsuarioAsync(usuarioId);
        return MapearIdentidad(actualizado!);
    }

    public async Task<IReadOnlyList<CarreraCatalogoResponse>> ListarCarrerasAsync()
    {
        return await _db.Carreras.AsNoTracking()
            .OrderBy(c => c.Institucion.Nombre).ThenBy(c => c.Nombre)
            .Select(c => new CarreraCatalogoResponse
            {
                CarreraId = c.CarreraId,
                Nombre = c.Nombre,
                AreaConocimiento = c.AreaConocimiento,
                InstitucionId = c.InstitucionId,
                Institucion = c.Institucion.Nombre,
                Provincia = c.Institucion.Provincia,
                Ciudad = c.Institucion.Ciudad
            })
            .ToListAsync();
    }

    public async Task<PortafolioResponse> ObtenerPortafolioAsync(int estudianteId)
    {
        // Validamos que el usuario exista (404) y que tenga perfil de estudiante (404).
        var usuario = await _db.Usuarios.AsNoTracking()
            .Include(u => u.PerfilEstudiante)
                .ThenInclude(pe => pe!.EstudianteCarreras)
                    .ThenInclude(ec => ec.Carrera)
                        .ThenInclude(c => c.Institucion)
            .FirstOrDefaultAsync(u => u.UsuarioId == estudianteId)
            ?? throw new NotFoundException($"No existe el usuario {estudianteId}.");

        if (usuario.PerfilEstudiante is null)
            throw new NotFoundException($"El usuario {estudianteId} no tiene un perfil de estudiante.");

        var perfil = usuario.PerfilEstudiante;

        // Trabajos completados: contamos directo sobre la tabla (sin depender del contador denormalizado).
        var trabajosCompletados = await _db.Trabajos
            .CountAsync(t => t.EstudianteId == estudianteId && t.Estado == EstadoTrabajo.Completado);

        // Reutilizamos los services existentes para servicios y reseñas.
        var servicios = await _servicios.ListarPorEstudianteAsync(estudianteId);
        var resenas = await _resenas.ListarRecibidasAsync(estudianteId);

        return new PortafolioResponse
        {
            UsuarioId = usuario.UsuarioId,
            NombreCompleto = usuario.NombreCompleto,
            Bio = perfil.Bio,
            AnioCursado = perfil.AnioCursado,
            CalificacionPromedio = perfil.CalificacionPromedio,
            TrabajosCompletados = trabajosCompletados,
            Carreras = perfil.EstudianteCarreras.Select(ec => new CarreraEstudianteResponse
            {
                CarreraId = ec.CarreraId,
                Carrera = ec.Carrera.Nombre,
                Institucion = ec.Carrera.Institucion.Nombre,
                EstadoVerificacion = ec.EstadoVerificacion
            }).ToList(),
            Servicios = servicios.ToList(),
            Resenas = resenas.ToList()
        };
    }

    // Carga el usuario con todo lo necesario para resolver su identidad.
    private Task<Usuario?> CargarUsuarioAsync(int usuarioId) =>
        _db.Usuarios
            .Include(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol)
            .Include(u => u.PerfilCliente)
            .Include(u => u.PerfilAgencia)
            .Include(u => u.PerfilEstudiante)
                .ThenInclude(pe => pe!.EstudianteCarreras)
                    .ThenInclude(ec => ec.Carrera)
                        .ThenInclude(c => c.Institucion)
            .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

    private static IdentidadResponse MapearIdentidad(Usuario usuario)
    {
        TipoCliente? tipoCliente = usuario.PerfilCliente is null
            ? null
            : (TipoCliente)usuario.PerfilCliente.TipoCliente;

        var esEstudiante = usuario.PerfilEstudiante is not null;

        return new IdentidadResponse
        {
            UsuarioId = usuario.UsuarioId,
            Email = usuario.Email,
            NombreCompleto = usuario.NombreCompleto,
            Telefono = usuario.Telefono,
            Roles = usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre).OrderBy(n => n).ToList(),
            TipoCliente = tipoCliente,
            EsEstudiante = esEstudiante,
            PuedeActivarEstudiante = !esEstudiante && tipoCliente == TipoCliente.Particular,
            Carreras = usuario.PerfilEstudiante is null
                ? new List<CarreraEstudianteResponse>()
                : usuario.PerfilEstudiante.EstudianteCarreras.Select(ec => new CarreraEstudianteResponse
                {
                    CarreraId = ec.CarreraId,
                    Carrera = ec.Carrera.Nombre,
                    Institucion = ec.Carrera.Institucion.Nombre,
                    EstadoVerificacion = ec.EstadoVerificacion
                }).ToList()
        };
    }
}
