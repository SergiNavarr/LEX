using Lex.Api.Domain.Entities;
using Lex.Api.Domain.Enums;

namespace Lex.Api.Features.Pagos;

public interface IPagoService
{
    // --- Consultas ---
    Task<IReadOnlyList<PagoResumenResponse>> ListarMiosAsync(int usuarioId, EstadoPago? estado, TipoServicio? tipoTrabajo);
    Task<PagoDetalleResponse> ObtenerDetalleAsync(int usuarioId, int idPago);
    Task<IReadOnlyList<MovimientoPagoResponse>> ListarMovimientosAsync(int usuarioId, int idPago);
    Task<IngresosAdminResponse> ObtenerIngresosLexAsync();

    // --- Negocio del escrow ---
    // Ninguno de estos llama a SaveChanges: solo dejan los cambios en el DbContext para
    // que el llamador cierre la unidad de trabajo. Asi el trabajo y su pago se commitean
    // en la misma transaccion implicita de SaveChanges, sin transaccion explicita.
    Pago CrearPagoParaTrabajo(Trabajo trabajo);

    /// <summary>Libera el saldo pendiente y cierra el escrow. Usado por trabajos sin sesiones (ProyectoCerrado) y por el cierre de disputas.</summary>
    Task LiberarPagoTotalAsync(int idTrabajo);

    /// <summary>
    /// Libera la fracción que le toca a una sesión: MontoAEstudiante / cantidadSesionesTotales.
    /// La última sesión paga el remanente, así la suma cierra exacta pese al redondeo.
    /// </summary>
    Task LiberarFraccionPagoPorSesionAsync(int idTrabajo, int cantidadSesionesTotales, bool esUltimaSesion);

    Task ReembolsarPagoAsync(int idTrabajo, string motivo);
    Task MarcarEnDisputaAsync(int idTrabajo);
}
