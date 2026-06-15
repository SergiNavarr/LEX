using Lex.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // --- Bloque 1: Identidad y roles ---
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();

    // --- Bloque 2: Validacion institucional ---
    public DbSet<TipoInstitucion> TiposInstitucion => Set<TipoInstitucion>();
    public DbSet<Institucion> Instituciones => Set<Institucion>();
    public DbSet<Carrera> Carreras => Set<Carrera>();

    // --- Bloque 3: Perfil del estudiante ---
    public DbSet<PerfilEstudiante> PerfilesEstudiante => Set<PerfilEstudiante>();
    public DbSet<EstudianteCarrera> EstudianteCarreras => Set<EstudianteCarrera>();

    // --- Bloque 4: Perfiles de demanda ---
    public DbSet<PerfilCliente> PerfilesCliente => Set<PerfilCliente>();
    public DbSet<DatosParticular> DatosParticulares => Set<DatosParticular>();
    public DbSet<DatosEmpresa> DatosEmpresas => Set<DatosEmpresa>();
    public DbSet<PerfilAgencia> PerfilesAgencia => Set<PerfilAgencia>();

    // --- Bloque 5: Catalogo de servicios ---
    public DbSet<TipoServicio> TiposServicio => Set<TipoServicio>();
    public DbSet<Servicio> Servicios => Set<Servicio>();

    // --- Bloque 6: Demanda abierta ---
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
    public DbSet<Postulacion> Postulaciones => Set<Postulacion>();

    // --- Bloque 7: Salud ---
    public DbSet<Paciente> Pacientes => Set<Paciente>();

    // --- Bloque 8: Motor transaccional ---
    public DbSet<Trabajo> Trabajos => Set<Trabajo>();
    public DbSet<TrabajoHistorial> TrabajoHistoriales => Set<TrabajoHistorial>();

    // --- Bloque 9: Dinero ---
    public DbSet<Pago> Pagos => Set<Pago>();

    // --- Bloque 10: Salud (consentimiento) ---
    public DbSet<Consentimiento> Consentimientos => Set<Consentimiento>();

    // --- Bloque 11: Reputacion ---
    public DbSet<Resena> Resenas => Set<Resena>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ----------------------------------------------------------------
        // Claves primarias compuestas
        // ----------------------------------------------------------------
        modelBuilder.Entity<UsuarioRol>()
            .HasKey(ur => new { ur.RolId, ur.UsuarioId });

        modelBuilder.Entity<EstudianteCarrera>()
            .HasKey(ec => new { ec.EstudianteId, ec.CarreraId });

        // ----------------------------------------------------------------
        // BLOQUE 1 — Identidad y roles
        // ----------------------------------------------------------------
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Rol>()
            .HasIndex(r => r.Nombre)
            .IsUnique();

        modelBuilder.Entity<UsuarioRol>()
            .HasOne(ur => ur.Rol)
            .WithMany(r => r.UsuarioRoles)
            .HasForeignKey(ur => ur.RolId);

        modelBuilder.Entity<UsuarioRol>()
            .HasOne(ur => ur.Usuario)
            .WithMany(u => u.UsuarioRoles)
            .HasForeignKey(ur => ur.UsuarioId);

        // ----------------------------------------------------------------
        // BLOQUE 2 — Validacion institucional
        // ----------------------------------------------------------------
        modelBuilder.Entity<Institucion>()
            .HasOne(i => i.TipoInstitucion)
            .WithMany(t => t.Instituciones)
            .HasForeignKey(i => i.TipoInstitucionId);

        modelBuilder.Entity<Carrera>()
            .HasOne(c => c.Institucion)
            .WithMany(i => i.Carreras)
            .HasForeignKey(c => c.InstitucionId);

        // ----------------------------------------------------------------
        // BLOQUE 3 — Perfil del estudiante (PK = FK a usuario)
        // ----------------------------------------------------------------
        modelBuilder.Entity<PerfilEstudiante>()
            .HasOne(pe => pe.Usuario)
            .WithOne(u => u.PerfilEstudiante)
            .HasForeignKey<PerfilEstudiante>(pe => pe.UsuarioId);

        modelBuilder.Entity<PerfilEstudiante>()
            .Property(pe => pe.CalificacionPromedio)
            .HasPrecision(3, 2);

        modelBuilder.Entity<EstudianteCarrera>()
            .HasOne(ec => ec.Estudiante)
            .WithMany(pe => pe.EstudianteCarreras)
            .HasForeignKey(ec => ec.EstudianteId);

        modelBuilder.Entity<EstudianteCarrera>()
            .HasOne(ec => ec.Carrera)
            .WithMany(c => c.EstudianteCarreras)
            .HasForeignKey(ec => ec.CarreraId);

        // ----------------------------------------------------------------
        // BLOQUE 4 — Perfiles de demanda (cliente y agencia)
        // ----------------------------------------------------------------
        modelBuilder.Entity<PerfilCliente>()
            .HasOne(pc => pc.Usuario)
            .WithOne(u => u.PerfilCliente)
            .HasForeignKey<PerfilCliente>(pc => pc.UsuarioId);

        modelBuilder.Entity<DatosParticular>()
            .HasOne(dp => dp.PerfilCliente)
            .WithOne(pc => pc.DatosParticular)
            .HasForeignKey<DatosParticular>(dp => dp.UsuarioId);

        modelBuilder.Entity<DatosEmpresa>()
            .HasOne(de => de.PerfilCliente)
            .WithOne(pc => pc.DatosEmpresa)
            .HasForeignKey<DatosEmpresa>(de => de.UsuarioId);

        modelBuilder.Entity<PerfilAgencia>()
            .HasOne(pa => pa.Usuario)
            .WithOne(u => u.PerfilAgencia)
            .HasForeignKey<PerfilAgencia>(pa => pa.UsuarioId);

        // ----------------------------------------------------------------
        // BLOQUE 5 — Catalogo de servicios
        // ----------------------------------------------------------------
        modelBuilder.Entity<Servicio>()
            .HasOne(s => s.Estudiante)
            .WithMany(pe => pe.Servicios)
            .HasForeignKey(s => s.EstudianteId);

        modelBuilder.Entity<Servicio>()
            .HasOne(s => s.TipoServicio)
            .WithMany(t => t.Servicios)
            .HasForeignKey(s => s.TipoServicioId);

        modelBuilder.Entity<Servicio>()
            .Property(s => s.Precio)
            .HasPrecision(12, 2);

        // ----------------------------------------------------------------
        // BLOQUE 6 — Demanda abierta (solicitud -> postulacion)
        // ----------------------------------------------------------------
        modelBuilder.Entity<Solicitud>()
            .HasOne(s => s.Cliente)
            .WithMany(pc => pc.Solicitudes)
            .HasForeignKey(s => s.ClienteId);

        modelBuilder.Entity<Solicitud>()
            .HasOne(s => s.TipoServicio)
            .WithMany(t => t.Solicitudes)
            .HasForeignKey(s => s.TipoServicioId);

        modelBuilder.Entity<Solicitud>()
            .Property(s => s.PresupuestoEstimado)
            .HasPrecision(12, 2);

        modelBuilder.Entity<Postulacion>()
            .HasOne(p => p.Solicitud)
            .WithMany(s => s.Postulaciones)
            .HasForeignKey(p => p.IdSolicitud);

        modelBuilder.Entity<Postulacion>()
            .HasOne(p => p.Estudiante)
            .WithMany(pe => pe.Postulaciones)
            .HasForeignKey(p => p.EstudianteId);

        modelBuilder.Entity<Postulacion>()
            .Property(p => p.MontoPropuesto)
            .HasPrecision(12, 2);

        // ----------------------------------------------------------------
        // BLOQUE 7 — Salud (pacientes)
        // ----------------------------------------------------------------
        modelBuilder.Entity<Paciente>()
            .HasOne(p => p.Cliente)
            .WithMany(pc => pc.Pacientes)
            .HasForeignKey(p => p.ClienteId);

        // ----------------------------------------------------------------
        // BLOQUE 8 — Motor transaccional (trabajo + historial)
        //   FKs nullable: Servicio, Postulacion, Paciente, TipoServicio.
        // ----------------------------------------------------------------
        modelBuilder.Entity<Trabajo>()
            .HasOne(t => t.Estudiante)
            .WithMany(pe => pe.Trabajos)
            .HasForeignKey(t => t.EstudianteId);

        modelBuilder.Entity<Trabajo>()
            .HasOne(t => t.Cliente)
            .WithMany(pc => pc.Trabajos)
            .HasForeignKey(t => t.ClienteId);

        modelBuilder.Entity<Trabajo>()
            .HasOne(t => t.TipoServicio)
            .WithMany(ts => ts.Trabajos)
            .HasForeignKey(t => t.TipoServicioId);

        modelBuilder.Entity<Trabajo>()
            .HasOne(t => t.Servicio)
            .WithMany(s => s.Trabajos)
            .HasForeignKey(t => t.IdServicio);

        modelBuilder.Entity<Trabajo>()
            .HasOne(t => t.Postulacion)
            .WithMany(p => p.Trabajos)
            .HasForeignKey(t => t.IdPostulacion);

        modelBuilder.Entity<Trabajo>()
            .HasOne(t => t.Paciente)
            .WithMany(p => p.Trabajos)
            .HasForeignKey(t => t.PacienteId);

        modelBuilder.Entity<Trabajo>()
            .Property(t => t.Monto)
            .HasPrecision(12, 2);

        modelBuilder.Entity<TrabajoHistorial>()
            .HasOne(h => h.Trabajo)
            .WithMany(t => t.Historiales)
            .HasForeignKey(h => h.IdTrabajo);

        modelBuilder.Entity<TrabajoHistorial>()
            .HasOne(h => h.Usuario)
            .WithMany(u => u.TrabajoHistoriales)
            .HasForeignKey(h => h.UsuarioId);

        // ----------------------------------------------------------------
        // BLOQUE 9 — Dinero (pago / escrow)
        // ----------------------------------------------------------------
        // 1->1: un trabajo tiene a lo sumo un pago. El FK unico (indice unico
        // sobre id_trabajo) lo genera EF al usar WithOne + HasForeignKey<Pago>.
        modelBuilder.Entity<Pago>()
            .HasOne(p => p.Trabajo)
            .WithOne(t => t.Pago)
            .HasForeignKey<Pago>(p => p.IdTrabajo);

        modelBuilder.Entity<Pago>().Property(p => p.MontoTotal).HasPrecision(12, 2);
        modelBuilder.Entity<Pago>().Property(p => p.PorcentajeComision).HasPrecision(5, 2);
        modelBuilder.Entity<Pago>().Property(p => p.ComisionLex).HasPrecision(12, 2);
        modelBuilder.Entity<Pago>().Property(p => p.MontoEstudiante).HasPrecision(12, 2);

        // ----------------------------------------------------------------
        // BLOQUE 10 — Consentimiento (Salud)
        // ----------------------------------------------------------------
        // 1->1: un trabajo tiene a lo sumo un consentimiento.
        modelBuilder.Entity<Consentimiento>()
            .HasOne(c => c.Trabajo)
            .WithOne(t => t.Consentimiento)
            .HasForeignKey<Consentimiento>(c => c.IdTrabajo);

        modelBuilder.Entity<Consentimiento>()
            .HasOne(c => c.Paciente)
            .WithMany(p => p.Consentimientos)
            .HasForeignKey(c => c.PacienteId);

        // ----------------------------------------------------------------
        // BLOQUE 11 — Reputacion (resena)
        //   Dos FKs a usuario (autor / receptor) -> origen del ciclo.
        //   UNIQUE (id_trabajo, autor_usuario_id).
        // ----------------------------------------------------------------
        modelBuilder.Entity<Resena>()
            .HasOne(r => r.Trabajo)
            .WithMany(t => t.Resenas)
            .HasForeignKey(r => r.IdTrabajo);

        modelBuilder.Entity<Resena>()
            .HasOne(r => r.Autor)
            .WithMany(u => u.ResenasComoAutor)
            .HasForeignKey(r => r.AutorUsuarioId);

        modelBuilder.Entity<Resena>()
            .HasOne(r => r.Receptor)
            .WithMany(u => u.ResenasComoReceptor)
            .HasForeignKey(r => r.ReceptorUsuarioId);

        modelBuilder.Entity<Resena>()
            .HasIndex(r => new { r.IdTrabajo, r.AutorUsuarioId })
            .IsUnique();

        // ----------------------------------------------------------------
        // Cascada: se desactiva en TODAS las relaciones (DeleteBehavior.Restrict)
        // para evitar ciclos de borrado en cascada. Es critico en trabajo,
        // resena y trabajo_historial, que apuntan varias veces a usuario/perfiles.
        // ----------------------------------------------------------------
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
