using AuditBank.Application.DTOs;
using AuditBank.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await auth.LoginAsync(request);
        return result is null
            ? Unauthorized(new { mensaje = "Credenciales inválidas." })
            : Ok(result);
    }

    [HttpPost("registro")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Registro(RegistroRequest request)
    {
        var (ok, error) = await auth.RegistrarAsync(request);
        return ok ? StatusCode(201) : BadRequest(new { mensaje = error });
    }

    [HttpGet("yo")]
    [Authorize]
    public IActionResult Yo() =>
        Ok(new { usuario = User.Identity?.Name, rol = User.FindFirst("role")?.Value });
}