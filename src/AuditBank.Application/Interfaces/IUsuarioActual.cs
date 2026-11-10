namespace AuditBank.Application.Interfaces;

public interface IUsuarioActual
{
    int? Id { get; }
    string? Rol { get; }
    string? DireccionIp { get; }
}