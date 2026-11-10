using AuditBank.Application.DTOs;
using AuditBank.Domain.Enums;
using AuditBank.Infrastructure.Data;
using AuditBank.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AuditBank.Tests;

public class CuentaServiceTests
{
    [Fact]
    public async Task CrearAsync_TrimAndRoundAndPersistAccount()
    {
        await using var db = CrearDbContext();
        var service = new CuentaService(db);

        var (cuenta, error) = await service.CrearAsync(
            new CrearCuentaRequest("  001-0001  ", "  Ana Pérez  ", TipoCuenta.CajaAhorro, Moneda.BOB, 125.678m));

        Assert.Null(error);
        Assert.NotNull(cuenta);
        Assert.Equal("001-0001", cuenta.NumeroCuenta);
        Assert.Equal("Ana Pérez", cuenta.Titular);
        Assert.Equal(125.68m, cuenta.Saldo);
        Assert.Single(await db.Cuentas.ToListAsync());
    }

    [Theory]
    [InlineData("", "Ana Pérez", 100)]
    [InlineData("001-0001", " ", 100)]
    [InlineData("001-0001", "Ana Pérez", -0.01)]
    public async Task CrearAsync_RechazaDatosInvalidos(string numero, string titular, decimal saldo)
    {
        await using var db = CrearDbContext();
        var service = new CuentaService(db);

        var (cuenta, error) = await service.CrearAsync(
            new CrearCuentaRequest(numero, titular, TipoCuenta.CajaAhorro, Moneda.BOB, saldo));

        Assert.Null(cuenta);
        Assert.NotNull(error);
        Assert.Empty(await db.Cuentas.ToListAsync());
    }

    [Fact]
    public async Task CrearAsync_RechazaNumeroDeCuentaDuplicado()
    {
        await using var db = CrearDbContext();
        var service = new CuentaService(db);
        var request = new CrearCuentaRequest(
            "001-0001", "Ana Pérez", TipoCuenta.CajaAhorro, Moneda.BOB, 100);

        var (primera, primerError) = await service.CrearAsync(request);
        var (duplicada, segundoError) = await service.CrearAsync(request);

        Assert.NotNull(primera);
        Assert.Null(primerError);
        Assert.Null(duplicada);
        Assert.Equal("Ya existe una cuenta con ese número.", segundoError);
        Assert.Single(await db.Cuentas.ToListAsync());
    }

    private static AppDbContext CrearDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
