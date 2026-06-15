using Lex.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Data;

public static class DbSeeder
{
    // Roles base de LEX.
    private static readonly string[] RolesBase = { "Estudiante", "Cliente", "Agencia", "Admin" };

    // Catalogo base de tipos de servicio (nombre -> requiere_supervision).
    private static readonly (string Nombre, bool RequiereSupervision)[] TiposServicioBase =
    {
        ("Digital", false),
        ("Clase", false),
        ("Salud", true),
        ("Otro", false)
    };

    public static async Task SeedAsync(AppDbContext db, string adminEmail, string adminPassword)
    {
        await SeedRolesAsync(db);
        await SeedTiposServicioAsync(db);
        await SeedInstitucionesYCarrerasAsync(db);
        await SeedAdminAsync(db, adminEmail, adminPassword);
    }

    private static async Task SeedRolesAsync(AppDbContext db)
    {
        var existentes = await db.Roles.Select(r => r.Nombre).ToListAsync();
        foreach (var nombre in RolesBase.Except(existentes))
            db.Roles.Add(new Rol { Nombre = nombre });

        await db.SaveChangesAsync();
    }

    private static async Task SeedTiposServicioAsync(AppDbContext db)
    {
        var existentes = await db.TiposServicio.Select(t => t.Nombre).ToListAsync();
        foreach (var tipo in TiposServicioBase.Where(t => !existentes.Contains(t.Nombre)))
            db.TiposServicio.Add(new TipoServicio
            {
                Nombre = tipo.Nombre,
                RequiereSupervision = tipo.RequiereSupervision
            });

        await db.SaveChangesAsync();
    }

    // Catalogo institucional del NEA. Da sustento a la validacion institucional
    // y puebla los selectores de carrera/institucion del frontend.
    private static async Task SeedInstitucionesYCarrerasAsync(AppDbContext db)
    {
        // 1) Tipos de institucion.
        var tiposBase = new[] { "Universidad", "Instituto Terciario" };
        var tiposExistentes = await db.TiposInstitucion.ToListAsync();
        foreach (var nombre in tiposBase.Where(n => tiposExistentes.All(t => t.Nombre != n)))
            db.TiposInstitucion.Add(new TipoInstitucion { Nombre = nombre });
        await db.SaveChangesAsync();

        var universidad = await db.TiposInstitucion.FirstAsync(t => t.Nombre == "Universidad");
        var terciario = await db.TiposInstitucion.FirstAsync(t => t.Nombre == "Instituto Terciario");

        // 2) Instituciones con sus carreras. area_conocimiento "Salud" habilita
        //    servicios que requieren supervision.
        var catalogo = new (string Institucion, int TipoId, string Provincia, string Ciudad,
                            (string Carrera, string? Area)[] Carreras)[]
        {
            ("Universidad Nacional del Nordeste (UNNE)", universidad.TipoInstitucionId, "Corrientes", "Corrientes", new[]
            {
                ("Medicina", (string?)"Salud"),
                ("Odontología", (string?)"Salud"),
                ("Bioquímica", (string?)"Salud"),
                ("Enfermería", (string?)"Salud"),
                ("Diseño Gráfico", (string?)"Diseño"),
                ("Licenciatura en Sistemas de Información", (string?)"Tecnología"),
                ("Abogacía", (string?)"Derecho"),
                ("Contador Público", (string?)"Economía"),
            }),
            ("Universidad Tecnológica Nacional - FRRe", universidad.TipoInstitucionId, "Chaco", "Resistencia", new[]
            {
                ("Ingeniería en Sistemas de Información", (string?)"Tecnología"),
                ("Ingeniería Electrónica", (string?)"Ingeniería"),
                ("Ingeniería Química", (string?)"Ingeniería"),
            }),
            ("Universidad de la Cuenca del Plata", universidad.TipoInstitucionId, "Corrientes", "Corrientes", new[]
            {
                ("Psicología", (string?)"Salud"),
                ("Diseño de Indumentaria", (string?)"Diseño"),
                ("Licenciatura en Marketing", (string?)"Negocios"),
            }),
            ("Instituto Superior de Formación Docente y Técnica", terciario.TipoInstitucionId, "Corrientes", "Corrientes", new[]
            {
                ("Tecnicatura en Desarrollo de Software", (string?)"Tecnología"),
                ("Profesorado de Matemática", (string?)"Educación"),
            }),
        };

        foreach (var item in catalogo)
        {
            var institucion = await db.Instituciones
                .Include(i => i.Carreras)
                .FirstOrDefaultAsync(i => i.Nombre == item.Institucion);

            if (institucion is null)
            {
                institucion = new Institucion
                {
                    Nombre = item.Institucion,
                    TipoInstitucionId = item.TipoId,
                    Provincia = item.Provincia,
                    Ciudad = item.Ciudad
                };
                db.Instituciones.Add(institucion);
            }

            foreach (var (carrera, area) in item.Carreras)
            {
                if (institucion.Carreras.All(c => c.Nombre != carrera))
                    institucion.Carreras.Add(new Carrera { Nombre = carrera, AreaConocimiento = area });
            }
        }

        await db.SaveChangesAsync();
    }

    // Usuario Admin para la demo: login normal -> acceso al panel de ingresos.
    private static async Task SeedAdminAsync(AppDbContext db, string adminEmail, string adminPassword)
    {
        var email = adminEmail.Trim();
        if (await db.Usuarios.AnyAsync(u => u.Email == email))
            return;

        var rolAdmin = await db.Roles.FirstOrDefaultAsync(r => r.Nombre == "Admin")
            ?? throw new InvalidOperationException("El rol 'Admin' no fue sembrado antes del usuario admin.");

        var admin = new Usuario
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            NombreCompleto = "LEX Admin",
            FechaRegistro = DateTime.UtcNow,
            Activo = true
        };
        // Admin no tiene perfil de Estudiante/Cliente/Agencia: solo el rol.
        admin.UsuarioRoles.Add(new UsuarioRol { Rol = rolAdmin });

        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
    }
}
