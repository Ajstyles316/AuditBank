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

    [HttpGet("{id:long}/hallazgos")]
    public async Task<ActionResult<List<HallazgoResponse>>> Hallazgos(long id)
    {
        var lista = await alertas.ListarHallazgosAsync(id);
        return lista is null ? NotFound() : Ok(lista);
    }

    [HttpPost("{id:long}/asignar")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Asignar(long id, AsignarAlertaRequest request) =>
        (await alertas.AsignarAsync(id, request.UsuarioId)).ToActionResult(this);

    [HttpPost("{id:long}/tomar")]
    [Authorize(Roles = "Auditor")]
    public async Task<IActionResult> Tomar(long id) =>
        (await alertas.TomarAsync(id)).ToActionResult(this);

    [HttpPost("{id:long}/iniciar-revision")]
    public async Task<IActionResult> IniciarRevision(long id) =>
        (await alertas.IniciarRevisionAsync(id)).ToActionResult(this);

    [HttpPost("{id:long}/comentarios")]
    public async Task<IActionResult> Comentar(long id, ComentarioRequest request) =>
        (await alertas.ComentarAsync(id, request.Texto)).ToActionResult(this);

    [HttpPost("{id:long}/cerrar")]
    public async Task<IActionResult> Cerrar(long id, CerrarAlertaRequest request) =>
        (await alertas.CerrarAsync(id, request)).ToActionResult(this);
}