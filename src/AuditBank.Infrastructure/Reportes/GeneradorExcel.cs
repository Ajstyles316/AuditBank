using AuditBank.Application.DTOs;
using ClosedXML.Excel;

namespace AuditBank.Infrastructure.Reportes;

public static class GeneradorExcel
{
    private const int FilaEncabezado = 4;

    public static byte[] Generar(TablaReporte t)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Reporte");

        // Título y subtítulo
        hoja.Cell(1, 1).Value = t.Titulo;
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(1, 1).Style.Font.FontSize = 14;
        hoja.Cell(2, 1).Value = t.Subtitulo;
        hoja.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

        // Encabezados
        for (var c = 0; c < t.Columnas.Length; c++)
            hoja.Cell(FilaEncabezado, c + 1).Value = t.Columnas[c];

        var encabezado = hoja.Range(FilaEncabezado, 1, FilaEncabezado, t.Columnas.Length);
        encabezado.Style.Font.Bold = true;
        encabezado.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Datos, conservando el tipo de cada valor
        for (var r = 0; r < t.Filas.Count; r++)
            for (var c = 0; c < t.Columnas.Length; c++)
                Asignar(hoja.Cell(FilaEncabezado + 1 + r, c + 1), t.Filas[r][c]);

        var ultimaFila = FilaEncabezado + Math.Max(t.Filas.Count, 1);

        // Formatos por columna (montos, fechas)
        for (var c = 0; c < t.Formatos.Length; c++)
            if (t.Formatos[c] is { } formato)
                hoja.Range(FilaEncabezado + 1, c + 1, ultimaFila, c + 1).Style.NumberFormat.Format = formato;

        hoja.Range(FilaEncabezado, 1, ultimaFila, t.Columnas.Length).SetAutoFilter();
        hoja.SheetView.FreezeRows(FilaEncabezado);

        foreach (var columna in hoja.ColumnsUsed())
        {
            columna.AdjustToContents(FilaEncabezado, FilaEncabezado + Math.Min(t.Filas.Count, 200));
            if (columna.Width > 60) columna.Width = 60;
        }

        using var ms = new MemoryStream();
        libro.SaveAs(ms);
        return ms.ToArray();
    }

    private static void Asignar(IXLCell celda, object? valor)
    {
        switch (valor)
        {
            case null: break;
            case string s: celda.Value = s; break;
            case DateTime d: celda.Value = d; break;
            case decimal m: celda.Value = m; break;
            case long l: celda.Value = l; break;
            case int i: celda.Value = i; break;
            default: celda.Value = valor.ToString() ?? string.Empty; break;
        }
    }
}