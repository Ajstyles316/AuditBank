using AuditBank.Application.DTOs;

namespace AuditBank.Application.Interfaces;

public interface ITransaccionService
{
    Task<(TransaccionResponse? Resultado, string? Error)> RegistrarAsync(CrearTransaccionRequest request, int usuarioId);
    Task<PagedResult<TransaccionResponse>> ListarAsync(FiltroTransacciones filtro);
    Task<TransaccionResponse?> ObtenerAsync(long id);
}