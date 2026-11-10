using AuditBank.Domain.Entities;
using AuditBank.Domain.Enums;
using AuditBank.Infrastructure.Reglas;

namespace AuditBank.Tests;

public class EvaluadoresReglaTests
{
    private const string ParametrosMontoAlto = """{"umbral":70000,"moneda":"BOB"}""";

    [Fact]
    public async Task MontoAlto_CreaDetalleCuandoSuperaElUmbral()
    {
        var evaluador = new MontoAltoEvaluador();
        var transaccion = CrearTransaccion(70_000m, Moneda.BOB);

        var detalle = await evaluador.EvaluarAsync(transaccion, ParametrosMontoAlto);

        Assert.NotNull(detalle);
        Assert.Contains("70,000.00 BOB", detalle);
    }

    [Theory]
    [InlineData(69_999.99, Moneda.BOB)]
    [InlineData(70_000, Moneda.USD)]
    public async Task MontoAlto_NoCreaDetalleSiNoCoincideElUmbralOMoneda(decimal monto, Moneda moneda)
    {
        var evaluador = new MontoAltoEvaluador();
        var transaccion = CrearTransaccion(monto, moneda);

        var detalle = await evaluador.EvaluarAsync(transaccion, ParametrosMontoAlto);

        Assert.Null(detalle);
    }

    private static Transaccion CrearTransaccion(decimal monto, Moneda moneda) =>
        new()
        {
            Monto = monto,
            Cuenta = new Cuenta { Moneda = moneda }
        };
}
