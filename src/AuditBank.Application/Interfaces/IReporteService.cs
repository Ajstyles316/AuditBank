using AuditBank.Application.DTOs;

namespace AuditBank.Application.Interfaces;

public interface IReporteService
{
    Task<ArchivoReporte> TransaccionesAsync(FiltroTransacciones filtro, FormatoReporte formato);
    Task<ArchivoReporte> AlertasAsync(FiltroAlertas filtro, FormatoReporte formato);
    Task<ArchivoReporte> AuditoriaAsync(FiltroAuditoria filtro, FormatoReporte formato);
}