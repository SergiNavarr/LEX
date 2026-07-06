using Lex.Api.Data;
using Lex.Api.DTOs;
using Lex.Api.Entities;
using Lex.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Services;

/// <summary>
/// Seeder de datos de DEMOSTRACIÓN, separado del seeder base (roles, tipos de
/// servicio, instituciones/carreras, admin). Puebla la plataforma para una
/// presentación: estudiantes, servicios, clientes, una agencia, trabajos en
/// distintos estados (con escrow coherente) y reseñas bidireccionales.
///
/// Estrategia de idempotencia: "borrar y recargar". <see cref="SeedAsync"/>
/// primero elimina TODOS los datos demo previos (identificados por el sufijo de
/// email "@demo.com") y luego los recrea. Así la demo siempre queda en un estado
/// limpio y consistente, sin duplicados, sin importar cuántas veces se llame.
///
/// Reutiliza la lógica de negocio real donde importa: <see cref="ITrabajoService"/>
/// para el ciclo de vida de los trabajos (máquina de estados + escrow + comisión)
/// y <see cref="IResenaService"/> para las reseñas (que recalculan la calificación
/// promedio del estudiante). Las fechas se "retrodatan" al final para que la
/// línea de tiempo se vea distribuida y realista.
/// </summary>
public class DemoService : IDemoService
{
    private readonly AppDbContext _db;
    private readonly ITrabajoService _trabajos;
    private readonly IResenaService _resenas;

    // Sufijo que marca a un usuario como "de demo": permite borrarlos sin tocar
    // el admin base ni cuentas reales creadas por registro normal.
    private const string DemoEmailSuffix = "@demo.com";
    private const string DemoPassword = "Demo1234";

    public DemoService(AppDbContext db, ITrabajoService trabajos, IResenaService resenas)
    {
        _db = db;
        _trabajos = trabajos;
        _resenas = resenas;
    }

    // ---------------------------------------------------------------------
    // Catálogo de datos demo (definiciones declarativas)
    // ---------------------------------------------------------------------

    private record EstudianteSeed(string Email, string Nombre, string Carrera, int Anio, string Bio);
    private record ServicioSeed(string Key, string EstudianteEmail, string Tipo, string Titulo, string Descripcion, decimal Precio, int Dias, string FotoId);

    // Imágenes de portada: usamos URLs DIRECTAS y fijas de Unsplash
    // (images.unsplash.com/photo-<id>) en vez del viejo formato Source
    // (source.unsplash.com/?keyword), que Unsplash discontinuó. Las directas son
    // estables (siempre la misma foto) y no dependen de un redirector. Pedimos un
    // recorte 600x400 (3:2) del lado del servidor para que todas pesen y se vean
    // parejas; el front igual aplica object-cover como segunda garantía.
    private static string FotoUrl(string fotoId) =>
        $"https://images.unsplash.com/photo-{fotoId}?w=600&h=400&fit=crop&q=80&auto=format";

    // 10 estudiantes (Cliente Particular + perfil estudiante), variando áreas:
    // diseño, sistemas, contabilidad, idiomas, matemática y 3 de salud.
    private static readonly EstudianteSeed[] Estudiantes =
    {
        new("camila@demo.com",    "Camila Ríos",       "Diseño Gráfico",                              4, "Diseñadora gráfica especializada en identidad de marca y logos. Trabajo con Illustrator y Figma."),
        new("mateo@demo.com",     "Mateo Gómez",       "Licenciatura en Sistemas de Información",     5, "Desarrollador full-stack. Hago sitios web a medida y doy apoyo en programación."),
        new("lucia@demo.com",     "Lucía Fernández",   "Contador Público",                            3, "Estudiante avanzada de contabilidad. Ayudo con impuestos, planillas de costos y rendiciones."),
        new("sofia@demo.com",     "Sofía Martínez",    "Odontología",                                 5, "Practicante de odontología. Realizo controles supervisados por profesionales matriculados."),
        new("juan@demo.com",      "Juan Pérez",        "Medicina",                                    6, "Estudiante de medicina en internado. Doy clases de anatomía y controles supervisados."),
        new("valentina@demo.com", "Valentina López",   "Enfermería",                                  3, "Estudiante de enfermería. Brindo cuidados y controles bajo supervisión profesional."),
        new("tomas@demo.com",     "Tomás Sánchez",     "Ingeniería en Sistemas de Información",       4, "Apasionado del contenido audiovisual: edición de video y manejo de redes para marcas."),
        new("martina@demo.com",   "Martina Díaz",      "Licenciatura en Marketing",                   3, "Bilingüe en inglés. Doy clases conversacionales y gestiono redes sociales."),
        new("benjamin@demo.com",  "Benjamín Torres",   "Profesorado de Matemática",                   2, "Futuro profe de matemática. Doy apoyo en cálculo, álgebra y preparación de finales."),
        new("agustina@demo.com",  "Agustina Romero",   "Diseño de Indumentaria",                      4, "Diseñadora e ilustradora. Hago flyers, ilustración digital y asesoría de imagen."),
    };

    // 18 servicios repartidos: 8 Digital, 5 Clase, 3 Salud, 2 Otro.
    private static readonly ServicioSeed[] Servicios =
    {
        // --- Digital ---
        new("logo",        "camila@demo.com",   "Digital", "Diseño de logo profesional",            "Diseño de logo a medida con 3 propuestas iniciales, hasta 2 rondas de cambios y archivos finales en alta calidad.", 25000m, 5,  "1626785774573-4b799315345d"),
        new("identidad",   "camila@demo.com",   "Digital", "Identidad de marca completa",           "Logo, paleta de colores, tipografías y manual de marca básico para tu emprendimiento.", 60000m, 10, "1558655146-9f40138edfeb"),
        new("fotos",       "camila@demo.com",   "Otro",    "Sesión de fotos de producto",           "Sesión fotográfica de productos para catálogo o redes, con edición y entrega de 20 fotos.", 30000m, 7,  "1606107557195-0e29a4b5b4aa"),
        new("web",         "mateo@demo.com",    "Digital", "Desarrollo de sitio web autogestionable", "Sitio web responsivo y autogestionable, con formulario de contacto y optimización para buscadores.", 180000m, 21, "1461749280684-dccba630e2f6"),
        new("video",       "tomas@demo.com",    "Digital", "Edición de video para redes",           "Edición de reels y videos cortos para Instagram/TikTok, con subtítulos, música y transiciones.", 18000m, 4,  "1574717024653-61fd2cf4d44d"),
        new("community",   "tomas@demo.com",    "Digital", "Community management mensual",          "Gestión de redes por un mes: calendario de contenido, 12 publicaciones y respuesta a mensajes.", 45000m, 30, "1611926653458-09294b3142bf"),
        new("redes",       "martina@demo.com",  "Digital", "Gestión de redes y contenido",          "Estrategia de contenido, diseño de placas y programación de publicaciones para tu marca.", 40000m, 30, "1563986768609-322da13575f3"),
        new("ilustracion", "agustina@demo.com", "Digital", "Ilustración digital / flyer",           "Ilustración digital personalizada o diseño de flyer para eventos y promociones.", 15000m, 3,  "1611532736597-de2d4265fba3"),

        // --- Clase ---
        new("planilla",    "lucia@demo.com",    "Digital", "Armado de planilla de costos en Excel", "Planilla de costos y precios automatizada en Excel, lista para usar y con instructivo.", 20000m, 5,  "1554224155-6726b3ff858f"),
        new("programacion","mateo@demo.com",    "Clase",   "Apoyo en programación (Python/Java)",   "Clases particulares de programación: estructuras, POO y resolución de trabajos prácticos.", 8000m, 2,  "1515879218367-8466d910aaa4"),
        new("ingles",      "martina@demo.com",  "Clase",   "Clases de inglés conversacional",       "Clases de inglés conversacional adaptadas a tu nivel, con material y práctica oral.", 7000m, 1,  "1543002588-bfa74002ed7e"),
        new("calculo",     "benjamin@demo.com", "Clase",   "Apoyo en cálculo y álgebra",           "Apoyo escolar y universitario en cálculo, álgebra y preparación de exámenes finales.", 6500m, 1,  "1635070041078-e363dbe005cb"),
        new("anatomia",    "juan@demo.com",     "Clase",   "Clases de anatomía para ingresantes",   "Clases de anatomía orientadas a ingresantes de carreras de salud, con material de estudio.", 7500m, 2,  "1530026405186-ed1f139313f8"),
        new("contabilidad","lucia@demo.com",    "Clase",   "Apoyo en contabilidad e impuestos",     "Apoyo en contabilidad básica, monotributo e impuestos para estudiantes y emprendedores.", 9000m, 2,  "1554224154-26032ffc0d07"),

        // --- Salud (requieren supervisión) ---
        new("odontologico","sofia@demo.com",    "Salud",   "Control odontológico de práctica supervisada", "Control y limpieza odontológica realizada como práctica supervisada por un profesional matriculado.", 12000m, 3,  "1606811841689-23dfddce3e95"),
        new("signos",      "juan@demo.com",     "Salud",   "Control de signos vitales supervisado", "Control de signos vitales y orientación, realizado bajo supervisión de un profesional matriculado.", 10000m, 2,  "1530497610245-94d3c16cda28"),
        new("enfermeria",  "valentina@demo.com","Salud",   "Sesión de cuidados de enfermería supervisada", "Curaciones y cuidados de enfermería a domicilio, supervisados por un profesional matriculado.", 11000m, 2,  "1576091160399-112ba8d25d1d"),

        // --- Otro ---
        new("imagen",      "agustina@demo.com", "Otro",    "Asesoría de imagen personal",          "Asesoría de imagen y estilo personal: análisis de colorimetría y armado de looks.", 14000m, 5,  "1490481651871-ab68de25d43d"),
    };

    // ---------------------------------------------------------------------
    // SEED
    // ---------------------------------------------------------------------
    public async Task<DemoSeedResponse> SeedAsync()
    {
        // 1) Estado limpio: borramos cualquier dato demo anterior.
        await BorrarDatosDemoAsync();

        var ahora = DateTime.UtcNow;
        // Mismo password para todos -> hasheamos una sola vez (BCrypt es costoso).
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);

        // Catálogos base (ya sembrados por DbSeeder).
        var roles = await _db.Roles.ToDictionaryAsync(r => r.Nombre, r => r.RolId);
        var tipos = await _db.TiposServicio.ToDictionaryAsync(t => t.Nombre, t => t.TipoServicioId);
        var carreras = await _db.Carreras.ToListAsync();

        int RolId(string n) => roles.TryGetValue(n, out var id)
            ? id : throw new InvalidOperationException($"Falta el rol base '{n}'. ¿Se ejecutó DbSeeder?");
        int CarreraId(string n) => carreras.FirstOrDefault(c => c.Nombre == n)?.CarreraId
            ?? throw new InvalidOperationException($"Falta la carrera '{n}' en el catálogo base.");

        // 2) Estudiantes: Cliente Particular + perfil de estudiante (igual que el
        //    flujo real de ActivarEstudiante). El vínculo de carrera lo dejamos
        //    Verificado para que la demo se vea pulida.
        var estudianteIds = new Dictionary<string, int>();
        int idxEst = 0;
        foreach (var e in Estudiantes)
        {
            var u = new Usuario
            {
                Email = e.Email,
                PasswordHash = passwordHash,
                NombreCompleto = e.Nombre,
                FechaRegistro = ahora.AddDays(-130 + idxEst), // alta escalonada
                Activo = true,
                PerfilCliente = new PerfilCliente
                {
                    TipoCliente = (int)TipoCliente.Particular,
                    DatosParticular = new DatosParticular { Dni = $"3{10000000 + idxEst:00000000}" }
                },
                PerfilEstudiante = new PerfilEstudiante
                {
                    Bio = e.Bio,
                    AnioCursado = e.Anio,
                    Disponible = true
                }
            };
            u.UsuarioRoles.Add(new UsuarioRol { RolId = RolId("Cliente") });
            u.UsuarioRoles.Add(new UsuarioRol { RolId = RolId("Estudiante") });
            u.PerfilEstudiante.EstudianteCarreras.Add(new EstudianteCarrera
            {
                CarreraId = CarreraId(e.Carrera),
                EstadoVerificacion = EstadoVerificacion.Verificado,
                FechaVerificacion = ahora.AddDays(-120 + idxEst)
            });
            _db.Usuarios.Add(u);
            // Guardamos por email; el id real se completa tras SaveChanges.
            estudianteIds[e.Email] = 0;
            idxEst++;
        }

        // 3) Clientes (3 particulares + 1 empresa) y 1 agencia.
        var empresa = new Usuario
        {
            Email = "laesquina@demo.com",
            PasswordHash = passwordHash,
            NombreCompleto = "Cafetería La Esquina",
            Telefono = "3624000111",
            FechaRegistro = ahora.AddDays(-95),
            Activo = true,
            PerfilCliente = new PerfilCliente
            {
                TipoCliente = (int)TipoCliente.Empresa,
                DatosEmpresa = new DatosEmpresa
                {
                    RazonSocial = "La Esquina S.R.L.",
                    Cuit = "30-71234567-9",
                    Rubro = "Gastronomía"
                }
            }
        };
        empresa.UsuarioRoles.Add(new UsuarioRol { RolId = RolId("Cliente") });
        _db.Usuarios.Add(empresa);

        var clientesParticulares = new[]
        {
            ("diego@demo.com",    "Diego Fernández", -90),
            ("veronica@demo.com", "Verónica Paz",    -88),
            ("roberto@demo.com",  "Roberto Sosa",    -85),
        };
        var clienteUsuarios = new Dictionary<string, Usuario>();
        foreach (var (email, nombre, dias) in clientesParticulares)
        {
            var u = new Usuario
            {
                Email = email,
                PasswordHash = passwordHash,
                NombreCompleto = nombre,
                FechaRegistro = ahora.AddDays(dias),
                Activo = true,
                PerfilCliente = new PerfilCliente
                {
                    TipoCliente = (int)TipoCliente.Particular,
                    DatosParticular = new DatosParticular { Dni = $"2{9000000 + email.Length:00000000}" }
                }
            };
            u.UsuarioRoles.Add(new UsuarioRol { RolId = RolId("Cliente") });
            _db.Usuarios.Add(u);
            clienteUsuarios[email] = u;
        }

        var agencia = new Usuario
        {
            Email = "agencia@demo.com",
            PasswordHash = passwordHash,
            NombreCompleto = "Agencia Creativa Norte",
            FechaRegistro = ahora.AddDays(-110),
            Activo = true,
            PerfilAgencia = new PerfilAgencia
            {
                NombreAgencia = "Agencia Creativa Norte",
                Rubro = "Marketing y Diseño",
                SitioWeb = "https://creativanorte.demo.com"
            }
        };
        agencia.UsuarioRoles.Add(new UsuarioRol { RolId = RolId("Agencia") });
        _db.Usuarios.Add(agencia);

        await _db.SaveChangesAsync(); // ahora todos los Usuario tienen id

        // Resolver ids reales de estudiantes y empresa.
        foreach (var e in Estudiantes)
            estudianteIds[e.Email] = await _db.Usuarios.Where(u => u.Email == e.Email).Select(u => u.UsuarioId).FirstAsync();
        int empresaId = empresa.UsuarioId;
        int diegoId = clienteUsuarios["diego@demo.com"].UsuarioId;
        int veronicaId = clienteUsuarios["veronica@demo.com"].UsuarioId;
        int robertoId = clienteUsuarios["roberto@demo.com"].UsuarioId;

        // 4) Pacientes de Verónica (flujo de salud).
        var lucasPaz = new Paciente { ClienteId = veronicaId, NombreCompleto = "Lucas Paz", Edad = 9, Notas = "Sin antecedentes relevantes." };
        var martinaPaz = new Paciente { ClienteId = veronicaId, NombreCompleto = "Martina Paz", Edad = 6, Notas = "Control de rutina." };
        _db.Pacientes.AddRange(lucasPaz, martinaPaz);

        // 5) Servicios (entidades directas, para controlar la fecha de publicación).
        var servicioIds = new Dictionary<string, int>();
        int idxSrv = 0;
        foreach (var s in Servicios)
        {
            var servicio = new Servicio
            {
                EstudianteId = estudianteIds[s.EstudianteEmail],
                TipoServicioId = tipos[s.Tipo],
                Titulo = s.Titulo,
                Descripcion = s.Descripcion,
                ImagenUrl = FotoUrl(s.FotoId),
                Precio = s.Precio,
                TiempoEntregaDias = s.Dias,
                Activo = true,
                FechaPublicacion = ahora.AddDays(-100 + idxSrv) // todos antes del trabajo más viejo
            };
            _db.Servicios.Add(servicio);
            servicioIds[s.Key] = 0;
            idxSrv++;
        }
        await _db.SaveChangesAsync();

        foreach (var s in Servicios)
            servicioIds[s.Key] = await _db.Servicios.Where(x => x.Titulo == s.Titulo && x.EstudianteId == estudianteIds[s.EstudianteEmail]).Select(x => x.IdServicio).FirstAsync();

        // 6) Trabajos: se crean y avanzan con TrabajoService (máquina de estados +
        //    escrow + comisión real) y luego se retrodatan las fechas.

        // --- Completados (habilitan reseñas; pago Liberado) ---
        var t1 = await CrearTrabajoAsync(diegoId,    servicioIds["logo"],         EstadoTrabajo.Completado, ahora.AddDays(-45), 6);
        var t2 = await CrearTrabajoAsync(empresaId,  servicioIds["web"],          EstadoTrabajo.Completado, ahora.AddDays(-38), 18);
        var t3 = await CrearTrabajoAsync(robertoId,  servicioIds["calculo"],      EstadoTrabajo.Completado, ahora.AddDays(-30), 2);
        var t4 = await CrearTrabajoAsync(robertoId,  servicioIds["anatomia"],     EstadoTrabajo.Completado, ahora.AddDays(-28), 3);
        var t5 = await CrearTrabajoAsync(empresaId,  servicioIds["community"],    EstadoTrabajo.Completado, ahora.AddDays(-24), 12);
        var t6 = await CrearTrabajoAsync(diegoId,    servicioIds["ingles"],       EstadoTrabajo.Completado, ahora.AddDays(-16), 2);
        // --- Salud completo (el diferenciador): paciente + consentimiento + supervisor ---
        var t7 = await CrearTrabajoSaludAsync(veronicaId, servicioIds["odontologico"], lucasPaz.PacienteId, EstadoTrabajo.Completado, "Dra. María Acosta (MP 1234)", ahora.AddDays(-12), 3);

        // --- En curso (pago Retenido) ---
        var t8 = await CrearTrabajoAsync(empresaId, servicioIds["identidad"],  EstadoTrabajo.EnCurso, ahora.AddDays(-6), 0);
        var t9 = await CrearTrabajoAsync(robertoId, servicioIds["ilustracion"], EstadoTrabajo.EnCurso, ahora.AddDays(-4), 0);

        // --- Aceptado (pago Retenido) ---
        var t10 = await CrearTrabajoAsync(diegoId, servicioIds["video"], EstadoTrabajo.Aceptado, ahora.AddDays(-3), 0);

        // --- Pendientes (sin pago) ---
        var t11 = await CrearTrabajoAsync(veronicaId, servicioIds["contabilidad"], EstadoTrabajo.Pendiente, ahora.AddDays(-2), 0);
        // Salud pendiente: consentimiento aceptado, todavía sin supervisor (lo asigna el estudiante al aceptar).
        var t12 = await CrearTrabajoSaludAsync(veronicaId, servicioIds["enfermeria"], martinaPaz.PacienteId, EstadoTrabajo.Pendiente, null, ahora.AddDays(-1), 0);

        // 7) Reseñas bidireccionales en los completados (mayormente 4-5, alguna 3).
        await ResenarAsync(t1, diegoId,    5, "Quedó espectacular el logo, súper profesional y atento a los cambios.", 5, "Cliente claro con lo que buscaba, un gusto trabajar así.");
        await ResenarAsync(t2, empresaId,  5, "La web quedó impecable y entregó antes de lo pactado.",                  4, "Buena comunicación durante todo el proyecto, recomendable.");
        await ResenarAsync(t3, robertoId,  4, "Muy claro explicando, me destrabó cálculo para el final.",              5, "Alumno muy aplicado y comprometido.");
        await ResenarAsync(t4, robertoId,  4, "Buenas clases de anatomía, bien organizado el material.",               3, "Llegó tarde a una clase pero después todo bien.");
        await ResenarAsync(t5, empresaId,  3, "Buen trabajo en redes, aunque tardó en responder algunos mensajes.",   4, "Todo en orden, pagos puntuales.");
        await ResenarAsync(t6, diegoId,    5, "Excelentes clases de inglés, muy didáctica y paciente.",               5, "Muy comprometido con la práctica, mejoró un montón.");
        await ResenarAsync(t7, veronicaId, 5, "Muy profesional y cuidadosa, todo realizado bajo supervisión. Confiable.", 5, "Paciente colaborador, gracias por la confianza.");

        await _db.SaveChangesAsync();

        return await ConstruirResumenAsync();
    }

    public async Task<DemoResetResponse> ResetAsync()
    {
        var eliminados = await BorrarDatosDemoAsync();
        return new DemoResetResponse
        {
            Mensaje = eliminados == 0
                ? "No había datos de demostración para borrar."
                : "Datos de demostración eliminados. Los datos base (roles, tipos, instituciones, admin) no se tocaron.",
            UsuariosEliminados = eliminados
        };
    }

    // ---------------------------------------------------------------------
    // Helpers de ciclo de vida (reutilizan TrabajoService / ResenaService)
    // ---------------------------------------------------------------------

    // Crea un trabajo directo y lo avanza hasta 'objetivo' usando la máquina de
    // estados real, luego retrodata las fechas. Devuelve (idTrabajo, idEstudiante).
    private async Task<(int Id, int EstudianteId)> CrearTrabajoAsync(
        int clienteId, int servicioId, EstadoTrabajo objetivo, DateTime creado, int durDias)
    {
        var trabajo = await _trabajos.ContratarServicioAsync(clienteId, new ContratarServicioRequest { IdServicio = servicioId });
        await AvanzarAsync(trabajo.IdTrabajo, trabajo.EstudianteId, clienteId, objetivo, null);
        await RetrodatarAsync(trabajo.IdTrabajo, creado, durDias);
        return (trabajo.IdTrabajo, trabajo.EstudianteId);
    }

    // Igual, pero para servicios de Salud (con paciente + consentimiento + supervisor).
    private async Task<(int Id, int EstudianteId)> CrearTrabajoSaludAsync(
        int clienteId, int servicioId, int pacienteId, EstadoTrabajo objetivo, string? supervisor, DateTime creado, int durDias)
    {
        var trabajo = await _trabajos.ContratarServicioSaludAsync(clienteId, new ContratarServicioSaludRequest
        {
            IdServicio = servicioId,
            PacienteId = pacienteId,
            ConsentimientoAceptado = true
        });
        await AvanzarAsync(trabajo.IdTrabajo, trabajo.EstudianteId, clienteId, objetivo, supervisor);
        await RetrodatarAsync(trabajo.IdTrabajo, creado, durDias);
        return (trabajo.IdTrabajo, trabajo.EstudianteId);
    }

    // Recorre las transiciones permitidas hasta el estado objetivo, respetando
    // qué parte está autorizada en cada paso (estudiante acepta/inicia, cliente completa).
    private async Task AvanzarAsync(int idTrabajo, int estudianteId, int clienteId, EstadoTrabajo objetivo, string? supervisor)
    {
        var meta = (int)objetivo;
        if (meta >= (int)EstadoTrabajo.Aceptado)
            await _trabajos.CambiarEstadoAsync(estudianteId, idTrabajo, EstadoTrabajo.Aceptado, supervisor);
        if (meta >= (int)EstadoTrabajo.EnCurso)
            await _trabajos.CambiarEstadoAsync(estudianteId, idTrabajo, EstadoTrabajo.EnCurso);
        if (meta >= (int)EstadoTrabajo.Completado)
            await _trabajos.CambiarEstadoAsync(clienteId, idTrabajo, EstadoTrabajo.Completado);
    }

    // Crea dos reseñas (cliente->estudiante y estudiante->cliente) en un trabajo
    // completado y las fecha unos días después del cierre.
    private async Task ResenarAsync((int Id, int EstudianteId) trabajo, int clienteId,
        int puntajeCliente, string comentarioCliente, int puntajeEstudiante, string comentarioEstudiante)
    {
        var rCliente = await _resenas.CrearAsync(clienteId, trabajo.Id, new CrearResenaRequest { Puntaje = puntajeCliente, Comentario = comentarioCliente });
        var rEstudiante = await _resenas.CrearAsync(trabajo.EstudianteId, trabajo.Id, new CrearResenaRequest { Puntaje = puntajeEstudiante, Comentario = comentarioEstudiante });

        var fin = await _db.Trabajos.Where(t => t.IdTrabajo == trabajo.Id).Select(t => t.FechaFin).FirstAsync() ?? DateTime.UtcNow;
        var e1 = await _db.Resenas.FindAsync(rCliente.IdResena);
        var e2 = await _db.Resenas.FindAsync(rEstudiante.IdResena);
        if (e1 is not null) e1.Fecha = fin.AddDays(1);
        if (e2 is not null) e2.Fecha = fin.AddDays(2);
    }

    // Reescribe las fechas del trabajo (y su historial / pago / consentimiento)
    // para que la línea de tiempo quede distribuida y coherente. Los servicios
    // estampan DateTime.UtcNow; acá las "movemos al pasado" sin alterar la lógica.
    private async Task RetrodatarAsync(int idTrabajo, DateTime creado, int durDias)
    {
        var t = await _db.Trabajos
            .Include(x => x.Historiales)
            .Include(x => x.Pago)
            .Include(x => x.Consentimiento)
            .FirstAsync(x => x.IdTrabajo == idTrabajo);

        t.FechaCreacion = creado;
        var hPend = t.Historiales.FirstOrDefault(h => h.EstadoNuevo == EstadoTrabajo.Pendiente);
        if (hPend is not null) hPend.Fecha = creado;
        if (t.Consentimiento is not null) t.Consentimiento.FechaAceptacion = creado;

        var aceptado = creado.AddDays(1);
        var hAcc = t.Historiales.FirstOrDefault(h => h.EstadoNuevo == EstadoTrabajo.Aceptado);
        if (hAcc is not null)
        {
            hAcc.Fecha = aceptado;
            if (t.Pago is not null) t.Pago.FechaRetencion = aceptado;
        }

        var enCurso = creado.AddDays(2);
        var hEC = t.Historiales.FirstOrDefault(h => h.EstadoNuevo == EstadoTrabajo.EnCurso);
        if (hEC is not null)
        {
            hEC.Fecha = enCurso;
            t.FechaInicio = enCurso;
        }

        var fin = creado.AddDays(Math.Max(durDias, 3));
        var hComp = t.Historiales.FirstOrDefault(h => h.EstadoNuevo == EstadoTrabajo.Completado);
        if (hComp is not null)
        {
            hComp.Fecha = fin;
            t.FechaFin = fin;
            if (t.Pago is not null) t.Pago.FechaLiberacion = fin;
        }

        await _db.SaveChangesAsync();
    }

    // ---------------------------------------------------------------------
    // Borrado de datos demo (respeta el orden de FKs: hijos antes que padres)
    // ---------------------------------------------------------------------
    private async Task<int> BorrarDatosDemoAsync()
    {
        var demoUserIds = await _db.Usuarios
            .Where(u => u.Email.EndsWith(DemoEmailSuffix))
            .Select(u => u.UsuarioId)
            .ToListAsync();

        if (demoUserIds.Count == 0)
            return 0;

        // Trabajos cuyos participantes son demo (estudiante o cliente).
        var trabajoIds = await _db.Trabajos
            .Where(t => demoUserIds.Contains(t.EstudianteId) || demoUserIds.Contains(t.ClienteId))
            .Select(t => t.IdTrabajo)
            .ToListAsync();

        // Solicitudes / postulaciones demo (por si se agregan en el futuro).
        var solicitudIds = await _db.Solicitudes
            .Where(s => demoUserIds.Contains(s.ClienteId))
            .Select(s => s.IdSolicitud)
            .ToListAsync();

        _db.Resenas.RemoveRange(_db.Resenas.Where(r => trabajoIds.Contains(r.IdTrabajo)
            || demoUserIds.Contains(r.AutorUsuarioId) || demoUserIds.Contains(r.ReceptorUsuarioId)));
        _db.Consentimientos.RemoveRange(_db.Consentimientos.Where(c => trabajoIds.Contains(c.IdTrabajo)));
        _db.Pagos.RemoveRange(_db.Pagos.Where(p => trabajoIds.Contains(p.IdTrabajo)));
        _db.TrabajoHistoriales.RemoveRange(_db.TrabajoHistoriales.Where(h => trabajoIds.Contains(h.IdTrabajo) || (h.UsuarioId != null && demoUserIds.Contains(h.UsuarioId.Value))));
        _db.Trabajos.RemoveRange(_db.Trabajos.Where(t => trabajoIds.Contains(t.IdTrabajo)));
        _db.Postulaciones.RemoveRange(_db.Postulaciones.Where(p => solicitudIds.Contains(p.IdSolicitud) || demoUserIds.Contains(p.EstudianteId)));
        _db.Solicitudes.RemoveRange(_db.Solicitudes.Where(s => solicitudIds.Contains(s.IdSolicitud)));
        _db.Servicios.RemoveRange(_db.Servicios.Where(s => demoUserIds.Contains(s.EstudianteId)));
        _db.Pacientes.RemoveRange(_db.Pacientes.Where(p => demoUserIds.Contains(p.ClienteId)));
        _db.EstudianteCarreras.RemoveRange(_db.EstudianteCarreras.Where(ec => demoUserIds.Contains(ec.EstudianteId)));
        _db.PerfilesEstudiante.RemoveRange(_db.PerfilesEstudiante.Where(p => demoUserIds.Contains(p.UsuarioId)));
        _db.DatosParticulares.RemoveRange(_db.DatosParticulares.Where(d => demoUserIds.Contains(d.UsuarioId)));
        _db.DatosEmpresas.RemoveRange(_db.DatosEmpresas.Where(d => demoUserIds.Contains(d.UsuarioId)));
        _db.PerfilesCliente.RemoveRange(_db.PerfilesCliente.Where(p => demoUserIds.Contains(p.UsuarioId)));
        _db.PerfilesAgencia.RemoveRange(_db.PerfilesAgencia.Where(p => demoUserIds.Contains(p.UsuarioId)));
        _db.UsuarioRoles.RemoveRange(_db.UsuarioRoles.Where(ur => demoUserIds.Contains(ur.UsuarioId)));

        await _db.SaveChangesAsync();

        _db.Usuarios.RemoveRange(_db.Usuarios.Where(u => demoUserIds.Contains(u.UsuarioId)));
        await _db.SaveChangesAsync();

        return demoUserIds.Count;
    }

    // ---------------------------------------------------------------------
    // Resumen de lo creado (para devolver al admin)
    // ---------------------------------------------------------------------
    private async Task<DemoSeedResponse> ConstruirResumenAsync()
    {
        var demoUserIds = await _db.Usuarios
            .Where(u => u.Email.EndsWith(DemoEmailSuffix))
            .Select(u => u.UsuarioId)
            .ToListAsync();

        var estudiantes = await _db.PerfilesEstudiante.CountAsync(p => demoUserIds.Contains(p.UsuarioId));
        var empresas = await _db.PerfilesCliente.CountAsync(p => demoUserIds.Contains(p.UsuarioId) && p.TipoCliente == (int)TipoCliente.Empresa);
        // Clientes "puros": particulares que NO son estudiantes.
        var idsEstudiante = await _db.PerfilesEstudiante.Where(p => demoUserIds.Contains(p.UsuarioId)).Select(p => p.UsuarioId).ToListAsync();
        var clientesParticulares = await _db.PerfilesCliente.CountAsync(p =>
            demoUserIds.Contains(p.UsuarioId) && p.TipoCliente == (int)TipoCliente.Particular && !idsEstudiante.Contains(p.UsuarioId));
        var agencias = await _db.PerfilesAgencia.CountAsync(p => demoUserIds.Contains(p.UsuarioId));
        var pacientes = await _db.Pacientes.CountAsync(p => demoUserIds.Contains(p.ClienteId));
        var servicios = await _db.Servicios.CountAsync(s => demoUserIds.Contains(s.EstudianteId));

        var trabajos = await _db.Trabajos
            .Where(t => demoUserIds.Contains(t.EstudianteId) || demoUserIds.Contains(t.ClienteId))
            .Select(t => new { t.IdTrabajo, t.Estado })
            .ToListAsync();
        var trabajoIds = trabajos.Select(t => t.IdTrabajo).ToList();

        var resenas = await _db.Resenas.CountAsync(r => trabajoIds.Contains(r.IdTrabajo));

        var pagos = await _db.Pagos.Where(p => trabajoIds.Contains(p.IdTrabajo)).Select(p => new { p.Estado, p.ComisionLex }).ToListAsync();

        return new DemoSeedResponse
        {
            Mensaje = "Datos de demostración cargados correctamente. Los datos base no se modificaron.",
            PasswordDemo = DemoPassword,
            Estudiantes = estudiantes,
            ClientesParticulares = clientesParticulares,
            Empresas = empresas,
            Agencias = agencias,
            Pacientes = pacientes,
            Servicios = servicios,
            Trabajos = trabajos.Count,
            Resenas = resenas,
            TrabajosPorEstado = trabajos.GroupBy(t => t.Estado.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            ComisionLexLiberada = pagos.Where(p => p.Estado == EstadoPago.Liberado).Sum(p => p.ComisionLex),
            ComisionLexRetenida = pagos.Where(p => p.Estado == EstadoPago.Retenido).Sum(p => p.ComisionLex),
            EmailsEjemplo = new List<string>
            {
                "camila@demo.com (estudiante - diseño)",
                "sofia@demo.com (estudiante - salud)",
                "diego@demo.com (cliente particular)",
                "veronica@demo.com (cliente con pacientes)",
                "laesquina@demo.com (cliente empresa)",
                "agencia@demo.com (agencia)"
            }
        };
    }
}
