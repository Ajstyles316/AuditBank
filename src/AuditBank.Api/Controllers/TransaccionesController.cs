using AuditBank.Application.DTOs;
using AuditBank.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransaccionesController(ITransaccionService transacciones) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<TransaccionResponse>>> Listar([FromQuery] FiltroTransacciones filtro) =>
        Ok(await transacciones.ListarAsync(filtro));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<TransaccionResponse>> Obtener(long id)
    {
        var t = await transacciones.ObtenerAsync(id);
        return t is null ? NotFound() : Ok(t);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<TransaccionResponse>> Registrar(CrearTransaccionRequest request)
    {
        var usuarioId = int.Parse(User.FindFirst("sub")!.Value);
        var (t, error) = await transacciones.RegistrarAsync(request, usuarioId);
        return t is null
            ? BadRequest(new { mensaje = error })
            : CreatedAtAction(nameof(Obtener), new { id = t.Id }, t);
    }
}