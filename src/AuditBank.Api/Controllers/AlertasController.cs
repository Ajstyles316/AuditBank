using AuditBank.Application.DTOs;
using AuditBank.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Auditor")]
public class AlertasController(IAlertaService alertas) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AlertaResponse>>> Listar([FromQuery] FiltroAlertas filtro) =>
        Ok(await alertas.ListarAsync(filtro));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AlertaResponse>> Obtener(long id)
    {
        var alerta = await alertas.ObtenerAsync(id);
        return alerta is null ? NotFound() : Ok(alerta);
    }
}