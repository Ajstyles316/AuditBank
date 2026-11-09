using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AuditBank.Application.Interfaces;
using AuditBank.Domain.Entities;
using AuditBank.Domain.Enums;
using AuditBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuditBank.Infrastructure.Reglas;

internal static class ReglasJson
{
    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static T Leer<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Opciones)
        ?? throw new InvalidOperationException("Parámetros de regla inválidos.");

    public static string Dinero(decimal valor) => valor.ToString("N2", CultureInfo.InvariantCulture);
}

public record ParamsMontoAlto(decimal Umbral, Moneda Moneda);
public record ParamsFraccionamiento(decimal Umbral, Moneda Moneda, int VentanaHoras, int CantidadMinima);
public record ParamsFueraHorario(int HoraInicio, int HoraFin, int OffsetUtcHoras, bool IncluirFinesDeSemana);

// ---------------------------------------------------------------
public class MontoAltoEvaluador : IEvaluadorRegla
{
    public TipoRegla Tipo => TipoRegla.MontoAlto;

    public Task<string?> EvaluarAsync(Transaccion t, string parametrosJson)
    {
        var p = ReglasJson.Leer<ParamsMontoAlto>(parametrosJson);

        if (t.Cuenta.Moneda != p.Moneda || t.Monto < p.Umbral)
            return Task.FromResult<string?>(null);

        return Task.FromResult<string?>(
            $"Transacción de {ReglasJson.Dinero(t.Monto)} {p.Moneda} supera el umbral de {ReglasJson.Dinero(p.Umbral)} {p.Moneda}.");
    }
}

// ---------------------------------------------------------------
public class FraccionamientoEvaluador(AppDbContext db) : IEvaluadorRegla
{
    public TipoRegla Tipo => TipoRegla.Fraccionamiento;

    public async Task<string?> EvaluarAsync(Transaccion t, string parametrosJson)
    {
        var p = ReglasJson.Leer<ParamsFraccionamiento>(parametrosJson);

        // Si la operación ya supera el umbral por sí sola, la cubre la regla de monto alto.
        if (t.Cuenta.Moneda != p.Moneda || t.Monto >= p.Umbral)
            return null;

        var desde = t.FechaUtc.AddHours(-p.VentanaHoras);

        var montos = await db.Transacciones.AsNoTracking()
            .Where(x => x.CuentaId == t.CuentaId
                     && x.Tipo == t.Tipo
                     && x.FechaUtc >= desde
                     && x.FechaUtc <= t.FechaUtc)
            .Select(x => x.Monto)
            .ToListAsync();

        var total = montos.Sum();
        if (montos.Count < p.CantidadMinima || total < p.Umbral)
            return null;

        return $"Posible fraccionamiento: {montos.Count} operaciones de tipo {t.Tipo} en la cuenta en las últimas " +
               $"{p.VentanaHoras} h suman {ReglasJson.Dinero(total)} {p.Moneda} (umbral {ReglasJson.Dinero(p.Umbral)}).";
    }
}

// ---------------------------------------------------------------
public class FueraDeHorarioEvaluador : IEvaluadorRegla
{
    public TipoRegla Tipo => TipoRegla.FueraDeHorario;

    public Task<string?> EvaluarAsync(Transaccion t, string parametrosJson)
    {
        var p = ReglasJson.Leer<ParamsFueraHorario>(parametrosJson);

        var local = t.FechaUtc.AddHours(p.OffsetUtcHoras);
        var esFinDeSemana = p.IncluirFinesDeSemana
            && local.DayOfWeek is (DayOfWeek.Saturday or DayOfWeek.Sunday);
        var fueraDeHora = local.Hour < p.HoraInicio || local.Hour >= p.HoraFin;

        if (!esFinDeSemana && !fueraDeHora)
            return Task.FromResult<string?>(null);

        var motivo = esFinDeSemana ? "en fin de semana" : $"fuera del horario {p.HoraInicio}:00–{p.HoraFin}:00";
        var fecha = local.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        return Task.FromResult<string?>($"Operación registrada el {fecha} (hora local), {motivo}.");
    }
}