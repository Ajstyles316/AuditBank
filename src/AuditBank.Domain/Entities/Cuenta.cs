using AuditBank.Domain.Enums;
namespace AuditBank.Domain.Entities;

public class Cuenta
{
    public int Id { get; set; }
    public string NumeroCuenta { get; set; } = string.Empty;
    public string Titular { get; set; } = string.Empty;
    public TipoCuenta Tipo { get; set; }
    public Moneda Moneda { get; set; }
    public decimal Saldo { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;

    public ICollection<Transaccion> Transacciones { get; set; } = new List<Transaccion>();
}