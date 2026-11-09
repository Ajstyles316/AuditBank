using AuditBank.Domain.Enums;

namespace AuditBank.Application.DTOs;

public record CrearTransaccionRequest(
    int CuentaId,
    int? CuentaDestinoId,
    TipoTransaccion Tipo,
    decimal Monto,
    string? Descripcion);

public record TransaccionResponse(
    long Id,
    TipoTransaccion Tipo,
    decimal Monto,
    string? Descripcion,
    DateTime FechaUtc,
    int CuentaId,
    string NumeroCuenta,
    int? CuentaDestinoId,
    string? NumeroCuentaDestino,
    int RegistradaPorId,
    string RegistradaPor);

public class FiltroTransacciones
{
    public int? CuentaId { get; set; }
    public TipoTransaccion? Tipo { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public decimal? MontoMin { get; set; }
    public decimal? MontoMax { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Pagina, int TamanoPagina);