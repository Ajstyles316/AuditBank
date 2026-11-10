using System.Globalization;
using AuditBank.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AuditBank.Infrastructure.Reportes;

public static class GeneradorPdf
{
    public static byte[] Generar(TablaReporte t)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4.Landscape());
            pagina.Margin(25);
            pagina.DefaultTextStyle(x => x.FontSize(8));

            pagina.Header().Column(col =>
            {
                col.Item().Text(t.Titulo).FontSize(15).Bold();
                col.Item().Text(t.Subtitulo).FontSize(8).FontColor(Colors.Grey.Darken2);
            });

            pagina.Content().PaddingVertical(8).Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    foreach (var ancho in t.Anchos)
                        columnas.RelativeColumn(ancho);
                });

                tabla.Header(encabezado =>
                {
                    foreach (var nombre in t.Columnas)
                        encabezado.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text(nombre).Bold();
                });

                foreach (var fila in t.Filas)
                    foreach (var valor in fila)
                        tabla.Cell()
                            .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(3).Text(Formatear(valor));
            });

            pagina.Footer().AlignCenter().Text(x =>
            {
                x.Span("Página ");
                x.CurrentPageNumber();
                x.Span(" de ");
                x.TotalPages();
            });
        })).GeneratePdf();
    }

    private static string Formatear(object? valor) => valor switch
    {
        null => string.Empty,
        DateTime d => d.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        decimal m => m.ToString("N2", CultureInfo.InvariantCulture),
        _ => Convert.ToString(valor, CultureInfo.InvariantCulture) ?? string.Empty
    };
}