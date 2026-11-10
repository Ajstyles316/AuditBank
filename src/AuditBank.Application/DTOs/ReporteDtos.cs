namespace AuditBank.Application.DTOs;

public enum FormatoReporte { Excel = 1, Pdf = 2 }

public record ArchivoReporte(byte[] Contenido, string TipoContenido, string NombreArchivo);

public record TablaReporte(
    string Titulo,
    string Subtitulo,
    string[] Columnas,
    float[] Anchos,          // anchos relativos de las columnas (PDF)
    string?[] Formatos,      // formato numérico/fecha de cada columna (Excel); null = sin formato
    List<object?[]> Filas);