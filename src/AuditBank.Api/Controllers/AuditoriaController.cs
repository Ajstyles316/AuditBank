using AuditBank.Application.DTOs;
using AuditBank.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Auditor")]
public class AuditoriaController(IAuditoriaService auditoria) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<RegistroAuditoriaResponse>>> Listar(
        [FromQuery] FiltroAuditoria filtro) =>
        Ok(await auditoria.ListarAsync(filtro));
}