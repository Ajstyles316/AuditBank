using System.Linq.Expressions;
using AuditBank.Application.DTOs;
using AuditBank.Application.Interfaces;
using AuditBank.Domain.Entities;
using AuditBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuditBank.Infrastructure.Services;

public class AlertaService(AppDbContext db) : IAlertaService
{
    private static readonly Expression<Func<Alerta, AlertaResponse>> Proyeccion = a =>
        new AlertaResponse(
            a.Id, a.Regla.Codigo, a.Regla.Nombre, a.Severidad, a.Estado, a.Detalle, a.CreadaEn,
            a.TransaccionId, a.Transaccion.CuentaId, a.Transaccion.Cuenta.NumeroCuenta,
            a.Transaccion.Monto, a.Transaccion.Tipo,
            a.AsignadaAId, a.AsignadaA != null ? a.AsignadaA.NombreUsuario : null, a.CerradaEn);

    public async Task<PagedResult<AlertaResponse>> ListarAsync(FiltroAlertas f)
    {
        var pagina = Math.Max(f.Pagina, 1);
        var tamano = Math.Clamp(f.TamanoPagina, 1, 100);

        var q = db.Alertas.AsNoTracking().AsQueryable();

        if (f.Estado is not null) q = q.Where(a => a.Estado == f.Estado);
        if (f.Severidad is not null) q = q.Where(a => a.Severidad == f.Severidad);
        if (f.ReglaId is not null) q = q.Where(a => a.ReglaId == f.ReglaId);
        if (f.CuentaId is not null) q = q.Where(a => a.Transaccion.CuentaId == f.CuentaId);
        if (f.Desde is not null) q = q.Where(a => a.CreadaEn >= f.Desde);
        if (f.Hasta is not null) q = q.Where(a => a.CreadaEn <= f.Hasta);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(a => a.Severidad).ThenByDescending(a => a.CreadaEn)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .Select(Proyeccion)
            .ToListAsync();

        return new PagedResult<AlertaResponse>(items, total, pagina, tamano);
    }

    public async Task<AlertaResponse?> ObtenerAsync(long id) =>
        await db.Alertas.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync();
}