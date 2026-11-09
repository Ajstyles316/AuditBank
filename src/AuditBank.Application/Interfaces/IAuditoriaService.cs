using AuditBank.Application.DTOs;

namespace AuditBank.Application.Interfaces;

public interface IAuditoriaService
{
    Task<PagedResult<RegistroAuditoriaResponse>> ListarAsync(FiltroAuditoria filtro);
}