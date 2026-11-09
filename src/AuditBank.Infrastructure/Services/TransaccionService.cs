using System.Linq.Expressions;
using AuditBank.Application.DTOs;
using AuditBank.Application.Interfaces;
using AuditBank.Domain.Entities;
using AuditBank.Domain.Enums;
using AuditBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace AuditBank.Infrastructure.Services;

public class TransaccionService(AppDbContext db, IMotorReglas motor, ILogger<TransaccionService> log) : ITransaccionService
{
    private static readonly Expression<Func<Transaccion, TransaccionResponse>> Proyeccion = t =>
        new TransaccionResponse(
            t.Id, t.Tipo, t.Monto, t.Descripcion, t.FechaUtc,
            t.CuentaId, t.Cuenta.NumeroCuenta,
            t.CuentaDestinoId, t.CuentaDestino != null ? t.CuentaDestino.NumeroCuenta : null,
            t.RegistradaPorId, t.RegistradaPor.NombreUsuario);

    public async Task<(TransaccionResponse? Resultado, string? Error)> RegistrarAsync(
        CrearTransaccionRequest r, int usuarioId)
    {
        if (r.Monto <= 0)
            return (null, "El monto debe ser mayor a cero.");
        var monto = decimal.Round(r.Monto, 2);

        var origen = await db.Cuentas.FirstOrDefaultAsync(c => c.Id == r.CuentaId);
        if (origen is null) return (null, "La cuenta de origen no existe.");
        if (!origen.Activa) return (null, "La cuenta de origen está inactiva.");

        Cuenta? destino = null;

        switch (r.Tipo)
        {
            case TipoTransaccion.Deposito:
                if (r.CuentaDestinoId is not null)
                    return (null, "Un depósito no lleva cuenta destino.");
                origen.Saldo += monto;
                break;

            case TipoTransaccion.Retiro:
                if (r.CuentaDestinoId is not null)
                    return (null, "Un retiro no lleva cuenta destino.");
                if (origen.Saldo < monto)
                    return (null, "Saldo insuficiente.");
                origen.Saldo -= monto;
                break;

            case TipoTransaccion.Transferencia:
                if (r.CuentaDestinoId is null)
                    return (null, "La transferencia requiere una cuenta destino.");
                if (r.CuentaDestinoId == r.CuentaId)
                    return (null, "La cuenta destino debe ser distinta de la de origen.");
                destino = await db.Cuentas.FirstOrDefaultAsync(c => c.Id == r.CuentaDestinoId);
                if (destino is null) return (null, "La cuenta destino no existe.");
                if (!destino.Activa) return (null, "La cuenta destino está inactiva.");
                if (destino.Moneda != origen.Moneda)
                    return (null, "Las cuentas deben tener la misma moneda.");
                if (origen.Saldo < monto)
                    return (null, "Saldo insuficiente.");
                origen.Saldo -= monto;
                destino.Saldo += monto;
                break;

            default:
                return (null, "Tipo de transacción inválido.");
        }

        var transaccion = new Transaccion
        {
            Tipo = r.Tipo,
            Monto = monto,
            Descripcion = r.Descripcion?.Trim(),
            CuentaId = origen.Id,
            CuentaDestinoId = destino?.Id,
            RegistradaPorId = usuarioId
        };

        db.Transacciones.Add(transaccion);
        await db.SaveChangesAsync(); // una sola llamada: todo o nada
        try
        {
            await motor.EvaluarAsync(transaccion.Id);
        }
        catch (Exception ex)
        {
            // El fallo del monitoreo no debe impedir que la transacción quede registrada.
            log.LogError(ex, "Falló el motor de reglas para la transacción {Id}", transaccion.Id);
        }
        return (await ObtenerAsync(transaccion.Id), null);
    }

    public async Task<PagedResult<TransaccionResponse>> ListarAsync(FiltroTransacciones f)
    {
        var pagina = Math.Max(f.Pagina, 1);
        var tamano = Math.Clamp(f.TamanoPagina, 1, 100);

        var q = db.Transacciones.AsNoTracking().AsQueryable();

        if (f.CuentaId is not null)
            q = q.Where(t => t.CuentaId == f.CuentaId || t.CuentaDestinoId == f.CuentaId);
        if (f.Tipo is not null) q = q.Where(t => t.Tipo == f.Tipo);
        if (f.Desde is not null) q = q.Where(t => t.FechaUtc >= f.Desde);
        if (f.Hasta is not null) q = q.Where(t => t.FechaUtc <= f.Hasta);
        if (f.MontoMin is not null) q = q.Where(t => t.Monto >= f.MontoMin);
        if (f.MontoMax is not null) q = q.Where(t => t.Monto <= f.MontoMax);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(t => t.FechaUtc).ThenByDescending(t => t.Id)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .Select(Proyeccion)
            .ToListAsync();

        return new PagedResult<TransaccionResponse>(items, total, pagina, tamano);
    }

    public async Task<TransaccionResponse?> ObtenerAsync(long id) =>
        await db.Transacciones.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync();
}