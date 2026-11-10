using System.Globalization;
using System.Text.Json;
using AuditBank.Application.DTOs;
using AuditBank.Application.Interfaces;
using AuditBank.Domain.Entities;
using AuditBank.Domain.Enums;
using AuditBank.Infrastructure.Data;
using AuditBank.Infrastructure.Reportes;
using Microsoft.EntityFrameworkCore;

namespace AuditBank.Infrastructure.Services;

public class ReporteService(
    ITransaccionService transacciones,
    IAlertaService alertas,
    IAuditoriaService auditoria,
    AppDbContext db,
    IUsuarioActual actual) : IReporteService
{
    private const int MaxFilas = 5000;
    private const string FormatoFecha = "yyyy-mm-dd hh:mm";
    private const string FormatoMonto = "#,##0.00";

    public async Task<ArchivoReporte> TransaccionesAsync(FiltroTransacciones f, FormatoReporte formato)
    {
        f.TamanoPagina = 100;
        var (items, total) = await TraerTodoAsync(p => { f.Pagina = p; return transacciones.ListarAsync(f); });

        var filtros = Filtros(("cuentaId", f.CuentaId), ("tipo", f.Tipo), ("desde", f.Desde),
            ("hasta", f.Hasta), ("montoMin", f.MontoMin), ("montoMax", f.MontoMax));

        var tabla = new TablaReporte(
            "Reporte de transacciones",
            await SubtituloAsync(filtros, items.Count, total),
            ["Id", "Fecha (UTC)", "Tipo", "Cuenta", "Cuenta destino", "Monto", "Descripción", "Registrada por"],
            [1f, 2f, 1.5f, 2f, 2f, 1.5f, 4f, 2f],
            [null, FormatoFecha, null, null, null, FormatoMonto, null, null],
            items.Select(t => new object?[]
            {
                t.Id, t.FechaUtc, t.Tipo.ToString(), t.NumeroCuenta, t.NumeroCuentaDestino,
                t.Monto, t.Descripcion, t.RegistradaPor
            }).ToList());

        return await EntregarAsync("transacciones", tabla, formato, filtros, items.Count);
    }

    public async Task<ArchivoReporte> AlertasAsync(FiltroAlertas f, FormatoReporte formato)
    {
        f.TamanoPagina = 100;
        var (items, total) = await TraerTodoAsync(p => { f.Pagina = p; return alertas.ListarAsync(f); });

        var filtros = Filtros(("estado", f.Estado), ("severidad", f.Severidad), ("reglaId", f.ReglaId),
            ("cuentaId", f.CuentaId), ("desde", f.Desde), ("hasta", f.Hasta));

        var tabla = new TablaReporte(
            "Reporte de alertas",
            await SubtituloAsync(filtros, items.Count, total),
            ["Id", "Creada (UTC)", "Severidad", "Estado", "Regla", "Cuenta", "Monto", "Detalle", "Asignada a", "Cerrada (UTC)"],
            [1f, 2f, 1.3f, 1.5f, 2.5f, 2f, 1.5f, 5f, 1.8f, 2f],
            [null, FormatoFecha, null, null, null, null, FormatoMonto, null, null, FormatoFecha],
            items.Select(a => new object?[]
            {
                a.Id, a.CreadaEn, a.Severidad.ToString(), a.Estado.ToString(), a.ReglaCodigo,
                a.NumeroCuenta, a.Monto, a.Detalle, a.AsignadaA, a.CerradaEn
            }).ToList());

        return await EntregarAsync("alertas", tabla, formato, filtros, items.Count);
    }

    public async Task<ArchivoReporte> AuditoriaAsync(FiltroAuditoria f, FormatoReporte formato)
    {
        f.TamanoPagina = 100;
        var (items, total) = await TraerTodoAsync(p => { f.Pagina = p; return auditoria.ListarAsync(f); });

        var filtros = Filtros(("entidad", f.Entidad), ("entidadId", f.EntidadId), ("usuarioId", f.UsuarioId),
            ("accion", f.Accion), ("desde", f.Desde), ("hasta", f.Hasta));

        var tabla = new TablaReporte(
            "Reporte de bitácora de auditoría",
            await SubtituloAsync(filtros, items.Count, total),
            ["Id", "Fecha (UTC)", "Usuario", "Acción", "Entidad", "Id entidad", "IP", "Valor anterior", "Valor nuevo"],
            [1f, 2f, 1.8f, 1.5f, 2f, 1.2f, 1.8f, 4f, 4f],
            [null, FormatoFecha, null, null, null, null, null, null, null],
            items.Select(r => new object?[]
            {
                r.Id, r.FechaUtc, r.Usuario, r.Accion.ToString(), r.Entidad, r.EntidadId,
                r.DireccionIp, r.ValorAnterior, r.ValorNuevo
            }).ToList());

        return await EntregarAsync("auditoria", tabla, formato, filtros, items.Count);
    }

    // ---------------- Apoyo ----------------

    private static async Task<(List<T> Items, int Total)> TraerTodoAsync<T>(
        Func<int, Task<PagedResult<T>>> obtenerPagina)
    {
        var items = new List<T>();
        var total = 0;

        for (var pagina = 1; items.Count < MaxFilas; pagina++)
        {
            var resultado = await obtenerPagina(pagina);
            total = resultado.Total;
            items.AddRange(resultado.Items);
            if (resultado.Items.Count == 0 || items.Count >= resultado.Total) break;
        }

        return (items.Take(MaxFilas).ToList(), total);
    }

    private static string Filtros(params (string Nombre, object? Valor)[] pares)
    {
        var activos = pares
            .Where(p => p.Valor is not null && !(p.Valor is string s && string.IsNullOrWhiteSpace(s)))
            .Select(p => $"{p.Nombre}={Convert.ToString(p.Valor, CultureInfo.InvariantCulture)}");

        var texto = string.Join(", ", activos);
        return texto.Length == 0 ? "sin filtros" : texto;
    }

    private async Task<string> SubtituloAsync(string filtros, int mostradas, int total)
    {
        var usuario = actual.Id is int id
            ? await db.Usuarios.AsNoTracking().Where(u => u.Id == id)
                .Select(u => u.NombreUsuario).FirstOrDefaultAsync()
            : null;

        var texto = $"Generado el {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC por {usuario ?? "desconocido"}. " +
                    $"Filtros: {filtros}. Filas: {mostradas}";

        if (total > mostradas)
            texto += $" de {total} (el reporte se limita a {MaxFilas}; afina los filtros).";

        return texto;
    }

    private async Task<ArchivoReporte> EntregarAsync(
        string nombre, TablaReporte tabla, FormatoReporte formato, string filtros, int filas)
    {
        var (bytes, tipo, extension) = formato == FormatoReporte.Pdf
            ? (GeneradorPdf.Generar(tabla), "application/pdf", "pdf")
            : (GeneradorExcel.Generar(tabla),
               "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx");

        await RegistrarExportacionAsync(nombre, formato, filtros, filas);

        return new ArchivoReporte(bytes, tipo, $"{nombre}_{DateTime.UtcNow:yyyyMMdd_HHmm}.{extension}");
    }

    private async Task RegistrarExportacionAsync(string reporte, FormatoReporte formato, string filtros, int filas)
    {
        db.RegistrosAuditoria.Add(new RegistroAuditoria
        {
            Accion = AccionAuditoria.Exportar,
            Entidad = $"Reporte:{reporte}",
            ValorNuevo = JsonSerializer.Serialize(new { formato = formato.ToString(), filtros, filas }),
            UsuarioId = actual.Id,
            DireccionIp = actual.DireccionIp
        });
        await db.SaveChangesAsync();
    }
}