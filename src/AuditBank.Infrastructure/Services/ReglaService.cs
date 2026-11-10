using System.Text.Json;
using AuditBank.Application.DTOs;
using AuditBank.Application.Interfaces;
using AuditBank.Domain.Entities;
using AuditBank.Domain.Enums;
using AuditBank.Infrastructure.Data;
using AuditBank.Infrastructure.Reglas;
using Microsoft.EntityFrameworkCore;

namespace AuditBank.Infrastructure.Services;

public class ReglaService(AppDbContext db) : IReglaService
{
    public async Task<List<ReglaResponse>> ListarAsync()
    {
        var reglas = await db.Reglas.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
        return reglas.Select(Mapear).ToList();
    }

    public async Task<Resultado> ActualizarAsync(int id, ActualizarReglaRequest r)
    {
        var regla = await db.Reglas.FirstOrDefaultAsync(x => x.Id == id);
        if (regla is null) return Resultado.NoEncontrado("La regla no existe.");

        if (!Enum.IsDefined(r.Severidad))
            return Resultado.Invalido("La severidad es inválida.");

        var json = r.Parametros.GetRawText();
        var error = Validar(regla.Tipo, json);
        if (error is not null) return Resultado.Invalido(error);

        regla.Activa = r.Activa;
        regla.Severidad = r.Severidad;
        regla.Parametros = json;
        await db.SaveChangesAsync();
        return Resultado.Exito();
    }

    private static string? Validar(TipoRegla tipo, string json)
    {
        try
        {
            switch (tipo)
            {
                case TipoRegla.MontoAlto:
                    var monto = ReglasJson.Leer<ParamsMontoAlto>(json);
                    if (monto.Umbral <= 0) return "El umbral debe ser mayor a 0.";
                    if (!Enum.IsDefined(monto.Moneda)) return "La moneda es inválida (BOB o USD).";
                    break;

                case TipoRegla.Fraccionamiento:
                    var frac = ReglasJson.Leer<ParamsFraccionamiento>(json);
                    if (frac.Umbral <= 0) return "El umbral debe ser mayor a 0.";
                    if (!Enum.IsDefined(frac.Moneda)) return "La moneda es inválida (BOB o USD).";
                    if (frac.VentanaHoras <= 0) return "La ventana debe ser mayor a 0 horas.";
                    if (frac.CantidadMinima < 2) return "La cantidad mínima de operaciones debe ser al menos 2.";
                    break;

                case TipoRegla.FueraDeHorario:
                    var hor = ReglasJson.Leer<ParamsFueraHorario>(json);
                    if (hor.HoraInicio < 0 || hor.HoraFin > 24 || hor.HoraInicio >= hor.HoraFin)
                        return "El horario es inválido: la hora de inicio debe ser menor a la de fin (0 a 24).";
                    if (hor.OffsetUtcHoras < -12 || hor.OffsetUtcHoras > 14)
                        return "El desfase UTC debe estar entre -12 y 14.";
                    break;
            }
            return null;
        }
        catch (JsonException)
        {
            return "Los parámetros no tienen el formato esperado.";
        }
    }

    private static ReglaResponse Mapear(ReglaAuditoria r)
    {
        using var doc = JsonDocument.Parse(r.Parametros);
        return new ReglaResponse(
            r.Id, r.Codigo, r.Nombre, r.Descripcion, r.Tipo, r.Severidad, r.Activa,
            doc.RootElement.Clone());
    }
}