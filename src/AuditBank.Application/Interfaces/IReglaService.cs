using AuditBank.Application.DTOs;

namespace AuditBank.Application.Interfaces;

public interface IReglaService
{
    Task<List<ReglaResponse>> ListarAsync();
    Task<Resultado> ActualizarAsync(int id, ActualizarReglaRequest request);
}