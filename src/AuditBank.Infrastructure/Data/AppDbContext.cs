using AuditBank.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using AuditBank.Domain.Enums;
namespace AuditBank.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ReglaAuditoria> Reglas => Set<ReglaAuditoria>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
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
        mb.Entity<ReglaAuditoria>(e =>
{
    e.Property(x => x.Codigo).HasMaxLength(50).IsRequired();
    e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
    e.Property(x => x.Descripcion).HasMaxLength(500).IsRequired();
    e.HasIndex(x => x.Codigo).IsUnique();

    e.HasData(
        new ReglaAuditoria
        {
            Id = 1, Codigo = "MONTO_ALTO_BOB", Nombre = "Monto alto en bolivianos",
            Descripcion = "Transacción en BOB que supera el umbral individual.",
            Tipo = TipoRegla.MontoAlto, Severidad = SeveridadAlerta.Alta,
            Parametros = """{"umbral":70000,"moneda":"BOB"}"""
        },
        new ReglaAuditoria
        {
            Id = 2, Codigo = "MONTO_ALTO_USD", Nombre = "Monto alto en dólares",
            Descripcion = "Transacción en USD que supera el umbral individual.",
            Tipo = TipoRegla.MontoAlto, Severidad = SeveridadAlerta.Alta,
            Parametros = """{"umbral":10000,"moneda":"USD"}"""
        },
        new ReglaAuditoria
        {
            Id = 3, Codigo = "FRACCIONAMIENTO_BOB", Nombre = "Posible fraccionamiento en bolivianos",
            Descripcion = "Varias operaciones del mismo tipo en una ventana de tiempo cuya suma supera el umbral.",
            Tipo = TipoRegla.Fraccionamiento, Severidad = SeveridadAlerta.Media,
            Parametros = """{"umbral":70000,"moneda":"BOB","ventanaHoras":24,"cantidadMinima":3}"""
        },
        new ReglaAuditoria
        {
            Id = 4, Codigo = "FRACCIONAMIENTO_USD", Nombre = "Posible fraccionamiento en dólares",
            Descripcion = "Varias operaciones del mismo tipo en una ventana de tiempo cuya suma supera el umbral.",
            Tipo = TipoRegla.Fraccionamiento, Severidad = SeveridadAlerta.Media,
            Parametros = """{"umbral":10000,"moneda":"USD","ventanaHoras":24,"cantidadMinima":3}"""
        },
        new ReglaAuditoria
        {
            Id = 5, Codigo = "FUERA_DE_HORARIO", Nombre = "Operación fuera de horario",
            Descripcion = "Operación registrada fuera del horario laboral o en fin de semana.",
            Tipo = TipoRegla.FueraDeHorario, Severidad = SeveridadAlerta.Baja,
            Parametros = """{"horaInicio":8,"horaFin":18,"offsetUtcHoras":-4,"incluirFinesDeSemana":true}"""
        });
});

mb.Entity<Alerta>(e =>
{
    e.Property(x => x.Detalle).HasMaxLength(1000).IsRequired();
    e.HasIndex(x => x.Estado);
    e.HasIndex(x => x.CreadaEn);
    e.HasIndex(x => new { x.ReglaId, x.TransaccionId }).IsUnique();

    e.HasOne(x => x.Regla).WithMany()
        .HasForeignKey(x => x.ReglaId).OnDelete(DeleteBehavior.Restrict);
    e.HasOne(x => x.Transaccion).WithMany()
        .HasForeignKey(x => x.TransaccionId).OnDelete(DeleteBehavior.Restrict);
    e.HasOne(x => x.AsignadaA).WithMany()
        .HasForeignKey(x => x.AsignadaAId).OnDelete(DeleteBehavior.Restrict);
});
    }
}