// Transaccion.cs
using AuditBank.Domain.Enums;
namespace AuditBank.Domain.Entities;

public class Transaccion
{
    public long Id { get; set; }
    public TipoTransaccion Tipo { get; set; }
    public decimal Monto { get; set; }
    public string? Descripcion { get; set; }
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;

    public int CuentaId { get; set; }
    public Cuenta Cuenta { get; set; } = null!;

    public int? CuentaDestinoId { get; set; }
    public Cuenta? CuentaDestino { get; set; }

    public int RegistradaPorId { get; set; }
    public Usuario RegistradaPor { get; set; } = null!;
}