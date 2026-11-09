using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuditBank.Application.DTOs;
using AuditBank.Application.Interfaces;
using AuditBank.Domain.Entities;
using AuditBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using AuditBank.Domain.Enums;
namespace AuditBank.Infrastructure.Services;
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiraMinutos { get; set; } = 60;
}
public class AuthService(AppDbContext db, IOptions<JwtSettings> jwtOptions, IUsuarioActual actual) : IAuthService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
{
    var usuario = await db.Usuarios
        .Include(u => u.Rol)
        .FirstOrDefaultAsync(u => u.NombreUsuario == request.NombreUsuario && u.Activo);

    if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
    {
        await RegistrarAccesoAsync(AccionAuditoria.LoginFallido, usuario?.Id, request.NombreUsuario);
        return null;
    }

    await RegistrarAccesoAsync(AccionAuditoria.Login, usuario.Id, usuario.NombreUsuario);
    return GenerarToken(usuario);
}

private async Task RegistrarAccesoAsync(AccionAuditoria accion, int? usuarioId, string nombreIntentado)
{
    var nombre = nombreIntentado.Length > 100 ? nombreIntentado[..100] : nombreIntentado;

    db.RegistrosAuditoria.Add(new RegistroAuditoria
    {
        Accion = accion,
        Entidad = nameof(Usuario),
        EntidadId = usuarioId?.ToString(),
        ValorNuevo = JsonSerializer.Serialize(new { usuario = nombre }),
        UsuarioId = usuarioId,
        DireccionIp = actual.DireccionIp
    });
    await db.SaveChangesAsync();
}

    public async Task<(bool Ok, string? Error)> RegistrarAsync(RegistroRequest r)
    {
        if (r.Password.Length < 8)
            return (false, "La contraseña debe tener al menos 8 caracteres.");
        if (await db.Usuarios.AnyAsync(u => u.NombreUsuario == r.NombreUsuario))
            return (false, "El nombre de usuario ya existe.");
        if (await db.Usuarios.AnyAsync(u => u.Email == r.Email))
            return (false, "El email ya está registrado.");
        if (!await db.Roles.AnyAsync(x => x.Id == r.RolId))
            return (false, "El rol no existe.");

        db.Usuarios.Add(new Usuario
        {
            NombreUsuario = r.NombreUsuario,
            NombreCompleto = r.NombreCompleto,
            Email = r.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(r.Password),
            RolId = r.RolId
        });
        await db.SaveChangesAsync();
        return (true, null);
    }

    private AuthResponse GenerarToken(Usuario u)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new Claim("name", u.NombreUsuario),
            new Claim("role", u.Rol.Nombre)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expira = DateTime.UtcNow.AddMinutes(_jwt.ExpiraMinutos);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expira,
            signingCredentials: credenciales);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expira, u.NombreUsuario, u.Rol.Nombre);
    }
}