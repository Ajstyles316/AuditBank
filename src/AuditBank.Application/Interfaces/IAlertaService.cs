using AuditBank.Application.DTOs;

namespace AuditBank.Application.Interfaces;

public interface IAlertaService
{
    Task<PagedResult<AlertaResponse>> ListarAsync(FiltroAlertas filtro);
    Task<AlertaResponse?> ObtenerAsync(long id);
}