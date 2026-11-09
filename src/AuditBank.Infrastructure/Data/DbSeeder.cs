using AuditBank.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditBank.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SembrarAdminAsync(AppDbContext db, string? password)
    {
        if (await db.Usuarios.AnyAsync()) return;
        if (string.IsNullOrWhiteSpace(password)) return;

        db.Usuarios.Add(new Usuario
        {
            NombreUsuario = "admin",
            NombreCompleto = "Administrador del Sistema",
            Email = "admin@auditbank.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RolId = 1 // Administrador
        });
        await db.SaveChangesAsync();
    }
}