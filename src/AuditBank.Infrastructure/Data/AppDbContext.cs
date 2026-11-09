using AuditBank.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditBank.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cuenta> Cuentas => Set<Cuenta>();
    public DbSet<Transaccion> Transacciones => Set<Transaccion>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Rol>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Nombre).IsUnique();
            e.HasData(
                new Rol { Id = 1, Nombre = "Administrador" },
                new Rol { Id = 2, Nombre = "Auditor" },
                new Rol { Id = 3, Nombre = "Consulta" });
        });

        mb.Entity<Usuario>(e =>
        {
            e.Property(x => x.NombreUsuario).HasMaxLength(50).IsRequired();
            e.Property(x => x.NombreCompleto).HasMaxLength(150).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.NombreUsuario).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Rol).WithMany(r => r.Usuarios)
                .HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Cuenta>(e =>
        {
            e.Property(x => x.NumeroCuenta).HasMaxLength(30).IsRequired();
            e.Property(x => x.Titular).HasMaxLength(150).IsRequired();
            e.Property(x => x.Saldo).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.NumeroCuenta).IsUnique();
        });

        mb.Entity<Transaccion>(e =>
        {
            e.Property(x => x.Monto).HasColumnType("decimal(18,2)");
            e.Property(x => x.Descripcion).HasMaxLength(300);
            e.HasIndex(x => x.FechaUtc);

            e.HasOne(x => x.Cuenta).WithMany(c => c.Transacciones)
                .HasForeignKey(x => x.CuentaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CuentaDestino).WithMany()
                .HasForeignKey(x => x.CuentaDestinoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.RegistradaPor).WithMany()
                .HasForeignKey(x => x.RegistradaPorId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<RegistroAuditoria>(e =>
        {
            e.Property(x => x.Entidad).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntidadId).HasMaxLength(50);
            e.Property(x => x.DireccionIp).HasMaxLength(45);
            e.HasIndex(x => new { x.Entidad, x.EntidadId });
            e.HasIndex(x => x.FechaUtc);
            e.HasOne(x => x.Usuario).WithMany()
                .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}