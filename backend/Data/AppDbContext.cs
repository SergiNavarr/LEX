using Lex.Api.Domain.Entities;
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

    // --- Bloque 5: Servicios (jerarquia TPT) + catalogo cerrado ---
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<ServicioProyectoCerrado> ServiciosProyectoCerrado => Set<ServicioProyectoCerrado>();
    public DbSet<ServicioClase> ServiciosClase => Set<ServicioClase>();
    public DbSet<ServicioSalud> ServiciosSalud => Set<ServicioSalud>();
    public DbSet<CatalogoServicio> CatalogoServicios => Set<CatalogoServicio>();
    public DbSet<CatalogoServicioCarrera> CatalogoServicioCarreras => Set<CatalogoServicioCarrera>();
    public DbSet<ProfesionalSupervisor> ProfesionalesSupervisores => Set<ProfesionalSupervisor>();

    // --- Bloque 6: Demanda abierta ---
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
    public DbSet<Postulacion> Postulaciones => Set<Postulacion>();

    // --- Bloque 7: Salud ---
    public DbSet<Paciente> Pacientes => Set<Paciente>();

    // --- Bloque 8: Motor transaccional (jerarquia TPT) ---
    public DbSet<Trabajo> Trabajos => Set<Trabajo>();
    public DbSet<TrabajoProyectoCerrado> TrabajosProyectoCerrado => Set<TrabajoProyectoCerrado>();
    public DbSet<TrabajoClase> TrabajosClase => Set<TrabajoClase>();
    public DbSet<TrabajoSalud> TrabajosSalud => Set<TrabajoSalud>();
    public DbSet<TrabajoHistorial> TrabajoHistoriales => Set<TrabajoHistorial>();

    // --- Bloque 9: Dinero ---
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<MovimientoPago> MovimientosPago => Set<MovimientoPago>();

    // --- Bloque 10: Salud (consentimiento) ---
    public DbSet<Consentimiento> Consentimientos => Set<Consentimiento>();

    // --- Bloque 11: Reputacion ---
    public DbSet<Resena> Resenas => Set<Resena>();

    // --- Bloque 12: Agenda (disponibilidad, turnos y sesiones) ---
    public DbSet<DisponibilidadEstudiante> DisponibilidadesEstudiante => Set<DisponibilidadEstudiante>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<Sesion> Sesiones => Set<Sesion>();

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

        modelBuilder.Entity<CatalogoServicioCarrera>()
            .HasKey(cc => new { cc.CatalogoServicioId, cc.CarreraId });

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
        // BLOQUE 5 — Servicios: jerarquia TPT (Table Per Type)
        //   servicio (base, abstracta) + una tabla por vertical concreta.
        //   La PK de cada tabla hija es tambien FK a servicio.id (lo genera EF).
        // ----------------------------------------------------------------
        modelBuilder.Entity<Servicio>().UseTptMappingStrategy();
        modelBuilder.Entity<Servicio>().ToTable("servicio");
        modelBuilder.Entity<ServicioProyectoCerrado>().ToTable("servicio_proyecto_cerrado");
        modelBuilder.Entity<ServicioClase>().ToTable("servicio_clase");
        modelBuilder.Entity<ServicioSalud>().ToTable("servicio_salud");

        modelBuilder.Entity<Servicio>()
            .HasOne(s => s.Estudiante)
            .WithMany(pe => pe.Servicios)
            .HasForeignKey(s => s.EstudianteId);

        modelBuilder.Entity<Servicio>()
            .Property(s => s.Precio)
            .HasPrecision(12, 2);

        // --- ProyectoCerrado: catalogo cerrado ---
        modelBuilder.Entity<ServicioProyectoCerrado>()
            .HasOne(s => s.CatalogoServicio)
            .WithMany()
            .HasForeignKey(s => s.CatalogoServicioId);

        modelBuilder.Entity<ServicioProyectoCerrado>()
            .Property(s => s.FormatoEntrega)
            .HasConversion<string>();

        // --- Clase: catalogo libre (sin FK a CatalogoServicio) ---
        modelBuilder.Entity<ServicioClase>()
            .Property(s => s.Nivel)
            .HasConversion<string>();

        modelBuilder.Entity<ServicioClase>()
            .Property(s => s.Modalidad)
            .HasConversion<string>();

        // --- Salud: catalogo cerrado + supervisor matriculado ---
        modelBuilder.Entity<ServicioSalud>()
            .HasOne(s => s.CatalogoServicio)
            .WithMany()
            .HasForeignKey(s => s.CatalogoServicioId);

        modelBuilder.Entity<ServicioSalud>()
            .HasOne(s => s.Supervisor)
            .WithMany(p => p.ServiciosSalud)
            .HasForeignKey(s => s.SupervisorId);

        modelBuilder.Entity<ServicioSalud>()
            .Property(s => s.Modalidad)
            .HasConversion<string>();

        // ----------------------------------------------------------------
        // BLOQUE 5b — Catalogo cerrado de servicios permitidos
        //   catalogo_servicio_carrera: PK compuesta + año minimo por carrera.
        // ----------------------------------------------------------------
        modelBuilder.Entity<CatalogoServicio>()
            .Property(c => c.TipoServicio)
            .HasConversion<string>();

        modelBuilder.Entity<CatalogoServicio>()
            .HasIndex(c => c.Nombre)
            .IsUnique();

        modelBuilder.Entity<CatalogoServicioCarrera>()
            .HasOne(cc => cc.CatalogoServicio)
            .WithMany(c => c.Carreras)
            .HasForeignKey(cc => cc.CatalogoServicioId);

        modelBuilder.Entity<CatalogoServicioCarrera>()
            .HasOne(cc => cc.Carrera)
            .WithMany()
            .HasForeignKey(cc => cc.CarreraId);

        modelBuilder.Entity<ProfesionalSupervisor>()
            .HasOne(p => p.Institucion)
            .WithMany()
            .HasForeignKey(p => p.InstitucionId);

        // ----------------------------------------------------------------
        // BLOQUE 6 — Demanda abierta (solicitud -> postulacion)
        // ----------------------------------------------------------------
        modelBuilder.Entity<Solicitud>()
            .HasOne(s => s.Cliente)
            .WithMany(pc => pc.Solicitudes)
            .HasForeignKey(s => s.ClienteId);

        // Ya no es FK a tipo_servicio: es la vertical que pide el cliente, como string.
        modelBuilder.Entity<Solicitud>()
            .Property(s => s.TipoServicio)
            .HasConversion<string>();

        modelBuilder.Entity<Solicitud>()
            .Property(s => s.PresupuestoEstimado)
            .HasPrecision(12, 2);

        modelBuilder.Entity<Postulacion>()
            .HasOne(p => p.Solicitud)
            .WithMany(s => s.Postulaciones)
            .HasForeignKey(p => p.SolicitudId);

        modelBuilder.Entity<Postulacion>()
            .HasOne(p => p.Estudiante)
            .WithMany(pe => pe.Postulaciones)
            .HasForeignKey(p => p.EstudianteId);

        modelBuilder.Entity<Postulacion>()
            .Property(p => p.MontoPropuesto)
            .HasPrecision(12, 2);

        // ----------------------------------------------------------------
        // BLOQUE 7 — Salud (pacientes: Humano/Animal en la misma tabla)
        // ----------------------------------------------------------------
        modelBuilder.Entity<Paciente>()
            .HasOne(p => p.ClienteResponsable)
            .WithMany(pc => pc.Pacientes)
            .HasForeignKey(p => p.ClienteResponsableId);

        modelBuilder.Entity<Paciente>()
            .Property(p => p.Tipo)
            .HasConversion<string>();

        // ----------------------------------------------------------------
        // BLOQUE 8 — Motor transaccional: jerarquia TPT (Table Per Type)
        //   trabajo (base, abstracta) + una tabla por vertical concreta.
        //   La PK de cada tabla hija es tambien FK a trabajo.id (lo genera EF).
        // ----------------------------------------------------------------
        modelBuilder.Entity<Trabajo>().UseTptMappingStrategy();
        modelBuilder.Entity<Trabajo>().ToTable("trabajo");
        modelBuilder.Entity<TrabajoProyectoCerrado>().ToTable("trabajo_proyecto_cerrado");
        modelBuilder.Entity<TrabajoClase>().ToTable("trabajo_clase");
        modelBuilder.Entity<TrabajoSalud>().ToTable("trabajo_salud");

        modelBuilder.Entity<Trabajo>()
            .HasOne(t => t.Estudiante)
            .WithMany(pe => pe.Trabajos)
            .HasForeignKey(t => t.EstudianteId);

        modelBuilder.Entity<Trabajo>()
            .HasOne(t => t.Cliente)
            .WithMany(pc => pc.Trabajos)
            .HasForeignKey(t => t.ClienteId);

        modelBuilder.Entity<Trabajo>()
            .HasOne(t => t.Servicio)
            .WithMany(s => s.Trabajos)
            .HasForeignKey(t => t.ServicioId);

        modelBuilder.Entity<Trabajo>()
            .Property(t => t.PrecioAcordado)
            .HasPrecision(12, 2);

        // Estado unificado, persistido como string.
        modelBuilder.Entity<Trabajo>()
            .Property(t => t.Estado)
            .HasConversion<string>();

        // Enums snapshot de cada vertical, persistidos como string.
        modelBuilder.Entity<TrabajoProyectoCerrado>()
            .Property(t => t.FormatoEntregaSnapshot)
            .HasConversion<string>();

        modelBuilder.Entity<TrabajoClase>()
            .Property(t => t.NivelSnapshot)
            .HasConversion<string>();

        modelBuilder.Entity<TrabajoClase>()
            .Property(t => t.ModalidadSnapshot)
            .HasConversion<string>();

        modelBuilder.Entity<TrabajoSalud>()
            .Property(t => t.ModalidadSaludSnapshot)
            .HasConversion<string>();

        // Salud -> Paciente (FK real).
        modelBuilder.Entity<TrabajoSalud>()
            .HasOne(t => t.Paciente)
            .WithMany(p => p.TrabajosSalud)
            .HasForeignKey(t => t.PacienteId);

        modelBuilder.Entity<TrabajoHistorial>()
            .HasOne(h => h.Trabajo)
            .WithMany(t => t.Historiales)
            .HasForeignKey(h => h.TrabajoId);

        modelBuilder.Entity<TrabajoHistorial>()
            .HasOne(h => h.Usuario)
            .WithMany(u => u.TrabajoHistoriales)
            .HasForeignKey(h => h.UsuarioId);

        // ----------------------------------------------------------------
        // BLOQUE 9 — Dinero (pago / escrow + libro de movimientos)
        // ----------------------------------------------------------------
        // 1->1: un trabajo tiene a lo sumo un pago. El FK unico (indice unico
        // sobre id_trabajo) lo genera EF al usar WithOne + HasForeignKey<Pago>.
        modelBuilder.Entity<Pago>()
            .HasOne(p => p.Trabajo)
            .WithOne(t => t.Pago)
            .HasForeignKey<Pago>(p => p.TrabajoId);

        modelBuilder.Entity<Pago>().Property(p => p.MontoTotal).HasPrecision(12, 2);
        modelBuilder.Entity<Pago>().Property(p => p.PorcentajeComisionLex).HasPrecision(5, 2);
        modelBuilder.Entity<Pago>().Property(p => p.MontoComisionCalculada).HasPrecision(12, 2);
        modelBuilder.Entity<Pago>().Property(p => p.MontoAEstudiante).HasPrecision(12, 2);

        // Estado del escrow como string.
        modelBuilder.Entity<Pago>()
            .Property(p => p.Estado)
            .HasConversion<string>();

        // --- Libro de movimientos (asientos contables del pago) ---
        modelBuilder.Entity<MovimientoPago>().Property(m => m.Monto).HasPrecision(12, 2);

        modelBuilder.Entity<MovimientoPago>()
            .Property(m => m.Tipo)
            .HasConversion<string>();

        // Traza opcional al historial de estados del trabajo (sin navegacion inversa).
        modelBuilder.Entity<MovimientoPago>()
            .HasOne<TrabajoHistorial>()
            .WithMany()
            .HasForeignKey(m => m.TrabajoHistorialId);

        // ----------------------------------------------------------------
        // BLOQUE 10 — Consentimiento (Salud): obligatorio para TrabajoSalud
        // ----------------------------------------------------------------
        // 1->1: un consentimiento por trabajo de salud. El FK modelado vive en
        // TrabajoSalud.ConsentimientoId (se llena al firmar); trabajo_salud_id es
        // columna de evidencia con indice unico.
        modelBuilder.Entity<Consentimiento>()
            .HasIndex(c => c.TrabajoSaludId)
            .IsUnique();

        modelBuilder.Entity<TrabajoSalud>()
            .HasOne(t => t.Consentimiento)
            .WithOne()
            .HasForeignKey<TrabajoSalud>(t => t.ConsentimientoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Consentimiento>()
            .HasOne(c => c.AceptadoPor)
            .WithMany()
            .HasForeignKey(c => c.AceptadoPorUsuarioId);

        // ----------------------------------------------------------------
        // BLOQUE 11 — Reputacion (resena)
        //   Dos FKs a usuario (autor / receptor) -> origen del ciclo.
        //   UNIQUE (id_trabajo, autor_usuario_id).
        // ----------------------------------------------------------------
        modelBuilder.Entity<Resena>()
            .HasOne(r => r.Trabajo)
            .WithMany(t => t.Resenas)
            .HasForeignKey(r => r.TrabajoId);

        modelBuilder.Entity<Resena>()
            .HasOne(r => r.Autor)
            .WithMany(u => u.ResenasComoAutor)
            .HasForeignKey(r => r.AutorUsuarioId);

        modelBuilder.Entity<Resena>()
            .HasOne(r => r.Receptor)
            .WithMany(u => u.ResenasComoReceptor)
            .HasForeignKey(r => r.ReceptorUsuarioId);

        modelBuilder.Entity<Resena>()
            .HasIndex(r => new { r.TrabajoId, r.AutorUsuarioId })
            .IsUnique();

        // ----------------------------------------------------------------
        // BLOQUE 12 — Agenda: disponibilidad semanal -> turno -> sesion
        //   El OnDelete de disponibilidad y sesion se declara DESPUES del loop
        //   de Restrict global, al final de este metodo.
        // ----------------------------------------------------------------
        modelBuilder.Entity<DisponibilidadEstudiante>()
            .Property(d => d.DiaSemana)
            .HasConversion<string>();

        // Busqueda tipica: los bloques de un estudiante, opcionalmente de un dia.
        modelBuilder.Entity<DisponibilidadEstudiante>()
            .HasIndex(d => new { d.EstudianteId, d.DiaSemana });

        modelBuilder.Entity<Turno>()
            .Property(t => t.Estado)
            .HasConversion<string>();

        modelBuilder.Entity<Turno>()
            .HasOne(t => t.Estudiante)
            .WithMany()
            .HasForeignKey(t => t.EstudianteId);

        modelBuilder.Entity<Turno>()
            .HasOne(t => t.Cliente)
            .WithMany()
            .HasForeignKey(t => t.ClienteId);

        // Deteccion de choques al reservar (agenda del estudiante en una ventana).
        modelBuilder.Entity<Turno>()
            .HasIndex(t => new { t.EstudianteId, t.FechaHoraInicio });

        // Agenda del cliente.
        modelBuilder.Entity<Turno>()
            .HasIndex(t => new { t.ClienteId, t.FechaHoraInicio });

        modelBuilder.Entity<Sesion>()
            .Property(s => s.Estado)
            .HasConversion<string>();

        // 1->1 con turno: un turno se consume por a lo sumo una sesion.
        modelBuilder.Entity<Sesion>()
            .HasIndex(s => s.TurnoId)
            .IsUnique();

        // Listado de sesiones de un trabajo.
        modelBuilder.Entity<Sesion>()
            .HasIndex(s => s.TrabajoId);

        // ----------------------------------------------------------------
        // Cascada: se desactiva en TODAS las relaciones (DeleteBehavior.Restrict)
        // para evitar ciclos de borrado en cascada. Es critico en trabajo,
        // resena y trabajo_historial, que apuntan varias veces a usuario/perfiles.
        // ----------------------------------------------------------------
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // Excepcion a la regla anterior: el libro de movimientos cuelga del pago; al
        // borrar el pago se borran en cascada sus movimientos. Se declara despues del
        // loop para que el Cascade no quede pisado por el Restrict global.
        modelBuilder.Entity<MovimientoPago>()
            .HasOne(m => m.Pago)
            .WithMany(p => p.Movimientos)
            .HasForeignKey(m => m.PagoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Misma excepcion para la agenda (declarada aca por el mismo motivo):
        //  - Los bloques de disponibilidad son una preferencia del estudiante, sin valor
        //    contractual: si se va, se van con el.
        //  - Las sesiones son parte del trabajo; borrar el trabajo se las lleva. El turno,
        //    en cambio, queda Restrict: no se borra un turno con sesion viva, primero se
        //    cancela la sesion.
        modelBuilder.Entity<DisponibilidadEstudiante>()
            .HasOne(d => d.Estudiante)
            .WithMany()
            .HasForeignKey(d => d.EstudianteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Sesion>()
            .HasOne(s => s.Trabajo)
            .WithMany()
            .HasForeignKey(s => s.TrabajoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Sesion>()
            .HasOne(s => s.Turno)
            .WithOne(t => t.Sesion)
            .HasForeignKey<Sesion>(s => s.TurnoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
