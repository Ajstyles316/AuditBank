using AuditBank.Application.DTOs;
using AuditBank.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CuentasController(ICuentaService cuentas) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CuentaResponse>>> Listar() =>
        Ok(await cuentas.ListarAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CuentaResponse>> Obtener(int id)
    {
        var cuenta = await cuentas.ObtenerAsync(id);
        return cuenta is null ? NotFound() : Ok(cuenta);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<CuentaResponse>> Crear(CrearCuentaRequest request)
    {
        var (cuenta, error) = await cuentas.CrearAsync(request);
        return cuenta is null
            ? BadRequest(new { mensaje = error })
            : CreatedAtAction(nameof(Obtener), new { id = cuenta.Id }, cuenta);
    }
}