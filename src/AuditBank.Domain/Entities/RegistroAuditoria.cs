using AuditBank.Domain.Enums;
namespace AuditBank.Domain.Entities;

public class RegistroAuditoria
{
    public long Id { get; set; }
    public AccionAuditoria Accion { get; set; }
    public string Entidad { get; set; } = string.Empty;
    public string? EntidadId { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public string? DireccionIp { get; set; }

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}