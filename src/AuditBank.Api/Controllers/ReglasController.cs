using AuditBank.Application.DTOs;
using AuditBank.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Auditor")]
public class ReglasController(IReglaService reglas) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ReglaResponse>>> Listar() =>
        Ok(await reglas.ListarAsync());

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Actualizar(int id, ActualizarReglaRequest request) =>
        (await reglas.ActualizarAsync(id, request)).ToActionResult(this);
}