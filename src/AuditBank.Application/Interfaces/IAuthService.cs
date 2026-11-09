using AuditBank.Application.DTOs;

namespace AuditBank.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<(bool Ok, string? Error)> RegistrarAsync(RegistroRequest request);
}