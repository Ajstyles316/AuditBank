namespace AuditBank.Application.DTOs;

public record LoginRequest(string NombreUsuario, string Password);

public record RegistroRequest(
    string NombreUsuario,
    string NombreCompleto,
    string Email,
    string Password,
    int RolId);

public record AuthResponse(string Token, DateTime ExpiraEnUtc, string NombreUsuario, string Rol);