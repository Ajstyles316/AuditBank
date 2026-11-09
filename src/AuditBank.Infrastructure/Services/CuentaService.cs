using AuditBank.Application.DTOs;
using AuditBank.Application.Interfaces;
using AuditBank.Domain.Entities;
using AuditBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuditBank.Infrastructure.Services;

public class CuentaService(AppDbContext db) : ICuentaService
{
    public async Task<(CuentaResponse? Resultado, string? Error)> CrearAsync(CrearCuentaRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.NumeroCuenta) || string.IsNullOrWhiteSpace(r.Titular))
            return (null, "El número de cuenta y el titular son obligatorios.");
        if (r.SaldoInicial < 0)
            return (null, "El saldo inicial no puede ser negativo.");
        if (await db.Cuentas.AnyAsync(c => c.NumeroCuenta == r.NumeroCuenta))
            return (null, "Ya existe una cuenta con ese número.");

        var cuenta = new Cuenta
        {
            NumeroCuenta = r.NumeroCuenta.Trim(),
            Titular = r.Titular.Trim(),
            Tipo = r.Tipo,
            Moneda = r.Moneda,
            Saldo = decimal.Round(r.SaldoInicial, 2)
        };

        db.Cuentas.Add(cuenta);
        await db.SaveChangesAsync();
        return (Mapear(cuenta), null);
    }

    public async Task<List<CuentaResponse>> ListarAsync()
    {
        var cuentas = await db.Cuentas.AsNoTracking().OrderBy(c => c.NumeroCuenta).ToListAsync();
        return cuentas.Select(Mapear).ToList();
    }

    public async Task<CuentaResponse?> ObtenerAsync(int id)
    {
        var cuenta = await db.Cuentas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return cuenta is null ? null : Mapear(cuenta);
    }

    private static CuentaResponse Mapear(Cuenta c) =>
        new(c.Id, c.NumeroCuenta, c.Titular, c.Tipo, c.Moneda, c.Saldo, c.Activa, c.CreadaEn);
}