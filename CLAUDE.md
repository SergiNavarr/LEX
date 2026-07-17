# LEX — Contexto para Claude Code

## Qué es LEX

Marketplace universitario del NEA (Nordeste Argentino) que conecta estudiantes universitarios verificados con clientes (PyMEs, agencias, particulares) para tres verticales:

- **ProyectoCerrado**: proyectos digitales con inicio, entrega y fin (diseño, desarrollo, etc.).
- **Clase**: tutorías y clases con turnos.
- **Salud**: prácticas supervisadas por profesional matriculado (odontología, veterinaria, etc.).

Diferenciales: verificación institucional (vínculo carrera-universidad), pagos en escrow, catálogo cerrado de servicios por carrera + año, supervisión matriculada en salud.

## Estructura del monorepo

```
Lex/
├── backend/           .NET 8 Web API (PostgreSQL en Supabase)
├── frontend/          Next.js 16 (App Router, React 19, TS, Tailwind 4)
└── (docs varios en raíz)
```

Repo actual: `github.com/somos-lex/lex-proto`.

## Stack técnico

**Backend**: .NET 8, ASP.NET Core, EF Core 8 + Npgsql, JWT/JwtBearer, BCrypt, Swashbuckle.
**Frontend**: Next.js 16, React 19, TypeScript 5, Tailwind CSS 4.
**Base de datos**: PostgreSQL en Supabase (session pooler puerto 5432).
**Deploy**: Render (backend en Docker), Vercel (frontend), Cloudflare (DNS).

## Convenciones de código — Backend

### Naming

- **Convención de PKs**: la propiedad C# se llama `Id` y mapea a la columna `id`. Aplica a todas las entidades.
- **Convención de FKs**: `<NombreEntidad>Id`, ej: `EstudianteId`, `ClienteId`, `ServicioId`.
- **PKs compuestas**: las tablas de M-a-N usan las FKs como PK compuesta (ej: `CatalogoServicioCarrera` usa `(CatalogoServicioId, CarreraId)`).

### Arquitectura: Feature Folders

Organización por feature/dominio, NO por capas técnicas:

```
backend/
├── Features/
│   ├── Auth/          → AuthController, AuthService, IAuthService, AuthDtos.cs
│   ├── Servicios/
│   │   ├── ProyectoCerrado/
│   │   ├── Clase/
│   │   ├── Salud/
│   │   └── Shared/
│   └── ...
├── Domain/
│   ├── Entities/
│   └── Enums/
├── Data/              (AppDbContext, DbSeeder, Migrations)
└── Common/
```

Cada feature agrupa controller + service + interface + dtos en una sola carpeta.

### Estilo de mapeo EF Core

- **Columnas**: Data Annotations (`[Table]`, `[Column]`) directamente en las entidades.
- **Relaciones, TPT, PKs compuestas, conversión de enums a string**: Fluent API en `AppDbContext.OnModelCreating`.

### Enums

- Enums NUEVOS se guardan como **string** en DB (`HasConversion<string>()`).
- Enums existentes (previos al refactor) siguen guardándose como int por deuda técnica. NO cambiar sin consultar.

### Herencia (TPT)

Servicio y Trabajo son clases abstractas con 3 subclases cada una (ProyectoCerrado, Clase, Salud). Configuración con Fluent API:

```csharp
modelBuilder.Entity<Servicio>().UseTptMappingStrategy();
modelBuilder.Entity<Servicio>().ToTable("servicio");
modelBuilder.Entity<ServicioProyectoCerrado>().ToTable("servicio_proyecto_cerrado");
```

### DateTime

- **SIEMPRE usar `DateTime.UtcNow`**, nunca `DateTime.Now` ni `DateTime.Today`.
- Columnas de fecha son `timestamp with time zone` (timestamptz).
- Npgsql exige `Kind = Utc` para timestamptz. Si viene un DateTime de DTO, hacer `DateTime.SpecifyKind(fecha, DateTimeKind.Utc)`.

### Migrations

- Framework: EF Core.
- Provider: Npgsql (PostgreSQL).
- **Session pooler (puerto 5432)** para migrations y todo el runtime. NO usar el transaction pooler (6543) — colgó las migrations con "Timeout during reading attempt" en el pasado.
- Generar: `dotnet ef migrations add <Nombre> --project Lex.Api.csproj`.
- Aplicar: `dotnet ef database update --project Lex.Api.csproj`.

### Docker

Como el proyecto tiene `Lex.sln` y `Lex.Api.csproj` en la misma carpeta, siempre especificar el proyecto:

- `dotnet build Lex.sln` (no `dotnet build` pelado).
- En Dockerfile: `dotnet publish Lex.Api.csproj -c Release ...`.

## Convenciones de código — Frontend

**IMPORTANTE**: Next.js 16 tiene breaking changes respecto a versiones previas. No asumir comportamiento de Next 13/14/15.

- App Router (no Pages Router).
- Componentes de servidor por default; `"use client"` solo cuando es necesario.
- API base: `NEXT_PUBLIC_API_URL` (se hornea en build-time).
- Auth: JWT en cookie `lex_token` (no httpOnly, decisión consciente documentada en `lib/session.ts`).

## Decisiones arquitectónicas importantes

### Modelo de servicios: catálogo cerrado por carrera

- **ProyectoCerrado** y **Salud** usan catálogo cerrado (`CatalogoServicio`).
- **Clase** usa texto libre (sin catálogo).
- Un servicio de PC/Salud solo puede publicarse si el estudiante tiene una carrera verificada que está habilitada para ese servicio, y su año cursado >= año mínimo.
- Ver `README_CATALOGO.md` para detalles.

### Snapshots por valor

Cuando un cliente contrata un servicio, se crea un `Trabajo` que hace **snapshots** de:
- Título, descripción, precio del servicio.
- Para Salud: nombre del catálogo, año mínimo, nombre y matrícula del supervisor.

Motivo: evidencia legal + independencia de cambios futuros en el servicio o supervisor.

### Estados de Trabajo (unificados)

Un solo enum `EstadoTrabajo` para los 3 verticales, con state machine documentada en `README_ESTADOS_TRABAJO.md`.

### Módulo de Solicitudes pausado

`SolicitudController` y `PostulacionController` están pausados desde Sub-hito 1.1 (`[Authorize(Roles = "Admin")]` + `IgnoreApi`). Se decidirá su forma final en un sub-hito posterior.

## Cosas que NO hacer

- **No cambiar el schema de la DB sin generar migration correspondiente.**
- **No cambiar la convención de `Id` como PK.** Si aparece código viejo con `IdXxx`, renombrar al pasar por ahí.
- **No usar `DateTime.Now`.** Siempre `UtcNow`.
- **No reactivar el módulo de Solicitudes sin decisión explícita** sobre cómo va a integrarse con el catálogo cerrado.
- **No cambiar enums viejos** (`EstadoPago`, etc.) a string sin migration explícita.
- **No usar el transaction pooler (puerto 6543) de Supabase**. Session pooler (5432) para todo.

## Comandos frecuentes

### Backend

```bash
cd backend

# Compilar
dotnet build Lex.sln

# Correr localmente (Development)
dotnet run --project Lex.Api.csproj
# → http://localhost:5156

# Migrations
dotnet ef migrations add <Nombre> --project Lex.Api.csproj
dotnet ef database update --project Lex.Api.csproj
dotnet ef migrations list --project Lex.Api.csproj
```

### Frontend

```bash
cd frontend

npm install
npm run dev
# → http://localhost:3000
```

### Git

Trabajo directo en `main` (proyecto en fase de refactor). Commits descriptivos con prefijo `feat:`, `fix:`, `chore:`, `refactor:`, `docs:`.

## Estado del proyecto (a esta fecha)

- **Sub-hito 1.1 completado**: Servicio TPT, catálogo cerrado, Feature Folders reorganizados por vertical.
- **Sub-hito 1.2 completado**: Trabajo TPT con estados unificados, snapshots por valor, Paciente Humano/Animal, consentimiento obligatorio en Salud, state machine documentada.
- **Sub-hito 1.3 completado**: modelo de Pagos con movimientos, comisión LEX calculada, liberación automática en flujos de Trabajo, endpoints públicos de consulta, admin/ingresos con breakdown por vertical.
- **Hito 2 Parte 1 completada**: modelo de disponibilidad, turnos y sesiones; CRUD de disponibilidad; consulta de disponibilidad y sesiones.
- **Hito 2 Parte 2 completada**: contratación de Clase y Salud con reserva atómica de turnos y sesiones. Cancelación individual con ajuste de CantidadSesionesTotales.
- **Hito 2 Parte 3 pendiente**: marcado de sesiones como Realizada y liberación fraccionada del pago.
- **Otros pendientes**: 1.4 (Solicitudes redesign), 1.5 (Frontend).

## Documentos importantes en el repo

- `CLAUDE.md` (este archivo) — contexto general.
- `README_CATALOGO.md` — modelo de catálogo cerrado.
- `README_ESTADOS_TRABAJO.md` — state machine y permisos por transición.
- `README_PAGOS.md` — modelo de pagos con movimientos y ciclo de vida.
- `README_TURNOS.md` — sistema de turnos y sesiones.

## Contacto y equipo

- **Sergio Navarro** — Cofundador, PM + Tech Lead.
- **Ruth, Luana, Victoria** — Cofundadoras (actualmente inactivas en código, retornan post-charla incubadora).

Todos estudiantes de Sistemas de la UNNE.
