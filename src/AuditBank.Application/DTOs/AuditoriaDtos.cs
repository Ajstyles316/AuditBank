using AuditBank.Domain.Enums;

namespace AuditBank.Application.DTOs;

public record RegistroAuditoriaResponse(
    long Id,
    AccionAuditoria Accion,
    string Entidad,
    string? EntidadId,
    string? ValorAnterior,
    string? ValorNuevo,
    DateTime FechaUtc,
    string? DireccionIp,
    int? UsuarioId,
    string? Usuario);

public class FiltroAuditoria
{
    public string? Entidad { get; set; }
    public string? EntidadId { get; set; }
    public int? UsuarioId { get; set; }
    public AccionAuditoria? Accion { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
}