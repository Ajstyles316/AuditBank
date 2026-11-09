using AuditBank.Domain.Enums;

namespace AuditBank.Application.DTOs;

public record AlertaResponse(
    long Id,
    string ReglaCodigo,
    string ReglaNombre,
    SeveridadAlerta Severidad,
    EstadoAlerta Estado,
    string Detalle,
    DateTime CreadaEn,
    long TransaccionId,
    int CuentaId,
    string NumeroCuenta,
    decimal Monto,
    TipoTransaccion TipoTransaccion,
    int? AsignadaAId,
    string? AsignadaA,
    DateTime? CerradaEn);

public class FiltroAlertas
{
    public EstadoAlerta? Estado { get; set; }
    public SeveridadAlerta? Severidad { get; set; }
    public int? ReglaId { get; set; }
    public int? CuentaId { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
}