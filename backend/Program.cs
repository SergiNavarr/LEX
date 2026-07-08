using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- EF Core: SQLite (archivo lex.db) ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Servicios de aplicacion ---
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPerfilService, PerfilService>();
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<ITrabajoService, TrabajoService>();
builder.Services.AddScoped<ISolicitudService, SolicitudService>();
builder.Services.AddScoped<IPostulacionService, PostulacionService>();
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IPagoService, PagoService>();
builder.Services.AddScoped<IResenaService, ResenaService>();
builder.Services.AddScoped<IDemoService, DemoService>();

// --- Parametros de negocio (take rate, etc.) ---
builder.Services.Configure<Lex.Api.Common.LexOptions>(
    builder.Configuration.GetSection(Lex.Api.Common.LexOptions.SectionName));

// --- CORS: origins permitidos desde configuracion (coma-separado) ---
const string LexCorsPolicy = "LexCors";
var corsOrigins = builder.Configuration.GetValue<string>("Cors:AllowedOrigins")
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries)
    .Select(o => o.Trim())
    .ToArray()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(LexCorsPolicy, policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// --- Autenticacion JWT Bearer ---
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });
builder.Services.AddAuthorization();

// --- Controllers (enums como string en el JSON) ---
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// --- Swagger / OpenAPI con soporte de JWT (boton Authorize) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "LEX API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegá solo el token JWT (sin el prefijo 'Bearer ').",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

var app = builder.Build();

// --- Base de datos: aplica migraciones (crea lex.db si no existe) y siembra datos base ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var lex = scope.ServiceProvider.GetRequiredService<IOptions<Lex.Api.Common.LexOptions>>().Value;
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, lex.AdminEmail, lex.AdminPassword);
}

// Red de seguridad ante errores: traduce excepciones a 400/403/404/401 con mensaje claro.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(LexCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
