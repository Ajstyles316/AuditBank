using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AuditBank.Application.Interfaces;
using AuditBank.Domain.Entities;
using AuditBank.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuditBank.Infrastructure.Data;

public class AuditoriaInterceptor(IUsuarioActual usuarioActual) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        Converters = { new JsonStringEnumConverter() },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly HashSet<string> Sensibles = ["PasswordHash"];

    private sealed class Pendiente
    {
        public EntityEntry Entry { get; set; } = null!;
        public AccionAuditoria Accion { get; set; }
        public string Entidad { get; set; } = string.Empty;
        public string? EntidadId { get; set; }
        public string? ValorAnterior { get; set; }
        public string? ValorNuevo { get; set; }
    }

    private List<Pendiente> _pendientes = [];
    private IDbContextTransaction? _tx;
    private bool _registrando;

    // ---------- ANTES de guardar ----------
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData e, InterceptionResult<int> result)
    {
        if (e.Context is not null && !_registrando)
        {
            Capturar(e.Context);
            if (_pendientes.Count > 0 && e.Context.Database.CurrentTransaction is null)
                _tx = e.Context.Database.BeginTransaction();
        }
        return base.SavingChanges(e, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData e, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (e.Context is not null && !_registrando)
        {
            Capturar(e.Context);
            if (_pendientes.Count > 0 && e.Context.Database.CurrentTransaction is null)
                _tx = await e.Context.Database.BeginTransactionAsync(ct);
        }
        return await base.SavingChangesAsync(e, result, ct);
    }

    // ---------- DESPUÉS de guardar ----------
    public override int SavedChanges(SaveChangesCompletedEventData e, int result)
    {
        if (e.Context is not null && !_registrando && _pendientes.Count > 0)
        {
            _registrando = true;
            try
            {
                AgregarRegistros(e.Context);
                e.Context.SaveChanges();
                _tx?.Commit();
            }
            finally { _registrando = false; Limpiar(); }
        }
        return base.SavedChanges(e, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData e, int result, CancellationToken ct = default)
    {
        if (e.Context is not null && !_registrando && _pendientes.Count > 0)
        {
            _registrando = true;
            try
            {
                AgregarRegistros(e.Context);
                await e.Context.SaveChangesAsync(ct);
                if (_tx is not null) await _tx.CommitAsync(ct);
            }
            finally { _registrando = false; Limpiar(); }
        }
        return await base.SavedChangesAsync(e, result, ct);
    }

    // ---------- Si algo falla: se deshace todo ----------
    public override void SaveChangesFailed(DbContextErrorEventData e)
    {
        Limpiar();
        base.SaveChangesFailed(e);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData e, CancellationToken ct = default)
    {
        Limpiar();
        return base.SaveChangesFailedAsync(e, ct);
    }

    // ---------- Lógica interna ----------
    private void Capturar(DbContext ctx)
    {
        ctx.ChangeTracker.DetectChanges();
        _pendientes = [];

        foreach (var entry in ctx.ChangeTracker.Entries())
        {
            if (entry.Entity is RegistroAuditoria)
            {
                if (entry.State is EntityState.Modified or EntityState.Deleted)
                    throw new InvalidOperationException(
                        "La bitácora de auditoría es de solo lectura: no se puede modificar ni eliminar.");
                continue;
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var anterior = new Dictionary<string, object?>();
            var nuevo = new Dictionary<string, object?>();

            foreach (var prop in entry.Properties)
            {
                if (prop.IsTemporary) continue;
                var nombre = prop.Metadata.Name;

                switch (entry.State)
                {
                    case EntityState.Added:
                        nuevo[nombre] = Proteger(nombre, prop.CurrentValue);
                        break;
                    case EntityState.Deleted:
                        anterior[nombre] = Proteger(nombre, prop.OriginalValue);
                        break;
                    case EntityState.Modified when prop.IsModified:
                        anterior[nombre] = Proteger(nombre, prop.OriginalValue);
                        nuevo[nombre] = Proteger(nombre, prop.CurrentValue);
                        break;
                }
            }

            _pendientes.Add(new Pendiente
            {
                Entry = entry,
                Entidad = entry.Metadata.ClrType.Name,
                Accion = entry.State switch
                {
                    EntityState.Added => AccionAuditoria.Crear,
                    EntityState.Modified => AccionAuditoria.Modificar,
                    _ => AccionAuditoria.Eliminar
                },
                EntidadId = entry.State == EntityState.Added ? null : ObtenerId(entry),
                ValorAnterior = anterior.Count > 0 ? JsonSerializer.Serialize(anterior, JsonOpts) : null,
                ValorNuevo = nuevo.Count > 0 ? JsonSerializer.Serialize(nuevo, JsonOpts) : null
            });
        }
    }

    private void AgregarRegistros(DbContext ctx)
    {
        foreach (var p in _pendientes)
        {
            ctx.Set<RegistroAuditoria>().Add(new RegistroAuditoria
            {
                Accion = p.Accion,
                Entidad = p.Entidad,
                EntidadId = p.EntidadId ?? ObtenerId(p.Entry), // los Id nuevos ya existen
                ValorAnterior = p.ValorAnterior,
                ValorNuevo = p.ValorNuevo,
                UsuarioId = usuarioActual.Id,
                DireccionIp = usuarioActual.DireccionIp
            });
        }
    }

    private static object? Proteger(string nombre, object? valor) =>
        Sensibles.Contains(nombre) ? "[PROTEGIDO]" : valor;

    private static string? ObtenerId(EntityEntry entry)
    {
        var clave = entry.Metadata.FindPrimaryKey();
        if (clave is null) return null;
        return string.Join(",", clave.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString()));
    }

    private void Limpiar()
    {
        _pendientes = [];
        _tx?.Dispose(); // si no se confirmó, esto deshace la transacción
        _tx = null;
    }
}