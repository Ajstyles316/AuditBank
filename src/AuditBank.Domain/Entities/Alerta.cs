using AuditBank.Domain.Enums;

namespace AuditBank.Domain.Entities;

public class Alerta
{
    public long Id { get; set; }
    public SeveridadAlerta Severidad { get; set; }
    public EstadoAlerta Estado { get; set; } = EstadoAlerta.Abierta;
    public string Detalle { get; set; } = string.Empty;
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
    public DateTime? CerradaEn { get; set; }

    public int ReglaId { get; set; }
    public ReglaAuditoria Regla { get; set; } = null!;

    public long TransaccionId { get; set; }
    public Transaccion Transaccion { get; set; } = null!;

    public int? AsignadaAId { get; set; }
    public Usuario? AsignadaA { get; set; }
}