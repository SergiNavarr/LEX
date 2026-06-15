using Lex.Api.Enums;

namespace Lex.Api.DTOs;

public class PagoResponse
{
    public int IdPago { get; set; }
    public int IdTrabajo { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal PorcentajeComision { get; set; } // 10.00 = 10%
    public decimal ComisionLex { get; set; }
    public decimal MontoEstudiante { get; set; }
    public EstadoPago Estado { get; set; }
    public DateTime? FechaRetencion { get; set; }
    public DateTime? FechaLiberacion { get; set; }
}

// GET /api/pagos/mios: pagos del estudiante con sus totales.
public class MisPagosResponse
{
    // Lo efectivamente cobrado (pagos liberados).
    public decimal TotalCobrado { get; set; }
    // Lo retenido en escrow, pendiente de liberar.
    public decimal TotalRetenido { get; set; }
    public IReadOnlyList<PagoResponse> Pagos { get; set; } = new List<PagoResponse>();
}

// GET /api/admin/ingresos: panel del modelo de ingresos de LEX.
public class IngresosLexResponse
{
    // Comision ya liberada = ingreso efectivo de LEX.
    public decimal ComisionLiberada { get; set; }
    // Comision retenida en escrow = ingreso potencial.
    public decimal ComisionRetenida { get; set; }
    // Suma de ambas (no incluye reembolsadas).
    public decimal ComisionTotal { get; set; }

    public int CantidadTrabajosConPago { get; set; }
    public int CantidadPagosLiberados { get; set; }
    public int CantidadPagosRetenidos { get; set; }
    public int CantidadPagosReembolsados { get; set; }
}
