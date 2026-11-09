using AuditBank.Application.Interfaces;

namespace AuditBank.Api.Services;

public class UsuarioActual(IHttpContextAccessor http) : IUsuarioActual
{
    public int? Id =>
        int.TryParse(http.HttpContext?.User.FindFirst("sub")?.Value, out var id) ? id : null;

    public string? DireccionIp =>
        http.HttpContext?.Connection.RemoteIpAddress?.ToString();
}