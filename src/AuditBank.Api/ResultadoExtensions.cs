using AuditBank.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AuditBank.Api;

public static class ResultadoExtensions
{
    public static IActionResult ToActionResult(this Resultado r, ControllerBase c) => r.Codigo switch
    {
        CodigoResultado.Ok => c.NoContent(),
        CodigoResultado.NoEncontrado => c.NotFound(new { mensaje = r.Mensaje }),
        CodigoResultado.Prohibido => c.StatusCode(StatusCodes.Status403Forbidden, new { mensaje = r.Mensaje }),
        _ => c.BadRequest(new { mensaje = r.Mensaje })
    };
}