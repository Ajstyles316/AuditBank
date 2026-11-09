namespace AuditBank.Application.Interfaces;

public interface IUsuarioActual
{
    int? Id { get; }
    string? DireccionIp { get; }
}