using AuditBank.Application.DTOs;

namespace AuditBank.Application.Interfaces;

public interface IAlertaService
{
    Task<PagedResult<AlertaResponse>> ListarAsync(FiltroAlertas filtro);
    Task<AlertaResponse?> ObtenerAsync(long id);

    Task<Resultado> AsignarAsync(long alertaId, int usuarioId);
    Task<Resultado> TomarAsync(long alertaId);
    Task<Resultado> IniciarRevisionAsync(long alertaId);
    Task<Resultado> ComentarAsync(long alertaId, string texto);
    Task<Resultado> CerrarAsync(long alertaId, CerrarAlertaRequest request);
    Task<List<HallazgoResponse>?> ListarHallazgosAsync(long alertaId);
}