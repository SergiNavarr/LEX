using Lex.Api.Common;
using Lex.Api.Data;
using Lex.Api.DTOs;
using Lex.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lex.Api.Services;

public class PacienteService : IPacienteService
{
    private readonly AppDbContext _db;

    public PacienteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PacienteResponse> CrearAsync(int clienteId, CrearPacienteRequest request)
    {
        // El paciente cuelga del perfil de cliente (clientes.cliente_id == usuario_id).
        if (!await _db.PerfilesCliente.AnyAsync(p => p.UsuarioId == clienteId))
            throw new ForbiddenException("Tu usuario no tiene un perfil de cliente.");

        var paciente = new Paciente
        {
            ClienteId = clienteId,
            NombreCompleto = request.NombreCompleto.Trim(),
            Edad = request.Edad,
            Notas = string.IsNullOrWhiteSpace(request.Notas) ? null : request.Notas.Trim()
        };

        _db.Pacientes.Add(paciente);
        await _db.SaveChangesAsync();

        return await ObtenerAsync(clienteId, paciente.PacienteId);
    }

    public async Task<IReadOnlyList<PacienteResponse>> ListarMiosAsync(int clienteId)
    {
        return await _db.Pacientes.AsNoTracking()
            .Where(p => p.ClienteId == clienteId)
            .OrderBy(p => p.NombreCompleto)
            .Select(Proyeccion)
            .ToListAsync();
    }

    public async Task<PacienteResponse> ObtenerAsync(int clienteId, int idPaciente)
    {
        var paciente = await _db.Pacientes.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PacienteId == idPaciente)
            ?? throw new NotFoundException($"No existe el paciente {idPaciente}.");

        if (paciente.ClienteId != clienteId)
            throw new ForbiddenException("Este paciente no te pertenece.");

        return ToResponse(paciente);
    }

    private static PacienteResponse ToResponse(Paciente p) => new()
    {
        PacienteId = p.PacienteId,
        ClienteId = p.ClienteId,
        NombreCompleto = p.NombreCompleto,
        Edad = p.Edad,
        Notas = p.Notas
    };

    private static readonly System.Linq.Expressions.Expression<Func<Paciente, PacienteResponse>> Proyeccion =
        p => new PacienteResponse
        {
            PacienteId = p.PacienteId,
            ClienteId = p.ClienteId,
            NombreCompleto = p.NombreCompleto,
            Edad = p.Edad,
            Notas = p.Notas
        };
}
