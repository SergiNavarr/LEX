using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.Features.Resenas;
using Lex.Api.Features.Trabajos.Clase;
using Lex.Api.Features.Trabajos.ProyectoCerrado;
using Lex.Api.Features.Trabajos.Salud;
using Lex.Api.Features.Trabajos.Shared;
using Lex.Api.Features.Turnos;
using Lex.Api.Domain.Entities;
using Lex.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Features.Demo;

/// <summary>
/// Seeder de datos de DEMOSTRACIÓN, separado del seeder base (roles, catálogo,
/// instituciones/carreras, admin). Puebla la plataforma para una presentación:
/// estudiantes, servicios, clientes, una agencia, pacientes (Humano/Animal),
/// trabajos TPT en distintos estados y reseñas bidireccionales.
///
/// Estrategia de idempotencia: "borrar y recargar". <see cref="SeedAsync"/> primero
/// elimina TODOS los datos demo previos (email "@demo.com") y luego los recrea.
///
/// Reutiliza la lógica de negocio real: los services de contratación por vertical
/// (Sub-hito 1.2) para nacer cada trabajo con sus snapshots, la máquina de estados
/// compartida (<see cref="ITrabajoService"/>) para avanzarlos, y
/// <see cref="IResenaService"/> para las reseñas. Las fechas se "retrodatan" al final.
///
/// Los pagos NO se siembran a mano: desde Sub-hito 1.3 Parte 2 el escrow lo crea la
/// contratación y lo resuelve la máquina de estados, así que el seeder los obtiene como
/// efecto de usar el flujo real. Solo se retrodatan sus fechas junto con las del trabajo.
/// </summary>
public class DemoService : IDemoService
{
    private readonly AppDbContext _db;
    private readonly ITrabajoProyectoCerradoService _trabajosPc;
    private readonly ITrabajoClaseService _trabajosClase;
    private readonly ITrabajoSaludService _trabajosSalud;
    private readonly ITrabajoService _trabajos;
    private readonly ITurnoService _turnos;
    private readonly IResenaService _resenas;

    private const string DemoEmailSuffix = "@demo.com";
    private const string DemoPassword = "Demo1234";

    public DemoService(
        AppDbContext db,
        ITrabajoProyectoCerradoService trabajosPc,
        ITrabajoClaseService trabajosClase,
        ITrabajoSaludService trabajosSalud,
        ITrabajoService trabajos,
        ITurnoService turnos,
        IResenaService resenas)
    {
        _db = db;
        _trabajosPc = trabajosPc;
        _trabajosClase = trabajosClase;
        _trabajosSalud = trabajosSalud;
        _trabajos = trabajos;
        _turnos = turnos;
        _resenas = resenas;
    }

    // ---------------------------------------------------------------------
    // Catálogo de datos demo (definiciones declarativas)
    // ---------------------------------------------------------------------

    private record EstudianteSeed(string Email, string Nombre, string Carrera, int Anio, string Bio);

    private record ServicioPcSeed(string Key, string EstudianteEmail, string CatalogoNombre, string Titulo,
        string Descripcion, decimal Precio, int PlazoDias, FormatoEntrega Formato, string FotoId);
    private record ServicioClaseSeed(string Key, string EstudianteEmail, string Titulo, string Descripcion,
        string Materia, NivelClase Nivel, ModalidadClase Modalidad, int Duracion, decimal Precio,
        bool EsPaquete, int? Sesiones, string FotoId);
    private record ServicioSaludSeed(string Key, string EstudianteEmail, string CatalogoNombre, string SupervisorMatricula,
        string Titulo, string Descripcion, decimal Precio, ModalidadSalud Modalidad, int Duracion, string FotoId);

    private static string FotoUrl(string fotoId) =>
        $"https://images.unsplash.com/photo-{fotoId}?w=600&h=400&fit=crop&q=80&auto=format";

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
        new("florencia@demo.com", "Florencia Aguirre", "Veterinaria",                                 4, "Practicante de veterinaria. Atiendo mascotas bajo supervisión de profesionales matriculados."),
    };

    private static readonly ServicioPcSeed[] ServiciosPc =
    {
        new("logo",        "camila@demo.com", "Diseño de logotipo",                  "Diseño de logo profesional",              "Diseño de logo a medida con 3 propuestas iniciales, hasta 2 rondas de cambios y archivos finales en alta calidad.", 25000m,  5, FormatoEntrega.Archivos, "1626785774573-4b799315345d"),
        new("identidad",   "camila@demo.com", "Identidad de marca completa",         "Identidad de marca completa",             "Logo, paleta de colores, tipografías y manual de marca básico para tu emprendimiento.", 60000m, 10, FormatoEntrega.Ambos,    "1558655146-9f40138edfeb"),
        new("ilustracion", "camila@demo.com", "Ilustración digital",                 "Ilustración digital / flyer",             "Ilustración digital personalizada o diseño de flyer para eventos y promociones.", 15000m,  3, FormatoEntrega.Archivos, "1611532736597-de2d4265fba3"),

        new("web",         "mateo@demo.com",  "Desarrollo de sitio web informativo", "Desarrollo de sitio web autogestionable", "Sitio web responsivo y autogestionable, con formulario de contacto y optimización para buscadores.", 180000m, 21, FormatoEntrega.Link,     "1461749280684-dccba630e2f6"),
        new("ecommerce",   "mateo@demo.com",  "Desarrollo de e-commerce",            "Tienda online completa",                  "Tienda online con catálogo de productos, carrito de compras y pasarela de pago integrada.", 250000m, 30, FormatoEntrega.Link,     "1563013544-824ae1b704d3"),
        new("basedatos",   "mateo@demo.com",  "Base de datos y modelado",            "Modelado de base de datos",               "Modelo de datos, diagrama entidad-relación y script de creación listo para producción.", 45000m,  7, FormatoEntrega.Archivos, "1544383835-bda2bc66a55d"),
    };

    private static readonly ServicioClaseSeed[] ServiciosClase =
    {
        new("programacion", "mateo@demo.com",     "Apoyo en programación (Python/Java)", "Clases particulares de programación: estructuras, POO y resolución de trabajos prácticos.", "Programación", NivelClase.Universitario, ModalidadClase.Online,     90, 8000m, false, null, "1515879218367-8466d910aaa4"),
        new("ingles",       "martina@demo.com",   "Clases de inglés conversacional",     "Clases de inglés conversacional adaptadas a tu nivel, con material y práctica oral.",       "Inglés",       NivelClase.Adulto,        ModalidadClase.Ambas,      60, 7000m, true,     8, "1543002588-bfa74002ed7e"),
        new("calculo",      "benjamin@demo.com",  "Apoyo en cálculo y álgebra",          "Apoyo escolar y universitario en cálculo, álgebra y preparación de exámenes finales.",      "Matemática",   NivelClase.Universitario, ModalidadClase.Online,     60, 6500m, false, null, "1635070041078-e363dbe005cb"),
        new("anatomia",     "juan@demo.com",      "Clases de anatomía para ingresantes", "Clases de anatomía orientadas a ingresantes de carreras de salud, con material de estudio.", "Anatomía",     NivelClase.Universitario, ModalidadClase.Presencial, 90, 7500m, false, null, "1530026405186-ed1f139313f8"),
        new("contabilidad", "lucia@demo.com",     "Apoyo en contabilidad e impuestos",   "Apoyo en contabilidad básica, monotributo e impuestos para estudiantes y emprendedores.",   "Contabilidad", NivelClase.Universitario, ModalidadClase.Ambas,      60, 9000m, false, null, "1554224154-26032ffc0d07"),
    };

    private record DisponibilidadSeed(string EstudianteEmail, DiaSemana[] Dias, int HoraInicio, int HoraFin);

    // Agenda semanal de los estudiantes que ofrecen Clase o Salud (las verticales con
    // turnos). Horas locales de Argentina. Los turnos NO se siembran: nacen al contratar,
    // que es Hito 2 Parte 2.
    private static readonly DisponibilidadSeed[] Disponibilidades =
    {
        // Camila (Diseño): tardes de lunes, miércoles y viernes.
        new("camila@demo.com",   new[] { DiaSemana.Lunes, DiaSemana.Miercoles, DiaSemana.Viernes }, 14, 18),
        // Sofía (Odontología): mañanas de martes y jueves.
        new("sofia@demo.com",    new[] { DiaSemana.Martes, DiaSemana.Jueves }, 9, 13),
        // Benjamín (Matemática): noches de lunes y martes.
        new("benjamin@demo.com", new[] { DiaSemana.Lunes, DiaSemana.Martes }, 18, 21),
        // Martina (Inglés): dos bloques el mismo día, para ejercitar el corte del mediodía.
        // Su paquete es de 8 clases, así que necesita agenda ancha para poder agendarlo entero.
        new("martina@demo.com",  new[] { DiaSemana.Miercoles, DiaSemana.Viernes }, 10, 12),
        new("martina@demo.com",  new[] { DiaSemana.Miercoles, DiaSemana.Viernes }, 16, 20),
        // Florencia (Veterinaria): sábados a la mañana.
        new("florencia@demo.com", new[] { DiaSemana.Sabado }, 9, 12),
        // Los tres siguientes tienen servicios de Clase que el seed contrata: sin agenda
        // publicada, la contratación con turnos no tendría dónde agendarlos.
        // Juan (Anatomía, 90 min): mañanas de lunes y miércoles.
        new("juan@demo.com",     new[] { DiaSemana.Lunes, DiaSemana.Miercoles }, 8, 12),
        // Lucía (Contabilidad, 60 min): tardes de martes y jueves.
        new("lucia@demo.com",    new[] { DiaSemana.Martes, DiaSemana.Jueves }, 15, 19),
        // Mateo (Programación, 90 min): noches de martes y jueves.
        new("mateo@demo.com",    new[] { DiaSemana.Martes, DiaSemana.Jueves }, 19, 22),
    };

    private static readonly ServicioSaludSeed[] ServiciosSalud =
    {
        new("odonto-consulta", "sofia@demo.com",     "Consulta odontológica básica",           "12345", "Consulta odontológica de práctica supervisada", "Consulta y diagnóstico odontológico inicial, realizado como práctica supervisada por un profesional matriculado.", 12000m, ModalidadSalud.Consultorio, 45, "1606811841689-23dfddce3e95"),
        new("odonto-higiene",  "sofia@demo.com",     "Higiene bucal completa",                 "12345", "Higiene bucal completa supervisada",            "Limpieza y profilaxis bucal completa, realizada bajo supervisión de un profesional matriculado.", 15000m, ModalidadSalud.Consultorio, 60, "1588776814546-1ffcf47267a5"),

        new("vet-consulta",    "florencia@demo.com", "Consulta clínica general (Veterinaria)", "67890", "Consulta clínica veterinaria supervisada",      "Consulta clínica general de tu mascota, realizada bajo supervisión de un profesional matriculado.", 10000m, ModalidadSalud.Ambas,     40, "1548767797-d8c844163c4c"),
        new("vet-vacunas",     "florencia@demo.com", "Aplicación de vacunas (Veterinaria)",    "67890", "Plan de vacunación supervisado",                "Aplicación del plan de vacunación de tu mascota, bajo supervisión de un profesional matriculado.", 8000m, ModalidadSalud.Domicilio, 30, "1576201836106-db1758fd1c97"),
    };

    // ---------------------------------------------------------------------
    // SEED
    // ---------------------------------------------------------------------
    public async Task<DemoSeedResponse> SeedAsync()
    {
        await BorrarDatosDemoAsync();

        var ahora = DateTime.UtcNow;
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);

        var roles = await _db.Roles.ToDictionaryAsync(r => r.Nombre, r => r.RolId);
        var carreras = await _db.Carreras.ToListAsync();
        var catalogo = await _db.CatalogoServicios.ToDictionaryAsync(c => c.Nombre, c => c.Id);
        var supervisores = await _db.ProfesionalesSupervisores.ToDictionaryAsync(p => p.Matricula, p => p.Id);

        int CatalogoId(string n) => catalogo.TryGetValue(n, out var id)
            ? id : throw new InvalidOperationException($"Falta la entrada de catálogo '{n}'. ¿Se ejecutó DbSeeder?");
        int SupervisorId(string m) => supervisores.TryGetValue(m, out var id)
            ? id : throw new InvalidOperationException($"Falta el supervisor con matrícula '{m}'. ¿Se ejecutó DbSeeder?");
        int RolId(string n) => roles.TryGetValue(n, out var id)
            ? id : throw new InvalidOperationException($"Falta el rol base '{n}'. ¿Se ejecutó DbSeeder?");
        int CarreraId(string n) => carreras.FirstOrDefault(c => c.Nombre == n)?.CarreraId
            ?? throw new InvalidOperationException($"Falta la carrera '{n}' en el catálogo base.");

        // 2) Estudiantes (Cliente Particular + perfil de estudiante).
        var estudianteIds = new Dictionary<string, int>();
        int idxEst = 0;
        foreach (var e in Estudiantes)
        {
            var u = new Usuario
            {
                Email = e.Email,
                PasswordHash = passwordHash,
                NombreCompleto = e.Nombre,
                FechaRegistro = ahora.AddDays(-130 + idxEst),
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

        foreach (var e in Estudiantes)
            estudianteIds[e.Email] = await _db.Usuarios.Where(u => u.Email == e.Email).Select(u => u.UsuarioId).FirstAsync();
        int empresaId = empresa.UsuarioId;
        int diegoId = clienteUsuarios["diego@demo.com"].UsuarioId;
        int veronicaId = clienteUsuarios["veronica@demo.com"].UsuarioId;
        int robertoId = clienteUsuarios["roberto@demo.com"].UsuarioId;

        // 4) Pacientes: Humanos (Verónica) + Animales (Roberto, Diego).
        var lucasPaz = new Paciente
        {
            ClienteResponsableId = veronicaId, Tipo = TipoPaciente.Humano, NombreCompleto = "Lucas Paz",
            EsTitular = false, FechaNacimiento = ahora.AddYears(-9), Dni = "55123456",
            ContactoEmergenciaNombre = "Verónica Paz", ContactoEmergenciaTelefono = "3624555111",
            NotasRelevantes = "Sin antecedentes relevantes."
        };
        var martinaPaz = new Paciente
        {
            ClienteResponsableId = veronicaId, Tipo = TipoPaciente.Humano, NombreCompleto = "Martina Paz",
            EsTitular = false, FechaNacimiento = ahora.AddYears(-6), Dni = "55234567",
            ContactoEmergenciaNombre = "Verónica Paz", ContactoEmergenciaTelefono = "3624555111",
            NotasRelevantes = "Control de rutina."
        };
        var firulais = new Paciente
        {
            ClienteResponsableId = robertoId, Tipo = TipoPaciente.Animal, NombreCompleto = "Firulais",
            Especie = "Perro", Raza = "Labrador", FechaNacimiento = ahora.AddYears(-3),
            NotasRelevantes = "Vacunas al día. Sociable."
        };
        var michi = new Paciente
        {
            ClienteResponsableId = diegoId, Tipo = TipoPaciente.Animal, NombreCompleto = "Michi",
            Especie = "Gato", Raza = "Siamés", FechaNacimiento = ahora.AddYears(-2),
            NotasRelevantes = "Castrado."
        };
        _db.Pacientes.AddRange(lucasPaz, martinaPaz, firulais, michi);

        // 5) Servicios (entidades directas, para controlar la fecha de publicación).
        int idxSrv = 0;
        foreach (var s in ServiciosPc)
        {
            _db.ServiciosProyectoCerrado.Add(new ServicioProyectoCerrado
            {
                EstudianteId = estudianteIds[s.EstudianteEmail],
                Titulo = s.Titulo,
                Descripcion = s.Descripcion,
                ImagenUrl = FotoUrl(s.FotoId),
                Precio = s.Precio,
                Activo = true,
                FechaPublicacion = ahora.AddDays(-100 + idxSrv),
                CatalogoServicioId = CatalogoId(s.CatalogoNombre),
                PlazoEntregaDias = s.PlazoDias,
                RevisionesIncluidas = 2,
                FormatoEntrega = s.Formato
            });
            idxSrv++;
        }
        foreach (var s in ServiciosClase)
        {
            _db.ServiciosClase.Add(new ServicioClase
            {
                EstudianteId = estudianteIds[s.EstudianteEmail],
                Titulo = s.Titulo,
                Descripcion = s.Descripcion,
                ImagenUrl = FotoUrl(s.FotoId),
                Precio = s.Precio,
                Activo = true,
                FechaPublicacion = ahora.AddDays(-100 + idxSrv),
                Materia = s.Materia,
                Nivel = s.Nivel,
                Modalidad = s.Modalidad,
                DuracionMinutosSesion = s.Duracion,
                EsPaquete = s.EsPaquete,
                CantidadSesionesPaquete = s.Sesiones
            });
            idxSrv++;
        }
        foreach (var s in ServiciosSalud)
        {
            _db.ServiciosSalud.Add(new ServicioSalud
            {
                EstudianteId = estudianteIds[s.EstudianteEmail],
                Titulo = s.Titulo,
                Descripcion = s.Descripcion,
                ImagenUrl = FotoUrl(s.FotoId),
                Precio = s.Precio,
                Activo = true,
                FechaPublicacion = ahora.AddDays(-100 + idxSrv),
                CatalogoServicioId = CatalogoId(s.CatalogoNombre),
                SupervisorId = SupervisorId(s.SupervisorMatricula),
                Modalidad = s.Modalidad,
                DuracionMinutosSesion = s.Duracion
            });
            idxSrv++;
        }

        // 5b) Disponibilidad semanal de los estudiantes de Clase/Salud.
        foreach (var d in Disponibilidades)
        {
            foreach (var dia in d.Dias)
            {
                _db.DisponibilidadesEstudiante.Add(new DisponibilidadEstudiante
                {
                    EstudianteId = estudianteIds[d.EstudianteEmail],
                    DiaSemana = dia,
                    HoraInicio = new TimeOnly(d.HoraInicio, 0),
                    HoraFin = new TimeOnly(d.HoraFin, 0),
                    Activo = true,
                    FechaCreacion = ahora.AddDays(-100)
                });
            }
        }

        await _db.SaveChangesAsync(); // servicios, pacientes y disponibilidad ahora tienen id

        var servicioIds = new Dictionary<string, int>();
        foreach (var (key, email, titulo) in ServiciosPc.Select(x => (x.Key, x.EstudianteEmail, x.Titulo))
            .Concat(ServiciosClase.Select(x => (x.Key, x.EstudianteEmail, x.Titulo)))
            .Concat(ServiciosSalud.Select(x => (x.Key, x.EstudianteEmail, x.Titulo))))
        {
            servicioIds[key] = await _db.Servicios
                .Where(x => x.Titulo == titulo && x.EstudianteId == estudianteIds[email])
                .Select(x => x.Id)
                .FirstAsync();
        }

        // 6) Trabajos: nacen con el service de contratación de su vertical y avanzan
        //    con la máquina de estados compartida; luego se retrodatan las fechas.

        // --- Completados (habilitan reseñas) ---
        var t1 = await CrearTrabajoPcAsync(diegoId,    servicioIds["logo"],      EstadoTrabajo.Completado, ahora.AddDays(-45), 6);
        var t2 = await CrearTrabajoPcAsync(empresaId,  servicioIds["web"],       EstadoTrabajo.Completado, ahora.AddDays(-38), 18);
        var t3 = await CrearTrabajoClaseAsync(robertoId, servicioIds["calculo"], EstadoTrabajo.Completado, ahora.AddDays(-30), 2,
            "Necesito repasar límites y continuidad para el final.");
        var t4 = await CrearTrabajoClaseAsync(robertoId, servicioIds["anatomia"], EstadoTrabajo.Completado, ahora.AddDays(-28), 3);
        var t5 = await CrearTrabajoPcAsync(empresaId,  servicioIds["ecommerce"], EstadoTrabajo.Completado, ahora.AddDays(-24), 12);
        // Paquete de 8 clases: 8 turnos + 8 sesiones en una sola contratación.
        var t6 = await CrearTrabajoClaseAsync(diegoId, servicioIds["ingles"], EstadoTrabajo.Completado, ahora.AddDays(-16), 2,
            "Quiero enfocarme en conversación para entrevistas de trabajo.");
        // Salud completo (Humano en Odonto): paciente + consentimiento firmado.
        var t7 = await CrearTrabajoSaludAsync(veronicaId, servicioIds["odonto-consulta"], lucasPaz.Id, EstadoTrabajo.Completado, ahora.AddDays(-12), 3);

        // --- En curso ---
        var t8 = await CrearTrabajoPcAsync(empresaId, servicioIds["identidad"],   EstadoTrabajo.EnCurso, ahora.AddDays(-6), 0);
        var t9 = await CrearTrabajoPcAsync(robertoId, servicioIds["ilustracion"], EstadoTrabajo.EnCurso, ahora.AddDays(-4), 0);
        // Salud en curso (Animal en Vet): consentimiento firmado -> puede iniciar.
        var t10 = await CrearTrabajoSaludAsync(robertoId, servicioIds["vet-consulta"], firulais.Id, EstadoTrabajo.EnCurso, ahora.AddDays(-3), 0,
            "Firulais viene comiendo poco hace una semana.");

        // --- Aceptado ---
        var t11 = await CrearTrabajoPcAsync(diegoId, servicioIds["basedatos"], EstadoTrabajo.Aceptado, ahora.AddDays(-3), 0);

        // --- Cancelado (ejercita el reembolso del escrow) ---
        await CrearTrabajoPcCanceladoAsync(veronicaId, servicioIds["logo"], ahora.AddDays(-20),
            "El cliente canceló antes de que el estudiante aceptara.");

        // --- Pendientes ---
        var t12 = await CrearTrabajoClaseAsync(veronicaId, servicioIds["contabilidad"], EstadoTrabajo.Pendiente, ahora.AddDays(-2), 0);
        // Salud pendiente (Humano en Odonto): consentimiento firmado, esperando aceptación.
        var t13 = await CrearTrabajoSaludAsync(veronicaId, servicioIds["odonto-higiene"], martinaPaz.Id, EstadoTrabajo.Pendiente, ahora.AddDays(-1), 0);

        // 7) Reseñas bidireccionales en los completados.
        await ResenarAsync(t1, diegoId,    5, "Quedó espectacular el logo, súper profesional y atento a los cambios.", 5, "Cliente claro con lo que buscaba, un gusto trabajar así.");
        await ResenarAsync(t2, empresaId,  5, "La web quedó impecable y entregó antes de lo pactado.",                  4, "Buena comunicación durante todo el proyecto, recomendable.");
        await ResenarAsync(t3, robertoId,  4, "Muy claro explicando, me destrabó cálculo para el final.",              5, "Alumno muy aplicado y comprometido.");
        await ResenarAsync(t4, robertoId,  4, "Buenas clases de anatomía, bien organizado el material.",               3, "Llegó tarde a una clase pero después todo bien.");
        await ResenarAsync(t5, empresaId,  3, "Buena la tienda online, aunque tardó en responder algunos mensajes.", 4, "Todo en orden, pagos puntuales.");
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
                : "Datos de demostración eliminados. Los datos base (roles, catálogo, instituciones, admin) no se tocaron.",
            UsuariosEliminados = eliminados
        };
    }

    // ---------------------------------------------------------------------
    // Helpers de ciclo de vida (reutilizan los services por vertical + shared)
    // ---------------------------------------------------------------------

    private async Task<(int Id, int EstudianteId)> CrearTrabajoPcAsync(
        int clienteId, int servicioId, EstadoTrabajo objetivo, DateTime creado, int durDias)
    {
        var t = await _trabajosPc.ContratarAsync(clienteId, new ContratarTrabajoProyectoCerradoRequest { ServicioId = servicioId });
        await AvanzarAsync(t.Id, t.EstudianteId, clienteId, objetivo);
        await RetrodatarAsync(t.Id, creado, durDias);
        return (t.Id, t.EstudianteId);
    }

    // Cancelado no entra en AvanzarAsync: su valor en el enum es mayor que Completado, asi
    // que la comparacion por orden avanzaria el trabajo hasta completarlo. Ademas el caso
    // que interesa es el corte temprano: el cliente cancela con el trabajo aun Pendiente.
    private async Task<(int Id, int EstudianteId)> CrearTrabajoPcCanceladoAsync(
        int clienteId, int servicioId, DateTime creado, string motivo)
    {
        var t = await _trabajosPc.ContratarAsync(clienteId, new ContratarTrabajoProyectoCerradoRequest { ServicioId = servicioId });
        await _trabajos.CancelarAsync(clienteId, t.Id, motivo);
        await RetrodatarAsync(t.Id, creado, 0);
        return (t.Id, t.EstudianteId);
    }

    private async Task<(int Id, int EstudianteId)> CrearTrabajoClaseAsync(
        int clienteId, int servicioId, EstadoTrabajo objetivo, DateTime creado, int durDias, string? notas = null)
    {
        var servicio = await _db.ServiciosClase.AsNoTracking().FirstAsync(s => s.Id == servicioId);
        var cantidad = servicio.EsPaquete ? servicio.CantidadSesionesPaquete ?? 1 : 1;
        var slots = await ElegirSlotsLibresAsync(servicio.EstudianteId, servicio.DuracionMinutosSesion, cantidad);

        var t = await _trabajosClase.ContratarAsync(clienteId, new ContratarTrabajoClaseRequest
        {
            ServicioId = servicioId,
            SlotsElegidos = slots,
            NotasCliente = notas
        });
        await AvanzarAsync(t.Id, t.EstudianteId, clienteId, objetivo);
        await RetrodatarAsync(t.Id, creado, durDias);
        return (t.Id, t.EstudianteId);
    }

    private async Task<(int Id, int EstudianteId)> CrearTrabajoSaludAsync(
        int clienteId, int servicioId, int pacienteId, EstadoTrabajo objetivo, DateTime creado, int durDias, string? notas = null)
    {
        var servicio = await _db.ServiciosSalud.AsNoTracking().FirstAsync(s => s.Id == servicioId);
        var slots = await ElegirSlotsLibresAsync(servicio.EstudianteId, servicio.DuracionMinutosSesion, 1);

        var t = await _trabajosSalud.ContratarAsync(clienteId, new ContratarTrabajoSaludRequest
        {
            ServicioId = servicioId,
            PacienteId = pacienteId,
            SlotElegido = slots[0],
            NotasCliente = notas
        });
        // El cliente firma el consentimiento (obligatorio para poder iniciar).
        await _trabajosSalud.FirmarConsentimientoAsync(clienteId, t.Id, "127.0.0.1");
        await AvanzarAsync(t.Id, t.EstudianteId, clienteId, objetivo);
        await RetrodatarAsync(t.Id, creado, durDias);
        return (t.Id, t.EstudianteId);
    }

    // El seed agenda con el mismo buscador de huecos que usa un cliente real, en vez de
    // inventar fechas: asi los turnos sembrados pasan las validaciones de contratacion y
    // caen siempre dentro de la disponibilidad publicada.
    //
    // Los turnos de los trabajos ya terminados se mandan al pasado despues, en
    // RetrodatarAsync: aca todos se agendan a futuro porque reservar hacia atras no se puede.
    private async Task<List<DateTime>> ElegirSlotsLibresAsync(int estudianteId, int duracionMinutos, int cantidad)
    {
        var hoy = DateOnly.FromDateTime(HorarioArgentina.AHoraLocal(DateTime.UtcNow));
        var libres = await _turnos.ListarSlotsDisponiblesAsync(estudianteId, hoy, hoy.AddDays(60), duracionMinutos);

        if (libres.Count < cantidad)
            throw new InvalidOperationException(
                $"El estudiante {estudianteId} tiene {libres.Count} horarios libres de {duracionMinutos} min " +
                $"en los próximos 60 días, pero el seed necesita {cantidad}. Revisá su disponibilidad en DemoService.");

        return libres.Take(cantidad).Select(s => s.FechaHoraInicio).ToList();
    }

    // Recorre las transiciones permitidas hasta el estado objetivo, respetando qué
    // parte está autorizada en cada paso (estudiante acepta/inicia/entrega, cliente completa).
    private async Task AvanzarAsync(int idTrabajo, int estudianteId, int clienteId, EstadoTrabajo objetivo)
    {
        var meta = (int)objetivo;
        if (meta >= (int)EstadoTrabajo.Aceptado)
            await _trabajos.AceptarAsync(estudianteId, idTrabajo);
        if (meta >= (int)EstadoTrabajo.EnCurso)
            await _trabajos.IniciarAsync(estudianteId, idTrabajo);
        if (meta >= (int)EstadoTrabajo.Entregado)
            await _trabajos.EntregarAsync(estudianteId, idTrabajo);
        if (meta >= (int)EstadoTrabajo.Completado)
            await _trabajos.CompletarAsync(clienteId, idTrabajo);
    }

    private async Task ResenarAsync((int Id, int EstudianteId) trabajo, int clienteId,
        int puntajeCliente, string comentarioCliente, int puntajeEstudiante, string comentarioEstudiante)
    {
        var rCliente = await _resenas.CrearAsync(clienteId, trabajo.Id, new CrearResenaRequest { Puntaje = puntajeCliente, Comentario = comentarioCliente });
        var rEstudiante = await _resenas.CrearAsync(trabajo.EstudianteId, trabajo.Id, new CrearResenaRequest { Puntaje = puntajeEstudiante, Comentario = comentarioEstudiante });

        var fin = await _db.Trabajos.Where(t => t.Id == trabajo.Id).Select(t => t.FechaFin).FirstAsync() ?? DateTime.UtcNow;
        var e1 = await _db.Resenas.FindAsync(rCliente.Id);
        var e2 = await _db.Resenas.FindAsync(rEstudiante.Id);
        if (e1 is not null) e1.Fecha = fin.AddDays(1);
        if (e2 is not null) e2.Fecha = fin.AddDays(2);
    }

    // Reescribe las fechas del trabajo (y su historial / consentimiento / pago) para que
    // la línea de tiempo quede distribuida y coherente.
    private async Task RetrodatarAsync(int idTrabajo, DateTime creado, int durDias)
    {
        var t = await _db.Trabajos
            .Include(x => x.Historiales)
            .FirstAsync(x => x.Id == idTrabajo);

        t.FechaCreacion = creado;
        var hPend = t.Historiales.FirstOrDefault(h => h.EstadoNuevo == EstadoTrabajo.Pendiente);
        if (hPend is not null) hPend.Fecha = creado;

        // Consentimiento (solo trabajos de salud): la evidencia se firma al contratar.
        var consentimiento = await _db.Consentimientos.FirstOrDefaultAsync(c => c.TrabajoSaludId == idTrabajo);
        if (consentimiento is not null) consentimiento.FechaAceptacion = creado;

        var aceptado = creado.AddDays(1);
        var hAcc = t.Historiales.FirstOrDefault(h => h.EstadoNuevo == EstadoTrabajo.Aceptado);
        if (hAcc is not null) hAcc.Fecha = aceptado;

        var enCurso = creado.AddDays(2);
        var hEC = t.Historiales.FirstOrDefault(h => h.EstadoNuevo == EstadoTrabajo.EnCurso);
        if (hEC is not null)
        {
            hEC.Fecha = enCurso;
            t.FechaInicio = enCurso;
        }

        var entregado = creado.AddDays(Math.Max(durDias - 1, 3));
        var hEnt = t.Historiales.FirstOrDefault(h => h.EstadoNuevo == EstadoTrabajo.Entregado);
        if (hEnt is not null) hEnt.Fecha = entregado;

        // Cierre: el trabajo termina al completarse o al cancelarse, lo que haya pasado.
        var fin = creado.AddDays(Math.Max(durDias, 4));
        var hFin = t.Historiales.FirstOrDefault(h => h.EstadoNuevo is EstadoTrabajo.Completado or EstadoTrabajo.Cancelado);
        if (hFin is not null)
        {
            hFin.Fecha = fin;
            t.FechaFin = fin;
        }

        // El escrow nace al contratar y se resuelve al cerrar el trabajo: sus asientos
        // siguen esa misma línea de tiempo.
        var pago = await _db.Pagos
            .Include(p => p.Movimientos)
            .FirstOrDefaultAsync(p => p.TrabajoId == idTrabajo);
        if (pago is not null)
        {
            var cierre = t.FechaFin ?? fin;
            pago.FechaCreacion = creado;
            if (pago.FechaLiberacion is not null)
                pago.FechaLiberacion = cierre;
            foreach (var m in pago.Movimientos)
                m.FechaMovimiento = m.Tipo == TipoMovimientoPago.Retencion ? creado : cierre;
        }

        await RetrodatarAgendaAsync(t, creado, fin);

        await _db.SaveChangesAsync();
    }

    // Los turnos se agendan siempre a futuro (no se puede reservar hacia atrás), así que un
    // trabajo ya terminado queda con la agenda en el lugar equivocado. Acá se la lleva al
    // pasado y se pone cada sesión en el estado que le corresponde a su trabajo.
    //
    // Se conserva la hora del día original y se cambia solo la fecha: el turno ya no
    // coincidirá con el día de semana de la disponibilidad, y está bien: un turno viejo es
    // independiente del bloque que lo originó, que además pudo haber cambiado desde entonces.
    private async Task RetrodatarAgendaAsync(Trabajo t, DateTime creado, DateTime fin)
    {
        var sesiones = await _db.Sesiones
            .Include(s => s.Turno)
            .Where(s => s.TrabajoId == t.Id)
            .OrderBy(s => s.NumeroSesion)
            .ToListAsync();

        if (sesiones.Count == 0)
            return;

        switch (t.Estado)
        {
            // Trabajo cumplido: toda la agenda ya ocurrió y se dio.
            case EstadoTrabajo.Completado or EstadoTrabajo.Entregado:
                for (var i = 0; i < sesiones.Count; i++)
                {
                    // Una sesión cada 2 días, terminando en la fecha de cierre del trabajo.
                    var fecha = ConservandoLaHora(sesiones[i].Turno.FechaHoraInicio, fin.AddDays(-2 * (sesiones.Count - 1 - i)));
                    sesiones[i].Turno.FechaHoraInicio = fecha;
                    sesiones[i].Turno.Estado = EstadoTurno.Realizado;
                    sesiones[i].Turno.FechaCreacion = creado;
                    sesiones[i].Estado = EstadoSesion.Realizada;
                    sesiones[i].FechaRealizada = fecha;
                }
                if (t is TrabajoClase claseCumplida)
                    claseCumplida.SesionesCompletadas = sesiones.Count;
                break;

            // Trabajo cancelado: la agenda se cae con él.
            case EstadoTrabajo.Cancelado:
                foreach (var s in sesiones)
                {
                    s.Turno.Estado = EstadoTurno.Cancelado;
                    s.Turno.FechaCreacion = creado;
                    s.Estado = EstadoSesion.Cancelada;
                }
                break;

            // Pendiente / Aceptado / EnCurso / Disputa: la agenda es lo que está por venir.
            // Los turnos quedan donde se agendaron (a futuro) y sus sesiones, Pendientes.
            default:
                foreach (var s in sesiones)
                    s.Turno.FechaCreacion = creado;
                break;
        }
    }

    private static DateTime ConservandoLaHora(DateTime original, DateTime nuevaFecha) =>
        DateTime.SpecifyKind(nuevaFecha.Date + original.TimeOfDay, DateTimeKind.Utc);

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

        var trabajoIds = await _db.Trabajos
            .Where(t => demoUserIds.Contains(t.EstudianteId) || demoUserIds.Contains(t.ClienteId))
            .Select(t => t.Id)
            .ToListAsync();

        var solicitudIds = await _db.Solicitudes
            .Where(s => demoUserIds.Contains(s.ClienteId))
            .Select(s => s.Id)
            .ToListAsync();

        // Reseñas, pagos e historial cuelgan de la tabla base 'trabajo' -> antes que ella.
        _db.Resenas.RemoveRange(_db.Resenas.Where(r => trabajoIds.Contains(r.TrabajoId)
            || demoUserIds.Contains(r.AutorUsuarioId) || demoUserIds.Contains(r.ReceptorUsuarioId)));
        // Sesiones antes que sus turnos (FK Restrict) y antes que su trabajo. Hoy el seed no
        // crea ninguna, pero desde Hito 2 Parte 2 los turnos nacen al contratar.
        _db.Sesiones.RemoveRange(_db.Sesiones.Where(s => trabajoIds.Contains(s.TrabajoId)));
        // Movimientos antes que su pago (FK) y antes del historial (traza opcional).
        var pagoIds = await _db.Pagos.Where(p => trabajoIds.Contains(p.TrabajoId)).Select(p => p.Id).ToListAsync();
        _db.MovimientosPago.RemoveRange(_db.MovimientosPago.Where(m => pagoIds.Contains(m.PagoId)));
        _db.Pagos.RemoveRange(_db.Pagos.Where(p => trabajoIds.Contains(p.TrabajoId)));
        _db.TrabajoHistoriales.RemoveRange(_db.TrabajoHistoriales.Where(h => trabajoIds.Contains(h.TrabajoId) || (h.UsuarioId != null && demoUserIds.Contains(h.UsuarioId.Value))));
        // Trabajos: al remover la entidad base (TPT) EF borra también la fila hija.
        _db.Trabajos.RemoveRange(_db.Trabajos.Where(t => trabajoIds.Contains(t.Id)));
        await _db.SaveChangesAsync();

        // Consentimientos: después de borrar trabajo_salud (que los referenciaba por consentimiento_id).
        _db.Consentimientos.RemoveRange(_db.Consentimientos.Where(c => trabajoIds.Contains(c.TrabajoSaludId)
            || demoUserIds.Contains(c.AceptadoPorUsuarioId)));
        _db.Postulaciones.RemoveRange(_db.Postulaciones.Where(p => solicitudIds.Contains(p.SolicitudId) || demoUserIds.Contains(p.EstudianteId)));
        _db.Solicitudes.RemoveRange(_db.Solicitudes.Where(s => solicitudIds.Contains(s.Id)));
        _db.Servicios.RemoveRange(_db.Servicios.Where(s => demoUserIds.Contains(s.EstudianteId)));
        _db.Pacientes.RemoveRange(_db.Pacientes.Where(p => demoUserIds.Contains(p.ClienteResponsableId)));
        // Agenda: los turnos ya perdieron sus sesiones arriba; la disponibilidad va antes
        // que el perfil de estudiante del que cuelga.
        _db.Turnos.RemoveRange(_db.Turnos.Where(t => demoUserIds.Contains(t.EstudianteId) || demoUserIds.Contains(t.ClienteId)));
        _db.DisponibilidadesEstudiante.RemoveRange(_db.DisponibilidadesEstudiante.Where(d => demoUserIds.Contains(d.EstudianteId)));
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
        var idsEstudiante = await _db.PerfilesEstudiante.Where(p => demoUserIds.Contains(p.UsuarioId)).Select(p => p.UsuarioId).ToListAsync();
        var clientesParticulares = await _db.PerfilesCliente.CountAsync(p =>
            demoUserIds.Contains(p.UsuarioId) && p.TipoCliente == (int)TipoCliente.Particular && !idsEstudiante.Contains(p.UsuarioId));
        var agencias = await _db.PerfilesAgencia.CountAsync(p => demoUserIds.Contains(p.UsuarioId));
        var pacientes = await _db.Pacientes.CountAsync(p => demoUserIds.Contains(p.ClienteResponsableId));
        var servicios = await _db.Servicios.CountAsync(s => demoUserIds.Contains(s.EstudianteId));

        var trabajos = await _db.Trabajos
            .Where(t => demoUserIds.Contains(t.EstudianteId) || demoUserIds.Contains(t.ClienteId))
            .Select(t => new { t.Id, t.Estado })
            .ToListAsync();
        var trabajoIds = trabajos.Select(t => t.Id).ToList();

        var resenas = await _db.Resenas.CountAsync(r => trabajoIds.Contains(r.TrabajoId));
        var bloques = await _db.DisponibilidadesEstudiante.CountAsync(d => demoUserIds.Contains(d.EstudianteId) && d.Activo);
        var turnos = await _db.Turnos.CountAsync(t => demoUserIds.Contains(t.EstudianteId));
        var sesiones = await _db.Sesiones
            .Where(s => trabajoIds.Contains(s.TrabajoId))
            .Select(s => s.Estado)
            .ToListAsync();

        // Escrow: comisión efectiva (liberada) vs. potencial (retenida).
        var pagos = await _db.Pagos.Where(p => trabajoIds.Contains(p.TrabajoId)).Select(p => new { p.Estado, Comision = p.MontoComisionCalculada }).ToListAsync();

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
            BloquesDisponibilidad = bloques,
            Turnos = turnos,
            Sesiones = sesiones.Count,
            SesionesPorEstado = sesiones.GroupBy(e => e.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            TrabajosPorEstado = trabajos.GroupBy(t => t.Estado.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            ComisionLexLiberada = pagos.Where(p => p.Estado == EstadoPago.Liberado).Sum(p => p.Comision),
            ComisionLexRetenida = pagos.Where(p => p.Estado == EstadoPago.Retenido).Sum(p => p.Comision),
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
