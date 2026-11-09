using AuditBank.Application.DTOs;

namespace AuditBank.Application.Interfaces;

public interface ICuentaService
{
    Task<(CuentaResponse? Resultado, string? Error)> CrearAsync(CrearCuentaRequest request);
    Task<List<CuentaResponse>> ListarAsync();
    Task<CuentaResponse?> ObtenerAsync(int id);
}