using AuditBank.Application.DTOs;
using AuditBank.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Auditor")]
public class ReportesController(IReporteService reportes) : ControllerBase
{
    [HttpGet("transacciones")]
    public async Task<IActionResult> Transacciones(
        [FromQuery] FiltroTransacciones filtro,
        [FromQuery] FormatoReporte formato = FormatoReporte.Excel)
    {
        var a = await reportes.TransaccionesAsync(filtro, formato);
        return File(a.Contenido, a.TipoContenido, a.NombreArchivo);
    }

    [HttpGet("alertas")]
    public async Task<IActionResult> Alertas(
        [FromQuery] FiltroAlertas filtro,
        [FromQuery] FormatoReporte formato = FormatoReporte.Excel)
    {
        var a = await reportes.AlertasAsync(filtro, formato);
        return File(a.Contenido, a.TipoContenido, a.NombreArchivo);
    }

    [HttpGet("auditoria")]
    public async Task<IActionResult> Auditoria(
        [FromQuery] FiltroAuditoria filtro,
        [FromQuery] FormatoReporte formato = FormatoReporte.Excel)
    {
        var a = await reportes.AuditoriaAsync(filtro, formato);
        return File(a.Contenido, a.TipoContenido, a.NombreArchivo);
    }
}